#ifndef __BEAGLE_REQUEST_H
#define __BEAGLE_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-response.h>

#define BEAGLE_TYPE_REQUEST            (beagle_request_get_type ())
#define BEAGLE_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_REQUEST, BeagleRequest))
#define BEAGLE_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_REQUEST, BeagleRequestClass))
#define BEAGLE_IS_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_REQUEST))
#define BEAGLE_IS_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_REQUEST))
#define BEAGLE_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_REQUEST, BeagleRequestClass))

typedef struct {
	GObject parent;
} BeagleRequest;

typedef struct {
	GObjectClass parent_class;

	GHashTable *response_types;

	/* Virtual methods */
	GString *(* to_xml) (BeagleRequest *request, GError **err);

	/* Signals */
	void (* closed) (BeagleRequest *request);
	void (* response) (BeagleRequest *request, BeagleResponse *response);
	void (* error) (BeagleRequest *request, GError *error);
} BeagleRequestClass;

GType    beagle_request_get_type (void);


#endif /* __BEAGLE_REQUEST_H */
