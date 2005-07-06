#ifndef __BEAGLE_DAEMON_INFORMATION_REQUEST_H
#define __BEAGLE_DAEMON_INFORMATION_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-request.h>

#define BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST            (beagle_daemon_information_request_get_type ())
#define BEAGLE_DAEMON_INFORMATION_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequest))
#define BEAGLE_DAEMON_INFORMATION_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestClass))
#define BEAGLE_IS_DAEMON_INFORMATION_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST))
#define BEAGLE_IS_DAEMON_INFORMATION_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST))
#define BEAGLE_DAEMON_INFORMATION_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestClass))

typedef struct {
	BeagleRequest parent;
} BeagleDaemonInformationRequest;

typedef struct {
	BeagleRequestClass parent_class;
} BeagleDaemonInformationRequestClass;

GType        beagle_daemon_information_request_get_type     (void);
BeagleDaemonInformationRequest *beagle_daemon_information_request_new          (void);

#endif /* __BEAGLE_DAEMON_INFORMATION_REQUEST_H */
