#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-cancelled-response.h"

typedef struct {
	gint foo;
} BeagleCancelledResponsePrivate;

#define BEAGLE_CANCELLED_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_CANCELLED_RESPONSE, BeagleCancelledResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleCancelledResponse, beagle_cancelled_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_cancelled_response_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_cancelled_response_class_init (BeagleCancelledResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_cancelled_response_finalize;


	g_type_class_add_private (klass, sizeof (BeagleCancelledResponsePrivate));
}

static void
beagle_cancelled_response_init (BeagleCancelledResponse *response)
{
}	
