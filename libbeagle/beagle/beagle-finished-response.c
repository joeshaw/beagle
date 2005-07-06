#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-finished-response.h"

typedef struct {
	gint foo;
} BeagleFinishedResponsePrivate;

#define BEAGLE_FINISHED_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_FINISHED_RESPONSE, BeagleFinishedResponsePrivate))

static BeagleResponseClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleFinishedResponse, beagle_finished_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_finished_response_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_finished_response_class_init (BeagleFinishedResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_finished_response_finalize;


	g_type_class_add_private (klass, sizeof (BeagleFinishedResponsePrivate));
}

static void
beagle_finished_response_init (BeagleFinishedResponse *response)
{
}	
