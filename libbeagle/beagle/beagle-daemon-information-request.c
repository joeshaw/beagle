#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-private.h"
#include "beagle-daemon-information-request.h"
#include "beagle-daemon-information-response.h"

typedef struct {
	gint foo;
} BeagleDaemonInformationRequestPrivate;

#define BEAGLE_DAEMON_INFORMATION_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestPrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_daemon_information_request_to_xml (BeagleRequest *request, GError **err)
{
	GString *data = g_string_new (NULL);

	_beagle_request_append_standard_header (data, 
						"DaemonInformationRequest");
	_beagle_request_append_standard_footer (data);

	return data;
}

G_DEFINE_TYPE (BeagleDaemonInformationRequest, beagle_daemon_information_request, BEAGLE_TYPE_REQUEST)

static void
beagle_daemon_information_request_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize) {
		G_OBJECT_CLASS (parent_class)->finalize (obj);
	}
}

static void
beagle_daemon_information_request_class_init (BeagleDaemonInformationRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_daemon_information_request_finalize;
	request_class->to_xml = beagle_daemon_information_request_to_xml;

	g_type_class_add_private (klass, sizeof (BeagleDaemonInformationRequestPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "DaemonInformationResponse",
						  BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE,
						  NULL);
}

static void
beagle_daemon_information_request_init (BeagleDaemonInformationRequest *daemon_information_request)
{
}

/**
 * beagle_daemon_information_request_new:
 *
 * Creates a new #BeagleDaemonInformationRequest.
 *
 * Return value: a newly created #BeagleDaemonInformationRequest.
 **/
BeagleDaemonInformationRequest *
beagle_daemon_information_request_new (void)
{
	BeagleDaemonInformationRequest *daemon_information_request = g_object_new (BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, 0);

	return daemon_information_request;
}
