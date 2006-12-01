/*
 * beagle-search-term-response.c
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

#include "beagle-search-term-response.h"
#include "beagle-private.h"
#include "beagle-property.h"

typedef struct {
	GSList *exact_text;   /* of string */
	GSList *stemmed_text; /* of string */

	GSList **current_list; /* points to one of the two above */
} BeagleSearchTermResponsePrivate;

#define BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_SEARCH_TERM_RESPONSE, BeagleSearchTermResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleSearchTermResponse, beagle_search_term_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_search_term_response_finalize (GObject *obj)
{
	BeagleSearchTermResponsePrivate *priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (obj);

	/*
	 * Note, we don't free the lists or their contents here!  This is
	 * because the BeagleSearchTermResponse is internal to BeagleQuery.
	 * This response never happens in reply to any other request, and
	 * the BeagleQuery gets these lists through the get_exact_text()
	 * and get_stemmed_text() functions below.  They take ownership
	 * of them.
	 */

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
start_exact (BeagleParserContext *ctx, const char **attrs)
{
	BeagleSearchTermResponse *response = BEAGLE_SEARCH_TERM_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleSearchTermResponsePrivate *priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (response);

	priv->current_list = &priv->exact_text;
}

static void
start_stemmed (BeagleParserContext *ctx, const char **attrs)
{
	BeagleSearchTermResponse *response = BEAGLE_SEARCH_TERM_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleSearchTermResponsePrivate *priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (response);

	priv->current_list = &priv->stemmed_text;
}

static void
end_text (BeagleParserContext *ctx)
{
	BeagleSearchTermResponse *response = BEAGLE_SEARCH_TERM_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleSearchTermResponsePrivate *priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (response);

	*priv->current_list = g_slist_append (*priv->current_list, _beagle_parser_context_get_text_buffer (ctx));
}

enum {
	PARSER_STATE_EXACT,
	PARSER_STATE_STEMMED,
	PARSER_STATE_TEXT,
};

static BeagleParserHandler parser_handlers[] = {
	{ "Exact",
	  -1,
	  PARSER_STATE_EXACT,
	  start_exact,
	  NULL },

	{ "Stemmed",
	  -1,
	  PARSER_STATE_STEMMED,
	  start_stemmed,
	  NULL },

	{ "Text",
	  PARSER_STATE_EXACT,
	  PARSER_STATE_TEXT,
	  NULL,
	  end_text },

	{ "Text",
	  PARSER_STATE_STEMMED,
	  PARSER_STATE_TEXT,
	  NULL,
	  end_text },

	{ 0 }
};

static void
beagle_search_term_response_class_init (BeagleSearchTermResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_search_term_response_finalize;

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleSearchTermResponsePrivate));
}

static void
beagle_search_term_response_init (BeagleSearchTermResponse *response)
{
}	

GSList *
_beagle_search_term_response_get_exact_text (BeagleSearchTermResponse *response)
{
	BeagleSearchTermResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_SEARCH_TERM_RESPONSE (response), NULL);

	priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (response);

	return priv->exact_text;
}

GSList *
_beagle_search_term_response_get_stemmed_text (BeagleSearchTermResponse *response)
{
	BeagleSearchTermResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_SEARCH_TERM_RESPONSE (response), NULL);

	priv = BEAGLE_SEARCH_TERM_RESPONSE_GET_PRIVATE (response);

	return priv->stemmed_text;
}

