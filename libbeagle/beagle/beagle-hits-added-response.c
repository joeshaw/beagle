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

	GSList *hits; /* of BeagleHit */
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
		else if (strcmp (attrs[i], "Revision") == 0) 
			priv->hit->revision = strtol (attrs[i + 1], NULL, 10);
		else if (strcmp (attrs[i], "Id") == 0)
			priv->hit->id = strtol (attrs[i + 1], NULL, 10);
		else if (strcmp (attrs[i], "Type") == 0)
			priv->hit->type = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "MimeType") == 0)
			priv->hit->mime_type = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "Source") == 0)
			priv->hit->source = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "SourceObjectName") == 0)
			priv->hit->source_object_name = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "ScoreRaw") == 0)
			priv->hit->score_raw = g_ascii_strtod (attrs[i + 1], NULL);
		else if (strcmp (attrs[i], "ScoreMultiplier") == 0) 
			priv->hit->score_multiplier = g_ascii_strtod (attrs[i + 1], NULL);
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
	const char *key = NULL, *value = NULL;
	gboolean is_keyword = FALSE, is_searched = FALSE;
	int i;
	
	for (i = 0; attrs[i] != NULL; i += 2) {
		if (strcmp (attrs[i], "Key") == 0)
			key = attrs[i + 1];
		else if (strcmp (attrs[i], "Value") == 0)
			value = attrs[i + 1];
		else if (strcmp (attrs[i], "IsKeyword") == 0)
			is_keyword = strcmp (attrs[i + 1], "true") == 0;
		else if (strcmp (attrs[i], "IsSearched") == 0)
			is_searched = strcmp (attrs[i + 1], "true") == 0;
		else
			g_warning ("could not handle %s", attrs[i]);
	}

	if (!key || !value) {
		g_warning ("key or value was null");
		
		return;
	}

	priv->prop = beagle_property_new (key, value);
	priv->prop->is_keyword = is_keyword;
	priv->prop->is_searched = is_searched;
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
	PARSER_STATE_HITS,
	PARSER_STATE_HIT,
	PARSER_STATE_PROPERTIES,
	PARSER_STATE_PROPERTY
};

static BeagleParserHandler parser_handlers[] = {
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

