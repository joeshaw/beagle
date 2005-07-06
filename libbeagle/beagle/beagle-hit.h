#ifndef __BEAGLE_HIT_H
#define __BEAGLE_HIT_H

#include <glib.h>
#include <beagle/beagle-property.h>
#include <beagle/beagle-timestamp.h>

#define BEAGLE_HIT(x) ((BeagleHit *) x)

typedef struct _BeagleHit BeagleHit;

BeagleHit * beagle_hit_ref (BeagleHit *hit);
void beagle_hit_unref (BeagleHit *hit);

G_CONST_RETURN char *beagle_hit_get_uri (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_type (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_mime_type (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_source (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_source_object_name (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_parent_uri (BeagleHit *hit);

BeagleTimestamp *beagle_hit_get_timestamp (BeagleHit *hit);

long beagle_hit_get_revision (BeagleHit *hit);
int beagle_hit_get_id (BeagleHit *hit);

double beagle_hit_get_score (BeagleHit *hit);
double beagle_hit_get_score_multiplier (BeagleHit *hit);
double beagle_hit_get_score_raw (BeagleHit *hit);

G_CONST_RETURN char *beagle_hit_get_property (BeagleHit *hit, const char *key);
BeagleProperty *beagle_hit_lookup_property (BeagleHit *hit, const char *key);

#endif /* __BEAGLE_HIT_H */
