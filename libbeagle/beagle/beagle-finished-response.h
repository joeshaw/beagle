#ifndef __BEAGLE_FINISHED_RESPONSE_H
#define __BEAGLE_FINISHED_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"

#define BEAGLE_TYPE_FINISHED_RESPONSE            (beagle_finished_response_get_type ())
#define BEAGLE_FINISHED_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_FINISHED_RESPONSE, BeagleFinishedResponse))
#define BEAGLE_FINISHED_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_FINISHED_RESPONSE, BeagleFinishedResponseClass))
#define BEAGLE_IS_FINISHED_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_FINISHED_RESPONSE))
#define BEAGLE_IS_FINISHED_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_FINISHED_RESPONSE))
#define BEAGLE_FINISHED_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_FINISHED_RESPONSE, BeagleFinishedResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleFinishedResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleFinishedResponseClass;

GType    beagle_finished_response_get_type (void);

#endif /* __BEAGLE_FINISHED_RESPONSE_H */
