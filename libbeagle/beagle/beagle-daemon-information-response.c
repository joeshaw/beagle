/*
 * beagle-daemon-information-response.c
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
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-daemon-information-response.h"
#include "beagle-private.h"

typedef struct {
	char *version;
	char *index_information;
	char *status;
} BeagleDaemonInformationResponsePrivate;

#define BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponsePrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleDaemonInformationResponse, beagle_daemon_information_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_daemon_information_response_finalize (GObject *obj)
{
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (obj);

	g_free (priv->version);
	g_free (priv->index_information);
	g_free (priv->status);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}


static void
end_version (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	priv->version = _beagle_parser_context_get_text_buffer (ctx);
}


static void
end_human_readable_status (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	priv->status = _beagle_parser_context_get_text_buffer (ctx);
}


static void
end_index_information (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	priv->index_information = _beagle_parser_context_get_text_buffer (ctx);
}

enum {
	PARSER_STATE_DAEMON_INFORMATION_VERSION,
	PARSER_STATE_DAEMON_INFORMATION_HUMAN_READABLE_STATUS,
	PARSER_STATE_DAEMON_INFORMATION_INDEX_INFORMATION,
};

static BeagleParserHandler parser_handlers[] = {
	{ "Version",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_VERSION,
	  NULL,
	  end_version },

	{ "HumanReadableStatus",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_HUMAN_READABLE_STATUS,
	  NULL,
	  end_human_readable_status },
	
	{ "IndexInformation",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_INDEX_INFORMATION,
	  NULL,
	  end_index_information },

	{ 0 }
};

static void
beagle_daemon_information_response_class_init (BeagleDaemonInformationResponseClass *klass)
{
	GObjectClass        *obj_class = G_OBJECT_CLASS (klass);
	BeagleResponseClass *response_class = BEAGLE_RESPONSE_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_daemon_information_response_finalize;

	_beagle_response_class_set_parser_handlers (response_class,
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleDaemonInformationResponsePrivate));
}

static void
beagle_daemon_information_response_init (BeagleDaemonInformationResponse *info)
{
}

/**
 * beagle_daemon_information_response_get_version:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the version string of the given #BeagleDaemonInformationResponse.
 *
 * Return value: the version string of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_version (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	return priv->version;
}

/**
 * beagle_daemon_information_response_get_human_readable_status:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the status string of the given #BeagleDaemonInformationResponse.
 *
 * Return value: the status of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_human_readable_status (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	return priv->status;
}

/**
 * beagle_daemon_information_response_get_index_information:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the index information of the given #BeagleDaemonInformationResponse.
 *
 * Return value: the index information of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_index_information (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	return priv->index_information;
}
