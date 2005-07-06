#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-empty-response.h"
#include "beagle-private.h"

typedef struct {
	int foo;
} BeagleEmptyResponsePrivate;

#define BEAGLE_EMPTY_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_EMPTY_RESPONSE, BeagleEmptyResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleEmptyResponse, beagle_empty_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_empty_response_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize) {
		G_OBJECT_CLASS (parent_class)->finalize (obj);
	}
}

static void
beagle_empty_response_class_init (BeagleEmptyResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_empty_response_finalize;

	g_type_class_add_private (klass, sizeof (BeagleEmptyResponsePrivate));
}

static void
beagle_empty_response_init (BeagleEmptyResponse *response)
{
}	
