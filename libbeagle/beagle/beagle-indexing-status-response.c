/*
 * beagle-indexing-status-response.c
 *
 * Copyright (C) 2006 Novell, Inc.
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

#include "beagle-indexing-status-response.h"
#include "beagle-private.h"

typedef struct {
	gboolean is_indexing;
} BeagleIndexingStatusResponsePrivate;

#define BEAGLE_INDEXING_STATUS_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_INDEXING_STATUS_RESPONSE, BeagleIndexingStatusResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleIndexingStatusResponse, beagle_indexing_status_response, BEAGLE_TYPE_RESPONSE)

static void
end_status (BeagleParserContext *ctx)
{
	BeagleIndexingStatusResponse *response = BEAGLE_INDEXING_STATUS_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleIndexingStatusResponsePrivate *priv = BEAGLE_INDEXING_STATUS_RESPONSE_GET_PRIVATE (response);

	char *buf;
	buf = _beagle_parser_context_get_text_buffer (ctx);

	if (strcmp (buf, "Running") == 0)
		priv->is_indexing = TRUE;
	else if (strcmp (buf, "NotRunning") == 0)
		priv->is_indexing = FALSE;
	else {
		g_warning ("Unknown value for indexing status: %s", buf);
		priv->is_indexing = FALSE;
	}

	g_free (buf);
}

enum {
	PARSER_STATE_STATUS,
};

static BeagleParserHandler parser_handlers[] = {
	{ "Status",
	  -1,
	  PARSER_STATE_STATUS,
	  NULL,
	  end_status},

	{ 0 }
};

static void
beagle_indexing_status_response_class_init (BeagleIndexingStatusResponseClass *klass)
{
	parent_class = g_type_class_peek_parent (klass);

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleIndexingStatusResponsePrivate));
}

static void
beagle_indexing_status_response_init (BeagleIndexingStatusResponse *response)
{
	BeagleIndexingStatusResponsePrivate *priv = BEAGLE_INDEXING_STATUS_RESPONSE_GET_PRIVATE (response);
	priv->is_indexing = FALSE;
}

/**
 * beagle_indexing_status_response_is_indexing
 * @response: a #BeagleIndexingStatusResponse
 *
 * Returns whether or not the daemon is running a large indexing process.  This
 * will be TRUE if a long crawling process is running, but will return FALSE if
 * it's just indexing one or two documents.
 *
 * Return value: TRUE if indexing, FALSE if not.
 **/
int
beagle_indexing_status_response_is_indexing (BeagleIndexingStatusResponse *response)
{
	BeagleIndexingStatusResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_INDEXING_STATUS_RESPONSE (response), -1);
	priv = BEAGLE_INDEXING_STATUS_RESPONSE_GET_PRIVATE (response);
	g_return_val_if_fail (priv != NULL, -1);

	return priv->is_indexing;
}

