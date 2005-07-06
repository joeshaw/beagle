/*
 * beagle-snippet-request.c
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

#include "beagle-private.h"
#include "beagle-snippet-request.h"
#include "beagle-snippet-response.h"

typedef struct {
	GSList *query_terms;
	BeagleHit *hit;
} BeagleSnippetRequestPrivate;

#define BEAGLE_SNIPPET_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequestPrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_snippet_request_to_xml (BeagleRequest *request, GError **err)
{
	BeagleSnippetRequestPrivate *priv = BEAGLE_SNIPPET_REQUEST_GET_PRIVATE (request);
	GString *data;
	GSList *list;

	g_return_val_if_fail (priv->query_terms != NULL, NULL);
	g_return_val_if_fail (priv->hit != NULL, NULL);

	data = g_string_new (NULL);

	_beagle_request_append_standard_header (data, "SnippetRequest");

	_beagle_hit_to_xml (priv->hit, data);
	
	g_string_append (data, "<QueryTerms>");
	for (list = priv->query_terms; list != NULL; list = list->next) {
		char *term = list->data;

		g_string_append_printf (data, "<string>%s</string>", term);
	}
	g_string_append (data, "</QueryTerms>");

	_beagle_request_append_standard_footer (data);

	return data;
}

G_DEFINE_TYPE (BeagleSnippetRequest, beagle_snippet_request, BEAGLE_TYPE_REQUEST)

static void
beagle_snippet_request_finalize (GObject *obj)
{
	BeagleSnippetRequestPrivate *priv = BEAGLE_SNIPPET_REQUEST_GET_PRIVATE (obj);

	if (priv->hit)
		beagle_hit_unref (priv->hit);

	g_slist_foreach (priv->query_terms, (GFunc)g_free, NULL);
	g_slist_free (priv->query_terms);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_snippet_request_class_init (BeagleSnippetRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_snippet_request_finalize;
	request_class->to_xml = beagle_snippet_request_to_xml;

	g_type_class_add_private (klass, sizeof (BeagleSnippetRequestPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "SnippetResponse",
						  BEAGLE_TYPE_SNIPPET_RESPONSE,
						  NULL);
}

static void
beagle_snippet_request_init (BeagleSnippetRequest *snippet_request)
{
}

/**
 * beagle_snippet_request_new:
 *
 * Creates a new #BeagleSnippetRequest.
 *
 * Return value: the newly created #BeagleSnippetRequest.
 **/
BeagleSnippetRequest *
beagle_snippet_request_new (void)
{
	BeagleSnippetRequest *snippet_request = g_object_new (BEAGLE_TYPE_SNIPPET_REQUEST, 0);

	return snippet_request;
}

/**
 * beagle_snippet_request_set_hit:
 * @request: a #BeagleSnippetRequest
 * @hit: a #BeagleHit
 *
 * Sets the @hit on the given #BeagleSnippetRequest.
 **/
void
beagle_snippet_request_set_hit (BeagleSnippetRequest *request,
				BeagleHit *hit)
{
	BeagleSnippetRequestPrivate *priv;

	g_return_if_fail (BEAGLE_IS_SNIPPET_REQUEST (request));
	g_return_if_fail (hit != NULL);

	priv = BEAGLE_SNIPPET_REQUEST_GET_PRIVATE (request);

	beagle_hit_ref (hit);

	if (priv->hit)
		beagle_hit_unref (priv->hit);

	priv->hit = hit;
}

/**
 * beagle_snippet_request_add_query_term:
 * @request: a #BeagleSnippetRequest
 * @text: a string
 *
 * Add a query term to the given #BeagleSnippetRequest.
 **/
void
beagle_snippet_request_add_query_term (BeagleSnippetRequest *request,
				       const char           *text)
{
	BeagleSnippetRequestPrivate *priv;

	g_return_if_fail (BEAGLE_IS_SNIPPET_REQUEST (request));
	g_return_if_fail (text != NULL);

	priv = BEAGLE_SNIPPET_REQUEST_GET_PRIVATE (request);

	priv->query_terms = g_slist_prepend (priv->query_terms, g_strdup (text));
}

/**
 * beagle_snippet_request_set_query_terms_from_query:
 * @request: a #BeagleSnippetRequest
 * @query: a #BeagleQuery
 *
 * Set the query terms of the given #BeagleSnippetRequest from the passed in #BeagleQuery.
 **/
void 
beagle_snippet_request_set_query_terms_from_query (BeagleSnippetRequest *request,
						   BeagleQuery          *query);
