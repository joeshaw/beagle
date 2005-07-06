#ifndef __BEAGLE_EMPTY_RESPONSE_H
#define __BEAGLE_EMPTY_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"

#define BEAGLE_TYPE_EMPTY_RESPONSE            (beagle_empty_response_get_type ())
#define BEAGLE_EMPTY_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_EMPTY_RESPONSE, BeagleEmptyResponse))
#define BEAGLE_EMPTY_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_EMPTY_RESPONSE, BeagleEmptyResponseClass))
#define BEAGLE_IS_EMPTY_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_EMPTY_RESPONSE))
#define BEAGLE_IS_EMPTY_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_EMPTY_RESPONSE))
#define BEAGLE_EMPTY_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_EMPTY_RESPONSE, BeagleEmptyResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleEmptyResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleEmptyResponseClass;

GType beagle_empty_response_get_type (void);

#endif /* __BEAGLE_EMPTY_RESPONSE_H */
