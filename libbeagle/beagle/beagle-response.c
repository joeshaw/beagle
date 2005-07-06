#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-response.h"
#include "beagle-marshal.h"
#include "beagle-util.h"
#include "beagle-private.h"

typedef struct {
	int foo;
} BeagleResponsePrivate;

#define BEAGLE_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_RESPONSE, BeagleResponsePrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleResponse, beagle_response, G_TYPE_OBJECT)

static void
beagle_response_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_response_class_init (BeagleResponseClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_response_finalize;

	g_type_class_add_private (klass, sizeof (BeagleResponsePrivate));
}

static void
beagle_response_init (BeagleResponse *response)
{
}	

void
_beagle_response_class_set_parser_handlers (BeagleResponseClass *klass,
					    BeagleParserHandler *handlers)
{
	int i;

	for (i = 0; handlers[i].name != NULL; i++) {
		if (handlers[i].src_state != -1)
			handlers[i].src_state += _beagle_parser_state_index;

		handlers[i].dest_state += _beagle_parser_state_index;
	}

	_beagle_parser_state_index += i;

	klass->parser_handlers = handlers;
}

