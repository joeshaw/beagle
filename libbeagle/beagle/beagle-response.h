#ifndef __BEAGLE_RESPONSE_H
#define __BEAGLE_RESPONSE_H

#include <glib-object.h>

typedef struct _BeagleResponse BeagleResponse;
typedef struct _BeagleResponseClass BeagleResponseClass;

#define BEAGLE_TYPE_RESPONSE            (beagle_response_get_type ())
#define BEAGLE_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_RESPONSE, BeagleResponse))
#define BEAGLE_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_RESPONSE, BeagleResponseClass))
#define BEAGLE_IS_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_RESPONSE))
#define BEAGLE_IS_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_RESPONSE))
#define BEAGLE_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_RESPONSE, BeagleResponseClass))

struct _BeagleResponse {
	GObject parent;
};

struct _BeagleResponseClass {
	GObjectClass parent_class;
	
	gpointer parser_handlers;
};

GType    beagle_response_get_type (void);

#endif /* __BEAGLE_RESPONSE_H */
