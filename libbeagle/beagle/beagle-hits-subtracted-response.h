#ifndef __BEAGLE_HITS_SUBTRACTED_RESPONSE_H
#define __BEAGLE_HITS_SUBTRACTED_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"
#include "beagle-hit.h"

#define BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE            (beagle_hits_subtracted_response_get_type ())
#define BEAGLE_HITS_SUBTRACTED_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE, BeagleHitsSubtractedResponse))
#define BEAGLE_HITS_SUBTRACTED_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE, BeagleHitsSubtractedResponseClass))
#define BEAGLE_IS_HITS_SUBTRACTED_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE))
#define BEAGLE_IS_HITS_SUBTRACTED_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE))
#define BEAGLE_HITS_SUBTRACTED_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_HITS_SUBTRACTED_RESPONSE, BeagleHitsSubtractedResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleHitsSubtractedResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleHitsSubtractedResponseClass;

GType    beagle_hits_subtracted_response_get_type (void);

GSList *beagle_hits_subtracted_response_get_uris (BeagleHitsSubtractedResponse *response);

#endif /* __BEAGLE_HITS_SUBTRACTED_RESPONSE_H */
