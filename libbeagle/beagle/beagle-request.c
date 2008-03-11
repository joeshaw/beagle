/*
 * beagle-request.c
 *
 * Copyright (C) 2005 Novell, Inc.
 *
 */

/*
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <fcntl.h>
#include <errno.h>

#include "beagle-error-response.h"
#include "beagle-marshal.h"
#include "beagle-parser.h"
#include "beagle-request.h"
#include "beagle-private.h"
#include "beagle-util.h"

typedef struct {
	char *path;
	GIOChannel *channel;
	guint io_watch;
	BeagleParserContext *ctx;
#ifdef ENABLE_XML_DUMP
	GString *data;
#endif
} BeagleRequestPrivate;

#define BEAGLE_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_REQUEST, BeagleRequestPrivate))

enum {
	CLOSED,
	RESPONSE,
	ERROR,
	LAST_SIGNAL
};

static GObjectClass *parent_class = NULL;
static guint signals [LAST_SIGNAL] = { 0 };

G_DEFINE_TYPE (BeagleRequest, beagle_request, G_TYPE_OBJECT)

static void
beagle_request_finalize (GObject *obj)
{
	BeagleRequestPrivate *priv = BEAGLE_REQUEST_GET_PRIVATE (obj);

	g_free (priv->path);

	if (priv->io_watch != 0) {
		g_source_remove (priv->io_watch);
		priv->io_watch = 0;
	}

	if (priv->channel) {
		g_io_channel_unref (priv->channel);
		priv->channel = NULL;
	}
	
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_request_class_init (BeagleRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_request_finalize;

	signals [CLOSED] = g_signal_new ("closed",
					 G_TYPE_FROM_CLASS (klass),
					 G_SIGNAL_RUN_LAST,
					 G_STRUCT_OFFSET (BeagleRequestClass, closed),
					 NULL, NULL,
					 beagle_marshal_VOID__VOID,
					 G_TYPE_NONE, 0);
	signals [RESPONSE] = g_signal_new ("response",
					   G_TYPE_FROM_CLASS (klass),
					   G_SIGNAL_RUN_LAST,
					   G_STRUCT_OFFSET (BeagleRequestClass, response),
					   NULL, NULL,
					   g_cclosure_marshal_VOID__OBJECT,
					   G_TYPE_NONE, 1,
					   BEAGLE_TYPE_RESPONSE);
	signals [ERROR] = g_signal_new ("error",
					G_TYPE_FROM_CLASS (klass),
					G_SIGNAL_RUN_LAST,
					G_STRUCT_OFFSET (BeagleRequestClass, error),
					NULL, NULL,
					g_cclosure_marshal_VOID__POINTER,
					G_TYPE_NONE, 1,
					G_TYPE_POINTER);
	
	g_type_class_add_private (klass, sizeof (BeagleRequestPrivate));

	klass->response_types = g_hash_table_new_full (g_str_hash,
						       g_str_equal,
						       g_free,
						       NULL);

	g_hash_table_insert (klass->response_types,
			     g_strdup ("ErrorResponse"),
			     (gpointer) BEAGLE_TYPE_ERROR_RESPONSE);
}

static void
beagle_request_init (BeagleRequest *request)
{
}

void
_beagle_request_class_set_response_types (BeagleRequestClass *klass,
					  const char *beagle_type,
					  GType gobject_type,
					  ...)
{
	va_list args;
	const char *arg;

	g_hash_table_replace (klass->response_types,
			      g_strdup (beagle_type),
			      (gpointer) gobject_type);

	va_start (args, gobject_type);
	arg = va_arg (args, const char *);
	
	while (arg != NULL) {
		GType gtype = va_arg (args, GType);
	  
		g_hash_table_replace (klass->response_types,
				      g_strdup (arg),
				      (gpointer) gtype);

		arg = va_arg (args, const char *);
	}

	va_end (args);
}

int
_beagle_connect_timeout (const char *path, GError **err)
{
	int sockfd;
	int ret;
	int error, error_len;
	long mode;
	fd_set select_set;
	struct timeval tv;
	struct sockaddr_un sun;

	sockfd = socket (AF_UNIX, SOCK_STREAM, 0);
	if (sockfd < 0) {
		g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
			     "Unable to create connection");
		return -1;
	}

	/* Set non-blocking mode */
	if ((mode = fcntl (sockfd, F_GETFL, NULL)) < 0) {
		g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
			    "Internal error: unable to get status flag for socket");
		return -1;
	}

	mode |= O_NONBLOCK;

	if (fcntl (sockfd, F_SETFL, mode) < 0) {
		g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
			    "Internal error: unable to set socket to non-blocking mode");
		return -1;
	}

	bzero (&sun, sizeof (sun));
	sun.sun_family = AF_UNIX;
	snprintf (sun.sun_path, sizeof (sun.sun_path), path);

	if (connect (sockfd, (struct sockaddr *) &sun, sizeof (sun)) < 0) {
		if (errno != EINPROGRESS) {
			g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
				"Unable to connect to Beagle daemon");
			return -1;
		} else {
			tv.tv_sec = 10; /* 5 seconds should be enough for a unix socket */
			tv.tv_usec = 0;

			FD_ZERO (&select_set);
			FD_SET (sockfd, &select_set);

			ret = select (sockfd + 1, NULL, &select_set, NULL, &tv);
			if (ret > 0) {
				/* Check for any error during select() */
				error_len = sizeof (error);
				ret = getsockopt (sockfd, SOL_SOCKET, SO_ERROR, &error, &error_len);
				if (ret < 0 || error != 0) {
					g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
						"Unable to connect to Beagle daemon");
					close (sockfd);
					return -1;
				}
				/* Else, connection successful */
			} else {
				g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
					"Unable to connect to Beagle daemon");
				close (sockfd);
				return -1;
			}
		}
	}

	/* Set socket to blocking mode */
	if ((mode = fcntl (sockfd, F_GETFL, NULL)) < 0) {
		g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
			    "Internal error: unable to get status flag for socket");
		close (sockfd);
		return -1;
	}

	mode &= (~O_NONBLOCK);

	if (fcntl (sockfd, F_SETFL, mode) < 0) {
		g_set_error (err, BEAGLE_ERROR, BEAGLE_ERROR,
			    "Internal error: unable to set socket to non-blocking mode");
		close (sockfd);
		return -1;
	}

	/* Finally, we are done ! */
	return sockfd;
}

static gboolean
request_connect (BeagleRequest *request, const char *path, GError **err)
{
	BeagleRequestPrivate *priv;
	int sockfd;
	struct sockaddr_un sun;

	priv = BEAGLE_REQUEST_GET_PRIVATE (request);

	sockfd = _beagle_connect_timeout (path, err);
	if (sockfd == -1)
		return FALSE;

	g_free (priv->path);
	priv->path = g_strdup (path);

	priv->channel = g_io_channel_unix_new (sockfd);

	g_io_channel_set_encoding (priv->channel, NULL, NULL);
	g_io_channel_set_buffered (priv->channel, FALSE);
	g_io_channel_set_close_on_unref (priv->channel, TRUE);

	return TRUE;
}

static void
request_close (BeagleRequest *request)
{
	BeagleRequestPrivate *priv;

	priv = BEAGLE_REQUEST_GET_PRIVATE (request);

	g_return_if_fail (priv->channel != NULL);

	g_free (priv->path);
	priv->path = NULL;

	g_io_channel_unref (priv->channel);
	priv->channel = NULL;

	g_source_remove (priv->io_watch);
	priv->io_watch = 0;

	g_signal_emit (request, signals [CLOSED], 0);
}

static gboolean
request_send (BeagleRequest *request, const char *socket_path, GError **err)
{
	BeagleRequestPrivate *priv;
	GString *buffer;
	gsize bytes_written, total_written;
	char eom_marker = 0xff;
	GIOStatus status;

	if (!request_connect (request, socket_path, err))
		return FALSE;

	priv = BEAGLE_REQUEST_GET_PRIVATE (request);

	buffer = BEAGLE_REQUEST_GET_CLASS (request)->to_xml (request, err);
	if (buffer == NULL)
		return FALSE;

#ifdef ENABLE_XML_DUMP
	printf ("Sending request:\n");
	printf ("%*s\n\n", buffer->len, buffer->str);
#endif

	/* Send the data over the wire */
	total_written = 0;
	do {
		status = g_io_channel_write_chars (priv->channel,
						   buffer->str + total_written,
						   buffer->len - total_written,
						   &bytes_written,
						   err);
		total_written += bytes_written;
	} while ((status == G_IO_STATUS_NORMAL || status == G_IO_STATUS_AGAIN)
		 && total_written < buffer->len);

	if (status == G_IO_STATUS_ERROR)
		return FALSE;

	/* And send the end-of-message marker */
	do {
		status = g_io_channel_write_chars (priv->channel,
						   &eom_marker, 1,
						   &bytes_written,
						   err);
	} while ((status == G_IO_STATUS_NORMAL || status == G_IO_STATUS_AGAIN)
		 && total_written == 0);

	if (status == G_IO_STATUS_ERROR)
		return FALSE;

	return TRUE;
}

static gboolean
request_io_cb (GIOChannel *channel, GIOCondition condition, gpointer user_data)
{
	BeagleRequestPrivate *priv = BEAGLE_REQUEST_GET_PRIVATE (user_data);
	gsize bytes_read;
	char buf[4096];
	GIOStatus status;
	char *marker;
	int start, to_parse;
	BeagleResponse *response;

	if (condition & G_IO_IN) {
		do {	
			GError *error = NULL;

			status = g_io_channel_read_chars (priv->channel,
							  buf, 4096,
							  &bytes_read,
							  &error);
			
			if (status == G_IO_STATUS_ERROR) {
				g_signal_emit (user_data, signals[ERROR], 0, error);
				g_error_free (error);
				error = NULL;
			}

			if (bytes_read > 0) {
				start = 0;

#ifdef ENABLE_XML_DUMP
				if (priv->data == NULL)
					priv->data = g_string_new (NULL);
#endif

				while (start < bytes_read) {
					marker = memchr (buf + start, 0xff, bytes_read - start);

					if (!priv->ctx) {
						priv->ctx = _beagle_parser_context_new ();
					}

					if (marker != NULL) {
						to_parse = (marker - buf) - start;

						if (to_parse > 0) {
#ifdef ENABLE_XML_DUMP
							priv->data = g_string_append_len (priv->data, buf + start, to_parse);
#endif
							_beagle_parser_context_parse_chunk (priv->ctx, buf + start, to_parse);
						}
						
#ifdef ENABLE_XML_DUMP
						printf ("Received async response:\n");
						printf ("%s\n\n", priv->data->str);
						g_string_free (priv->data, TRUE);
						priv->data = NULL;
#endif

						/* Finish the context */
						response = _beagle_parser_context_finished (priv->ctx);
						g_assert (response != NULL);
						
						if (BEAGLE_IS_ERROR_RESPONSE (response)) {
							_beagle_error_response_to_g_error (BEAGLE_ERROR_RESPONSE (response), &error);
							g_signal_emit (BEAGLE_REQUEST (user_data),
								       signals[ERROR], 0, error);
							g_error_free (error);
							error = NULL;
						} else {
							g_signal_emit (BEAGLE_REQUEST (user_data),
								       signals[RESPONSE], 0, response);
						}
						g_object_unref (response);

						/* Move past the 0xff marker */
						start += to_parse + 1;
						
						priv->ctx = NULL;
					}
					else {
#ifdef ENABLE_XML_DUMP
						priv->data = g_string_append_len (priv->data, buf + start, bytes_read - start);
#endif
						_beagle_parser_context_parse_chunk (priv->ctx, buf + start, bytes_read - start);
						break;
					}
				}
			}
		} while (bytes_read == 4096 || status == G_IO_STATUS_AGAIN);
	}

	if (condition & G_IO_HUP ||
	    condition & G_IO_ERR) {
		request_close (BEAGLE_REQUEST (user_data));
	}

	return TRUE;
}

gboolean
_beagle_request_send_async (BeagleRequest  *request, 
			    const char     *socket_path, 
			    GError        **err)
{
	BeagleRequestPrivate *priv = BEAGLE_REQUEST_GET_PRIVATE (request);

	if (!request_send (request, socket_path, err))
		return FALSE;

	priv->io_watch = g_io_add_watch (priv->channel,
					 G_IO_IN | G_IO_HUP | G_IO_ERR,
					 request_io_cb,
					 request);

	return TRUE;
}

BeagleResponse *
_beagle_request_send (BeagleRequest *request, const char *socket_path, GError **err)
{
	BeagleRequestPrivate *priv;
	BeagleParserContext *ctx;
	char buf [4096];
	gsize bytes_read;
	GIOStatus status;
	char *marker = NULL;
	BeagleResponse *response;

	if (!request_send (request, socket_path, err))
		return FALSE;

	priv = BEAGLE_REQUEST_GET_PRIVATE (request);

	ctx = _beagle_parser_context_new ();

#ifdef ENABLE_XML_DUMP
	priv->data = g_string_new (NULL);
#endif

	do {
		gsize to_parse;

		status = g_io_channel_read_chars (priv->channel,
						  buf, 4096,
						  &bytes_read,
						  err);
		
		if (bytes_read > 0) {
			marker = memchr (buf, 0xff, bytes_read);

			if (marker != NULL)
				to_parse = marker - buf;
			else
				to_parse = bytes_read;

#ifdef ENABLE_XML_DUMP
			priv->data = g_string_append_len (priv->data, buf, to_parse);
#endif
			_beagle_parser_context_parse_chunk (ctx, buf, to_parse);
		}
	} while ((status == G_IO_STATUS_NORMAL || G_IO_STATUS_AGAIN) &&
		 marker == NULL);

#ifdef ENABLE_XML_DUMP
	printf ("Received sync response:\n");
	printf ("%s\n\n", priv->data->str);
	g_string_free (priv->data, TRUE);
	priv->data = NULL;
#endif

	response = _beagle_parser_context_finished (ctx);

	g_io_channel_unref (priv->channel);
	priv->channel = NULL;

	if (BEAGLE_IS_ERROR_RESPONSE (response)) {
		_beagle_error_response_to_g_error (BEAGLE_ERROR_RESPONSE (response), err);
		g_object_unref (response);
		return NULL;
	}

	return response;
}

void
_beagle_request_append_standard_header (GString *data, const char *xsi_type)
{
	const char header[] =
		"<?xml version=\"1.0\" encoding=\"utf-8\"?>"
		"<RequestWrapper xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">"
		"<Message xsi:type=\"";

	g_string_append_len (data, header, sizeof (header) - 1);
	g_string_append (data, xsi_type);
	g_string_append_len (data, "\">", 2);
}

void
_beagle_request_append_standard_footer (GString *data)
{
	const char footer[] = "</Message></RequestWrapper>";

	g_string_append_len (data, footer, sizeof (footer) - 1);
}
