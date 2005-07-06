#ifndef __BEAGLE_PROPERTY_H
#define __BEAGLE_PROPERTY_H

#include <glib.h>

typedef struct _BeagleProperty BeagleProperty;

BeagleProperty *beagle_property_new (const char *key, const char *value);
void beagle_property_free (BeagleProperty *prop);

G_CONST_RETURN char *beagle_property_get_key (BeagleProperty *prop);
void beagle_property_set_key (BeagleProperty *prop, const char *key);

G_CONST_RETURN char *beagle_property_get_value (BeagleProperty *prop);
void beagle_property_set_value (BeagleProperty *prop, const char *value);

gboolean beagle_property_get_is_searched (BeagleProperty *prop);
void beagle_property_set_is_searched (BeagleProperty *prop, gboolean is_searched);

gboolean beagle_property_get_is_keyword (BeagleProperty *prop);
void beagle_property_set_is_keyword (BeagleProperty *prop, gboolean is_keyword);

#endif /* __BEAGLE_PROPERTY_H */
