/*
 * beagle-error-response.c
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

#include "beagle-error-response.h"
#include "beagle-private.h"
#include "beagle-util.h"

typedef struct {
	char *message;
	char *details;
} BeagleErrorResponsePrivate;

#define BEAGLE_ERROR_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_ERROR_RESPONSE, BeagleErrorResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleErrorResponse, beagle_error_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_error_response_finalize (GObject *obj)
{
	BeagleErrorResponsePrivate *priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (obj);

	g_free (priv->message);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
end_error_message (BeagleParserContext *ctx)
{
	BeagleErrorResponse *response = BEAGLE_ERROR_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleErrorResponsePrivate *priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (response);

	priv->message = _beagle_parser_context_get_text_buffer (ctx);
}

static void
end_error_details (BeagleParserContext *ctx)
{
	BeagleErrorResponse *response = BEAGLE_ERROR_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleErrorResponsePrivate *priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (response);

	priv->details = _beagle_parser_context_get_text_buffer (ctx);
}

enum {
	PARSER_STATE_MESSAGE,
	PARSER_STATE_DETAILS,
};

static BeagleParserHandler parser_handlers[] = {
	{ "Message",
	  -1,
	  PARSER_STATE_MESSAGE,
	  NULL,
	  end_error_message },

	{ "Details",
	  -1,
	  PARSER_STATE_DETAILS,
	  NULL,
	  end_error_details },

	{ 0 }
};

static void
beagle_error_response_class_init (BeagleErrorResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_error_response_finalize;

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleErrorResponsePrivate));
}

static void
beagle_error_response_init (BeagleErrorResponse *response)
{
}	

/**
 * beagle_error_response_get_message:
 * @response: a #BeagleErrorResponse
 *
 * Get the message from given #BeagleErrorResponse.
 *
 * Return value: the error message from the #BeagleErrorResponse.
 **/
const char *
beagle_error_response_get_message (BeagleErrorResponse *response)
{
	BeagleErrorResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_ERROR_RESPONSE (response), NULL);

	priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (response);

	return priv->message;
}

/**
 * beagle_error_response_get_details:
 * @response: a #BeagleErrorResponse
 *
 * Gets the error's details from given #BeagleErrorResponse.
 *
 * Return value: the error details from the #BeagleErrorResponse.
 **/
const char *
beagle_error_response_get_details (BeagleErrorResponse *response)
{
	BeagleErrorResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_ERROR_RESPONSE (response), NULL);

	priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (response);

	return priv->details;
}


void
_beagle_error_response_to_g_error (BeagleErrorResponse *response,
				   GError **error)
{
	BeagleErrorResponsePrivate *priv;

	priv = BEAGLE_ERROR_RESPONSE_GET_PRIVATE (response);

	g_set_error (error, BEAGLE_ERROR, BEAGLE_ERROR_DAEMON_ERROR,
		     "%s", priv->message);
}
