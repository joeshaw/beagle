#ifndef __BEAGLE_CANCELLED_RESPONSE_H
#define __BEAGLE_CANCELLED_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"

#define BEAGLE_TYPE_CANCELLED_RESPONSE            (beagle_cancelled_response_get_type ())
#define BEAGLE_CANCELLED_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_CANCELLED_RESPONSE, BeagleCancelledResponse))
#define BEAGLE_CANCELLED_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_CANCELLED_RESPONSE, BeagleCancelledResponseClass))
#define BEAGLE_IS_CANCELLED_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_CANCELLED_RESPONSE))
#define BEAGLE_IS_CANCELLED_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_CANCELLED_RESPONSE))
#define BEAGLE_CANCELLED_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_CANCELLED_RESPONSE, BeagleCancelledResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleCancelledResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleCancelledResponseClass;

GType    beagle_cancelled_response_get_type (void);

#endif /* __BEAGLE_CANCELLED_RESPONSE_H */
