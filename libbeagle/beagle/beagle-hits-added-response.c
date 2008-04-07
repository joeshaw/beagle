/*
 * beagle-hits-added-response.c
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

#include "beagle-hits-added-response.h"
#include "beagle-private.h"
#include "beagle-property.h"

typedef struct {
	BeagleHit *hit; /* Current hit */
	BeagleProperty *prop; /* Current property; */

	GSList *hits;    /* of BeagleHit */
	int num_matches; /* Actual number of matches in index */
} BeagleHitsAddedResponsePrivate;

#define BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_HITS_ADDED_RESPONSE, BeagleHitsAddedResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleHitsAddedResponse, beagle_hits_added_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_hits_added_response_finalize (GObject *obj)
{
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (obj);

	_beagle_hit_list_free (priv->hits);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
end_num_matches (BeagleParserContext *ctx)
{
	BeagleHitsAddedResponse *response = BEAGLE_HITS_ADDED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);

	char *buf;
	buf = _beagle_parser_context_get_text_buffer (ctx);

	priv->num_matches = (int) g_ascii_strtod (buf, NULL);

	g_free (buf);
}

static void
start_hit (BeagleParserContext *ctx, const char **attrs)
{
	BeagleHitsAddedResponse *response = BEAGLE_HITS_ADDED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);

	int i;
	
	priv->hit = _beagle_hit_new ();

	for (i = 0; attrs[i] != NULL; i += 2) {
		if (strcmp (attrs[i], "Uri") == 0) 
			priv->hit->uri = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "ParentUri") == 0)
			priv->hit->parent_uri = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "Timestamp") == 0)
			priv->hit->timestamp = beagle_timestamp_new_from_string (attrs[i + 1]);
		else if (strcmp (attrs[i], "Score") == 0)
			priv->hit->score = g_ascii_strtod (attrs[i + 1], NULL);

		else
			g_warning ("unknown attribute \"%s\" with value \"%s\"", attrs[i], attrs[i + 1]);
	}
}

static void
end_hit (BeagleParserContext *ctx)
{
	BeagleHitsAddedResponse *response = BEAGLE_HITS_ADDED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);

	priv->hits = g_slist_prepend (priv->hits, priv->hit);
	priv->hit = NULL;
}

static void
start_property (BeagleParserContext *ctx, const char **attrs)
{
	BeagleHitsAddedResponse *response = BEAGLE_HITS_ADDED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);
	BeaglePropertyType type = BEAGLE_PROPERTY_TYPE_UNKNOWN;
	const char *key = NULL, *value = NULL;
	gboolean is_mutable = FALSE, is_searched = FALSE, is_stored = FALSE, is_persistent = TRUE;
	int i;
	
	for (i = 0; attrs[i] != NULL; i += 2) {
		if (strcmp (attrs[i], "Key") == 0)
			key = attrs[i + 1];
		else if (strcmp (attrs[i], "Value") == 0)
			value = attrs[i + 1];
		else if (strcmp (attrs[i], "IsMutable") == 0)
			is_mutable = strcmp (attrs[i + 1], "true") == 0;
		else if (strcmp (attrs[i], "IsSearched") == 0)
			is_searched = strcmp (attrs[i + 1], "true") == 0;
		else if (strcmp (attrs[i], "IsStored") == 0)
			is_stored = strcmp (attrs[i + 1], "true") == 0;
		else if (strcmp (attrs[i], "IsPersistent") == 0)
			is_persistent = strcmp (attrs[i + 1], "true") == 0;
		else if (strcmp (attrs[i], "Type") == 0) {
		        if (strcmp (attrs [i + 1], "Text") == 0)
			        type = BEAGLE_PROPERTY_TYPE_TEXT;
		        else if (strcmp (attrs [i + 1], "Keyword") == 0)
			        type = BEAGLE_PROPERTY_TYPE_KEYWORD;
			else if (strcmp (attrs [i + 1], "Date") == 0)
			        type = BEAGLE_PROPERTY_TYPE_DATE;
		} else
			g_warning ("could not handle %s", attrs[i]);
	}

	if (!key || !value) {
		g_warning ("key or value was null");
		return;
	}

	priv->prop = beagle_property_new (type, key, value);
	priv->prop->is_mutable = is_mutable;
	priv->prop->is_searched = is_searched;
	priv->prop->is_persistent = is_persistent;
}

static void
end_property (BeagleParserContext *ctx)
{
	BeagleHitsAddedResponse *response = BEAGLE_HITS_ADDED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);

	_beagle_hit_add_property (priv->hit, priv->prop);
	priv->prop = NULL;
}

enum {
	PARSER_STATE_NUM_MATCHES,
	PARSER_STATE_HITS,
	PARSER_STATE_HIT,
	PARSER_STATE_PROPERTIES,
	PARSER_STATE_PROPERTY
};

static BeagleParserHandler parser_handlers[] = {
	{ "NumMatches",
	  -1,
	  PARSER_STATE_NUM_MATCHES,
	  NULL,
	  end_num_matches},

	{ "Hits",
	  -1,
	  PARSER_STATE_HITS,
	  NULL, 
	  NULL },

	{ "Hit",
	  PARSER_STATE_HITS,
	  PARSER_STATE_HIT,
	  start_hit,
	  end_hit },

	{ "Properties",
	  PARSER_STATE_HIT,
	  PARSER_STATE_PROPERTIES,
	  NULL,
	  NULL },

	{ "Property",
	  PARSER_STATE_PROPERTIES,
	  PARSER_STATE_PROPERTY,
	  start_property,
	  end_property },

	{ 0 }
};

static void
beagle_hits_added_response_class_init (BeagleHitsAddedResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_hits_added_response_finalize;

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleHitsAddedResponsePrivate));
}

static void
beagle_hits_added_response_init (BeagleHitsAddedResponse *response)
{
	BeagleHitsAddedResponsePrivate *priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);
	priv->hit = NULL;
	priv->prop = NULL;
	priv->hits = NULL;
	priv->num_matches = 0;
}

/**
 * beagle_hits_added_response_get_hits:
 * @response: a #BeagleHitsAddedResponse
 *
 * Fetches the hits from the given #BeagleHitsAddedResponse. The list should not be modified or freed.
 *
 * Return value: A list of #BeagleHit.
 **/
GSList *
beagle_hits_added_response_get_hits (BeagleHitsAddedResponse *response)
{
	BeagleHitsAddedResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_HITS_ADDED_RESPONSE (response), NULL);

	priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);

	return priv->hits;
}

/**
 * beagle_hits_added_response_get_num_matches
 * @response: a #BeagleHitsAddedResponse
 *
 * Fetches the total of matches for this query from the given #BeagleHitsAddedResponse. The actual number of results returned is set by max-hits in #BeagleQuery.
 *
 * Return value: Total number of actual matches.
 **/
int
beagle_hits_added_response_get_num_matches (BeagleHitsAddedResponse *response)
{
	BeagleHitsAddedResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_HITS_ADDED_RESPONSE (response), -1);
	priv = BEAGLE_HITS_ADDED_RESPONSE_GET_PRIVATE (response);
	g_return_val_if_fail (priv != NULL, -1);

	return priv->num_matches;
}

