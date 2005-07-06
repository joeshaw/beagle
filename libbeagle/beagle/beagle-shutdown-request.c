#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-private.h"
#include "beagle-shutdown-request.h"
#include "beagle-empty-response.h"

typedef struct {
	gint foo;
} BeagleShutdownRequestPrivate;

#define BEAGLE_SHUTDOWN_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequestPrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_shutdown_request_to_xml (BeagleRequest *request, GError **err)
{
	GString *data = g_string_new (NULL);

	_beagle_request_append_standard_header (data, "ShutdownRequest");
	_beagle_request_append_standard_footer (data);

	return data;
}

G_DEFINE_TYPE (BeagleShutdownRequest, beagle_shutdown_request, BEAGLE_TYPE_REQUEST)

static void
beagle_shutdown_request_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_shutdown_request_class_init (BeagleShutdownRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_shutdown_request_finalize;
	request_class->to_xml = beagle_shutdown_request_to_xml;

	g_type_class_add_private (klass, sizeof (BeagleShutdownRequestPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "EmptyResponse",
						  BEAGLE_TYPE_EMPTY_RESPONSE,
						  NULL);
}

static void
beagle_shutdown_request_init (BeagleShutdownRequest *shutdown_request)
{
}

/**
 * beagle_shutdown_request_new:
 *
 * Creates a new #BeagleShutdownRequest.
 *
 * Return value: the newly created #BeagleShutdownRequest.
 **/
BeagleShutdownRequest *
beagle_shutdown_request_new (void)
{
	BeagleShutdownRequest *shutdown_request = g_object_new (BEAGLE_TYPE_SHUTDOWN_REQUEST, 0);

	return shutdown_request;
}
