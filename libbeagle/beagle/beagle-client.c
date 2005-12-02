/*
 * beagle-client.c
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

#include <stdlib.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-private.h"
#include "beagle-client.h"
#include "beagle-util.h"

typedef struct {
	gchar *socket_path;
} BeagleClientPrivate;

#define BEAGLE_CLIENT_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_CLIENT, BeagleClientPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleClient, beagle_client, G_TYPE_OBJECT)

static void
beagle_client_finalize (GObject *obj)
{
	BeagleClientPrivate *priv = BEAGLE_CLIENT_GET_PRIVATE (obj);

	g_free (priv->socket_path);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_client_class_init (BeagleClientClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_client_finalize;

	g_type_class_add_private (klass, sizeof (BeagleClientPrivate));
}

static void
beagle_client_init (BeagleClient *client)
{
}

/**
 * beagle_client_new:
 * @client_name: a string
 *
 * Creates a new #BeagleClient. If @client_name is %NULL it will default to "socket".
 *
 * Return value: a newly created #BeagleClient, or NULL if the client cannot be created.
 **/
BeagleClient *
beagle_client_new (const char *client_name)
{
	BeagleClient *client;
	BeagleClientPrivate *priv;
	const gchar *beagle_home;
	gchar *socket_dir;
	gchar *socket_path;
	struct stat buf;
	
	if (!client_name) 
		client_name = "socket";

	beagle_home = g_getenv ("BEAGLE_HOME");
	if (beagle_home == NULL)
		beagle_home = g_get_home_dir ();

	if (! beagle_util_is_path_on_block_device (beagle_home) ||
	    getenv ("BEAGLE_SYNCHRONIZE_LOCALLY") != NULL) {
		gchar *remote_storage_dir = g_build_filename (beagle_home, ".beagle", "remote_storage_dir", NULL);
		gchar *tmp;

		if (! g_file_test (remote_storage_dir, G_FILE_TEST_EXISTS)) {
			g_free (remote_storage_dir);
			return NULL;
		}

		if (! g_file_get_contents (remote_storage_dir, &socket_dir, NULL, NULL)) {
			g_free (remote_storage_dir);
			return NULL;
		}

		g_free (remote_storage_dir);

		/* There's a newline at the end that we want to strip off */
		tmp = strrchr (socket_dir, '\n');
		if (tmp != NULL)
			*tmp = '\0';

		if (! g_file_test (socket_dir, G_FILE_TEST_EXISTS | G_FILE_TEST_IS_DIR)) {
			g_free (socket_dir);
			return NULL;
		}
	} else {
		socket_dir = g_build_filename (beagle_home, ".beagle", NULL);
	}

	socket_path = g_build_filename (socket_dir, client_name, NULL);
	g_free (socket_dir);
	if (stat (socket_path, &buf) == -1 || !S_ISSOCK (buf.st_mode)) {
		g_free (socket_path);
		return NULL;
	}

	client = g_object_new (BEAGLE_TYPE_CLIENT, 0);
	priv = BEAGLE_CLIENT_GET_PRIVATE (client);
	priv->socket_path = socket_path;

	return client;
}

/**
 * beagle_client_new_from_socket_path:
 * @socket_path: a string of the path to the daemon socket
 *
 * Creates a new #BeagleClient, connecting to the path with @socket_path. NULL
 * is not allowed.
 *
 * Return value: a newly created #BeagleClient, or NULL if the client cannot be created.
 **/
BeagleClient *
beagle_client_new_from_socket_path (const char *socket_path)
{
	BeagleClient *client;
	BeagleClientPrivate *priv;
	struct stat buf;

	if (stat (socket_path, &buf) == -1 || !S_ISSOCK (buf.st_mode))
		return NULL;

	client = g_object_new (BEAGLE_TYPE_CLIENT, 0);
	priv = BEAGLE_CLIENT_GET_PRIVATE (client);
	priv->socket_path = g_strdup (socket_path);

	return client;
}

/**
 * beagle_client_send_request:
 * @client: a #BeagleClient
 * @request: a #BeagleRequest
 * @err: a location to return an error #GError of type #GIOChannelError.
 *
 * Synchronously send a #BeagleRequest using the given #BeagleClient. 
 *
 * Return value: a #BeagleResponse.
 **/
BeagleResponse *
beagle_client_send_request (BeagleClient   *client,
			    BeagleRequest  *request,
			    GError        **err)
{
	BeagleClientPrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_CLIENT (client), NULL);
	g_return_val_if_fail (BEAGLE_IS_REQUEST (request), NULL);

	priv = BEAGLE_CLIENT_GET_PRIVATE (client);

	return _beagle_request_send (request, priv->socket_path, err);
}

/**
 * beagle_client_send_request_async:
 * @client: a #BeagleClient
 * @request: a #BeagleRequest
 * @err: a location to store a #GError of type #GIOChannelError
 *
 * Asynchronously send a #BeagleRequest using the given #BeagleClient. 
 *
 * Return value: %TRUE on success and otherwise %FALSE.
 **/
gboolean 
beagle_client_send_request_async (BeagleClient   *client,
				  BeagleRequest  *request,
				  GError        **err)
{
	BeagleClientPrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_CLIENT (client), FALSE);
	g_return_val_if_fail (BEAGLE_IS_REQUEST (request), FALSE);

	priv = BEAGLE_CLIENT_GET_PRIVATE (client);

	return _beagle_request_send_async (request, priv->socket_path, err);
}

