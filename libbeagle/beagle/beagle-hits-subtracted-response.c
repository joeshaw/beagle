#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-hits-subtracted-response.h"
#include "beagle-private.h"

typedef struct {
	GSList *uris; /* of char * */
} BeagleHitsSubtractedResponsePrivate;

#define BEAGLE_HITS_SUBTRACTED_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE, BeagleHitsSubtractedResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleHitsSubtractedResponse, beagle_hits_subtracted_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_hits_subtracted_response_finalize (GObject *obj)
{
	BeagleHitsSubtractedResponsePrivate *priv = BEAGLE_HITS_SUBTRACTED_RESPONSE_GET_PRIVATE (obj);

	g_slist_foreach (priv->uris, (GFunc)g_free, NULL);
	g_slist_free (priv->uris);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
end_uri (BeagleParserContext *ctx)
{
	BeagleHitsSubtractedResponse *response = BEAGLE_HITS_SUBTRACTED_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleHitsSubtractedResponsePrivate *priv = BEAGLE_HITS_SUBTRACTED_RESPONSE_GET_PRIVATE (response);

	priv->uris = g_slist_prepend (priv->uris, _beagle_parser_context_get_text_buffer (ctx));
}

enum {
	PARSER_STATE_URIS,
	PARSER_STATE_URI
};

static BeagleParserHandler parser_handlers[] = {
	{ "Uris",
	  -1,
	  PARSER_STATE_URIS,
	  NULL, 
	  NULL },
	{ "Uri",
	  PARSER_STATE_URIS,
	  PARSER_STATE_URI,
	  NULL,
	  end_uri },
	{ 0 }
};

static void
beagle_hits_subtracted_response_class_init (BeagleHitsSubtractedResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_hits_subtracted_response_finalize;

	_beagle_response_class_set_parser_handlers (BEAGLE_RESPONSE_CLASS (klass),
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleHitsSubtractedResponsePrivate));
}

static void
beagle_hits_subtracted_response_init (BeagleHitsSubtractedResponse *response)
{
}	

/**
 * beagle_hits_subtracted_response_get_uris:
 * @response: a #BeagleHitsSubtractedResponse
 *
 * Fetches the list of hit uris contained in the given #BeagleHitsSubtractedResponse. The list should not be modified or freed.
 *
 * Return value: A list of uri strings.
 **/
GSList *
beagle_hits_subtracted_response_get_uris (BeagleHitsSubtractedResponse *response)
{
	BeagleHitsSubtractedResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_HITS_SUBTRACTED_RESPONSE (response), NULL);

	priv = BEAGLE_HITS_SUBTRACTED_RESPONSE_GET_PRIVATE (response);

	return priv->uris;
}

