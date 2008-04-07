/*
 * beagle-query.c
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

#include "beagle-finished-response.h"
#include "beagle-hits-added-response.h"
#include "beagle-hits-subtracted-response.h"
#include "beagle-marshal.h"
#include "beagle-query.h"
#include "beagle-query-part.h"
#include "beagle-query-part-human.h"
#include "beagle-query-part-or.h"
#include "beagle-query-part-property.h"
#include "beagle-search-term-response.h"
#include "beagle-private.h"

typedef struct {
	GSList *parts;      /* of BeagleQueryPart */
	int max_hits;

	/* These are extracted from the BeagleSearchTermResponse */
	GSList *exact_text;   /* of string */
	GSList *stemmed_text; /* of string */

	BeagleQueryDomain domain;
} BeagleQueryPrivate;

#define BEAGLE_QUERY_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY, BeagleQueryPrivate))

enum {
	HITS_ADDED,
	HITS_SUBTRACTED,
	FINISHED,
	LAST_SIGNAL
};

static GObjectClass *parent_class = NULL;
static guint signals [LAST_SIGNAL] = { 0 };

static void
query_set_search_terms (BeagleQuery *query, BeagleSearchTermResponse *response)
{
	BeagleQueryPrivate *priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->exact_text = _beagle_search_term_response_get_exact_text (response);
	priv->stemmed_text = _beagle_search_term_response_get_stemmed_text (response);
}

static GString *
beagle_query_to_xml (BeagleRequest *request, GError **err)
{
	BeagleQueryPrivate *priv = BEAGLE_QUERY_GET_PRIVATE (request);
	GString *data = g_string_new (NULL);
	GString *sub_data;
	GSList *iter;
	gboolean first = TRUE;

	_beagle_request_append_standard_header (data, "Query");

	g_string_append_len (data, "<Parts>", 7);

	for (iter = priv->parts; iter != NULL; iter = iter->next) {
		BeagleQueryPart *part = (BeagleQueryPart *) iter->data;

		sub_data = _beagle_query_part_to_xml (part);
		g_string_append_len (data, sub_data->str, sub_data->len);
		g_string_free (sub_data, TRUE);
	}

	g_string_append_len (data, "</Parts>", 8);

	g_string_append_len (data, "<QueryDomain>", 13);

	if (priv->domain & BEAGLE_QUERY_DOMAIN_LOCAL) {
		g_string_append_len (data, "Local", 5);
		first = FALSE;
	}

	if (priv->domain & BEAGLE_QUERY_DOMAIN_SYSTEM) {
		if (!first)
			g_string_append_c (data, ' ');

		g_string_append_len (data, "System", 6);
		first = FALSE;
	}

	if (priv->domain & BEAGLE_QUERY_DOMAIN_NEIGHBORHOOD) {
		if (!first)
			g_string_append_c (data, ' ');

		g_string_append_len (data, "Neighborhood", 12);
		first = FALSE;
	}

	if (priv->domain & BEAGLE_QUERY_DOMAIN_GLOBAL) {
		if (!first)
			g_string_append_c (data, ' ');

		g_string_append_len (data, "Global", 6);
	}

	g_string_append_len (data, "</QueryDomain>", 14);

	g_string_append_printf (data, "<MaxHits>%d</MaxHits>", priv->max_hits);

	_beagle_request_append_standard_footer (data);

	return data;
}

static void
beagle_query_response (BeagleRequest *request, BeagleResponse *response)
{
	if (BEAGLE_IS_SEARCH_TERM_RESPONSE (response)) {
		query_set_search_terms (BEAGLE_QUERY (request),
					BEAGLE_SEARCH_TERM_RESPONSE (response));
	} else if (BEAGLE_IS_HITS_ADDED_RESPONSE (response))
		g_signal_emit (request, signals[HITS_ADDED], 0, response);
	else if (BEAGLE_IS_HITS_SUBTRACTED_RESPONSE (response))
		g_signal_emit (request, signals[HITS_SUBTRACTED], 0, response);
	else if (BEAGLE_IS_FINISHED_RESPONSE (response))
		g_signal_emit (request, signals[FINISHED], 0, response);

}


G_DEFINE_TYPE (BeagleQuery, beagle_query, BEAGLE_TYPE_REQUEST)

static void
beagle_query_finalize (GObject *obj)
{
	BeagleQueryPrivate *priv = BEAGLE_QUERY_GET_PRIVATE (obj);

	g_slist_foreach (priv->parts, (GFunc) g_object_unref, NULL);
	g_slist_free (priv->parts);

	g_slist_foreach (priv->exact_text, (GFunc) g_free, NULL);
	g_slist_free (priv->exact_text);

	g_slist_foreach (priv->stemmed_text, (GFunc) g_free, NULL);
	g_slist_free (priv->stemmed_text);


	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_class_init (BeagleQueryClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);
	
	obj_class->finalize = beagle_query_finalize;
	request_class->to_xml = beagle_query_to_xml;

	request_class->response = beagle_query_response;

	signals [HITS_ADDED] =
		g_signal_new ("hits_added",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      G_STRUCT_OFFSET (BeagleQueryClass, hits_added),
			      NULL, NULL,
			      g_cclosure_marshal_VOID__OBJECT,
			      G_TYPE_NONE, 1,
			      BEAGLE_TYPE_HITS_ADDED_RESPONSE);

	signals [HITS_SUBTRACTED] =
		g_signal_new ("hits_subtracted",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      G_STRUCT_OFFSET (BeagleQueryClass, hits_subtracted),
			      NULL, NULL,
			      g_cclosure_marshal_VOID__OBJECT,
			      G_TYPE_NONE, 1,
			      BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE);

	signals [FINISHED] =
		g_signal_new ("finished",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      G_STRUCT_OFFSET (BeagleQueryClass, finished),
			      NULL, NULL,
			      g_cclosure_marshal_VOID__OBJECT,
			      G_TYPE_NONE, 1,
			      BEAGLE_TYPE_FINISHED_RESPONSE);

	g_type_class_add_private (klass, sizeof (BeagleQueryPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "HitsAddedResponse",
						  BEAGLE_TYPE_HITS_ADDED_RESPONSE,
						  "HitsSubtractedResponse",
						  BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE,
						  "FinishedResponse",
						  BEAGLE_TYPE_FINISHED_RESPONSE,
						  "SearchTermResponse",
						  BEAGLE_TYPE_SEARCH_TERM_RESPONSE,
						  NULL);
}

static void
beagle_query_init (BeagleQuery *query)
{
	BeagleQueryPrivate *priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->max_hits = 100;

	/* FIXME: This is a good default when on an airplane. */
	priv->domain = BEAGLE_QUERY_DOMAIN_LOCAL | BEAGLE_QUERY_DOMAIN_SYSTEM;

	priv->parts = NULL;
	priv->stemmed_text = NULL;
	priv->exact_text = NULL;
}

/**
 * beagle_query_new:
 *
 * Creates a new #BeagleQuery.
 *
 * Return value: the newly created #BeagleQuery.
 **/
BeagleQuery *
beagle_query_new (void)
{
	BeagleQuery *query = g_object_new (BEAGLE_TYPE_QUERY, 0);

	return query;
}

/**
 * beagle_query_add_part:
 * @query: a #BeagleQuery
 * @part: a #BeagleQueryPart
 *
 * Adds a #BeagleQueryPart to the given #BeagleQuery.
 **/
void
beagle_query_add_part (BeagleQuery *query, BeagleQueryPart *part)
{
	BeagleQueryPrivate *priv = BEAGLE_QUERY_GET_PRIVATE (query);

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	priv->parts = g_slist_append (priv->parts, part);
}

/**
 * beagle_query_add_text:
 * @query: a #BeagleQuery
 * @str: a string
 *
 * Adds a text part to the given #BeagleQuery.
 **/
void
beagle_query_add_text (BeagleQuery *query, const char *str)
{
	BeagleQueryPartHuman *part;

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	part = beagle_query_part_human_new ();
	beagle_query_part_human_set_string (part, str);
	beagle_query_part_set_logic (BEAGLE_QUERY_PART (part),
				     BEAGLE_QUERY_PART_LOGIC_REQUIRED);

	beagle_query_add_part (query, BEAGLE_QUERY_PART (part));
}


/**
 * beagle_query_set_domain:
 * @query: a #BeagleQuery
 * @domain: a #BeagleQueryDomain
 *
 * Sets the search domain for a given #BeagleQuery.  This limits the scope of
 * a search to certain backends.
 **/
void
beagle_query_set_domain (BeagleQuery *query,
			 BeagleQueryDomain domain)
{
	BeagleQueryPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->domain = domain;
}

/**
 * beagle_query_add_domain:
 * @query: a #BeagleQuery
 * @domain: a #BeagleQueryDomain
 *
 * Adds a search domain to the list of domains to search.
 **/
void
beagle_query_add_domain (BeagleQuery *query,
			 BeagleQueryDomain domain)
{
	BeagleQueryPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->domain |= domain;
}

/**
 * beagle_query_remove_domain:
 * @query: a #BeagleQuery
 * @domain: a #BeagleQueryDomain
 *
 * Removes a search domain.
 **/
void
beagle_query_remove_domain (BeagleQuery *query,
			    BeagleQueryDomain domain)
{
	BeagleQueryPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->domain &= ~domain;
}

/**
 * beagle_query_set_max_hits
 * @query: a #BeagleQuery
 * @max_hits: Max number of hits
 *
 * Sets the max number of hits a given #BeagleQuery should return.
 **/
void
beagle_query_set_max_hits (BeagleQuery *query,
			   int max_hits)
{
	BeagleQueryPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY (query));

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	priv->max_hits = max_hits;
}


/**
 * beagle_query_get_max_hits
 * @query: a #BeagleQuery
 *
 * Returns the max number of hits a given #BeagleQuery should return.
 *
 * Return value: Max number of hits
 **/
int
beagle_query_get_max_hits (BeagleQuery *query)
{
	BeagleQueryPrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_QUERY (query), 0);

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	return priv->max_hits;
}

/**
 * beagle_query_get_exact_text
 * @query: a #BeagleQuery
 *
 * Returns a list of strings which contain the exact text processed by the
 * query.  The list should not be modified or freed.
 *
 * Return value: A list of strings containing the exact text
 **/
GSList *
beagle_query_get_exact_text (BeagleQuery *query)
{
	BeagleQueryPrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_QUERY (query), 0);

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	return priv->exact_text;
}

/**
 * beagle_query_get_stemmed_text
 * @query: a #BeagleQuery
 *
 * Returns a list of strings which contain the stemmed text processed by the
 * query.  The list should not be modified or freed.
 *
 * Return value: A list of strings containing the stemmed text
 **/
GSList *
beagle_query_get_stemmed_text (BeagleQuery *query)
{
	BeagleQueryPrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_QUERY (query), 0);

	priv = BEAGLE_QUERY_GET_PRIVATE (query);

	return priv->stemmed_text;
}
