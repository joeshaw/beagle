#ifndef __BEAGLE_HITS_ADDED_RESPONSE_H
#define __BEAGLE_HITS_ADDED_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"
#include "beagle-hit.h"

#define BEAGLE_TYPE_HITS_ADDED_RESPONSE            (beagle_hits_added_response_get_type ())
#define BEAGLE_HITS_ADDED_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_HITS_ADDED_RESPONSE, BeagleHitsAddedResponse))
#define BEAGLE_HITS_ADDED_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_HITS_ADDED_RESPONSE, BeagleHitsAddedResponseClass))
#define BEAGLE_IS_HITS_ADDED_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_HITS_ADDED_RESPONSE))
#define BEAGLE_IS_HITS_ADDED_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_HITS_ADDED_RESPONSE))
#define BEAGLE_HITS_ADDED_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_HITS_ADDED_RESPONSE, BeagleHitsAddedResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleHitsAddedResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleHitsAddedResponseClass;

GType    beagle_hits_added_response_get_type (void);

GSList *beagle_hits_added_response_get_hits (BeagleHitsAddedResponse *response);

#endif /* __BEAGLE_HITS_ADDED_RESPONSE_H */
