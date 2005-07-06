#ifndef __BEAGLE_SHUTDOWN_REQUEST_H
#define __BEAGLE_SHUTDOWN_REQUEST_H

#include <glib-object.h>

#include "beagle-request.h"

#define BEAGLE_TYPE_SHUTDOWN_REQUEST            (beagle_shutdown_request_get_type ())
#define BEAGLE_SHUTDOWN_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequest))
#define BEAGLE_SHUTDOWN_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequestClass))
#define BEAGLE_IS_SHUTDOWN_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST))
#define BEAGLE_IS_SHUTDOWN_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_SHUTDOWN_REQUEST))
#define BEAGLE_SHUTDOWN_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequestClass))

typedef struct {
	BeagleRequest parent;
} BeagleShutdownRequest;

typedef struct {
	BeagleRequestClass parent_class;
} BeagleShutdownRequestClass;

GType        beagle_shutdown_request_get_type     (void);
BeagleShutdownRequest *beagle_shutdown_request_new          (void);

#endif /* __BEAGLE_SHUTDOWN_REQUEST_H */
