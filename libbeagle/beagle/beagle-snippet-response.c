/*
 * beagle-snippet-response.c
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

#include "beagle-snippet-response.h"
#include "beagle-private.h"

typedef struct {
	char *snippet;
} BeagleSnippetResponsePrivate;

#define BEAGLE_SNIPPET_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_SNIPPET_RESPONSE, BeagleSnippetResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleSnippetResponse, beagle_snippet_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_snippet_response_finalize (GObject *obj)
{
	BeagleSnippetResponsePrivate *priv = BEAGLE_SNIPPET_RESPONSE_GET_PRIVATE (obj);

	g_free (priv->snippet);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
end_snippet (BeagleParserContext *ctx)
{
	BeagleSnippetResponse *response = BEAGLE_SNIPPET_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleSnippetResponsePrivate *priv = BEAGLE_SNIPPET_RESPONSE_GET_PRIVATE (response);
	
	priv->snippet = _beagle_parser_context_get_text_buffer (ctx);
}

enum {
	PARSER_STATE_SNIPPET
};

static BeagleParserHandler parser_handlers[] = {
	{ "Snippet",
	  -1,
	  PARSER_STATE_SNIPPET,
	  NULL,
	  end_snippet },
	{ 0 }
};

static void
beagle_snippet_response_class_init (BeagleSnippetResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_snippet_response_finalize;

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleSnippetResponsePrivate));
}

static void
beagle_snippet_response_init (BeagleSnippetResponse *response)
{
}	

/**
 * beagle_snippet_response_get_snippet:
 * @response: a #BeagleSnippetResponse
 *
 * Fetches the snippet from the given #BeagleSnippetResponse.
 *
 * Return value: the snippet string from the #BeagleSnippetResponse.
 **/
G_CONST_RETURN char *
beagle_snippet_response_get_snippet (BeagleSnippetResponse *response)
{
	BeagleSnippetResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_SNIPPET_RESPONSE (response), NULL);

	priv = BEAGLE_SNIPPET_RESPONSE_GET_PRIVATE (response);
	
	return priv->snippet;
}
