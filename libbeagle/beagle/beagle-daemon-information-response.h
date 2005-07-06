#ifndef __BEAGLE_DAEMON_INFORMATION_RESPONSE_H
#define __BEAGLE_DAEMON_INFORMATION_RESPONSE_H

#include <glib-object.h>

#include "beagle-response.h"

#define BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE            (beagle_daemon_information_response_get_type ())
#define BEAGLE_DAEMON_INFORMATION_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponse))
#define BEAGLE_DAEMON_INFORMATION_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponseClass))
#define BEAGLE_IS_DAEMON_INFORMATION_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE))
#define BEAGLE_IS_DAEMON_INFORMATION_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE))
#define BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleDaemonInformationResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleDaemonInformationResponseClass;

GType        beagle_daemon_information_response_get_type     (void);

G_CONST_RETURN char *
beagle_daemon_information_response_get_version (BeagleDaemonInformationResponse *response);

G_CONST_RETURN char *
beagle_daemon_information_response_get_human_readable_status (BeagleDaemonInformationResponse *response);

G_CONST_RETURN char *
beagle_daemon_information_response_get_index_information (BeagleDaemonInformationResponse *response);

#endif /* __BEAGLE_DAEMON_INFORMATION_RESPONSE_H */
