#ifndef __BEAGLE_CLIENT_H
#define __BEAGLE_CLIENT_H

#include <glib-object.h>

#include "beagle-request.h"

#define BEAGLE_TYPE_CLIENT            (beagle_client_get_type ())
#define BEAGLE_CLIENT(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_CLIENT, BeagleClient))
#define BEAGLE_CLIENT_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_CLIENT, BeagleClientClass))
#define BEAGLE_IS_CLIENT(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_CLIENT))
#define BEAGLE_IS_CLIENT_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_CLIENT))
#define BEAGLE_CLIENT_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_CLIENT, BeagleClientClass))

typedef struct {
	GObject parent;
} BeagleClient;

typedef struct {
	GObjectClass parent_class;
} BeagleClientClass;

GType        beagle_client_get_type     (void);
BeagleClient *beagle_client_new          (const char *client_name);

BeagleResponse *beagle_client_send_request (BeagleClient   *client,
					    BeagleRequest  *request,
					    GError        **err);
gboolean beagle_client_send_request_async (BeagleClient   *client,
					   BeagleRequest  *request,
					   GError        **err);

#endif /* __BEAGLE_CLIENT_H */
