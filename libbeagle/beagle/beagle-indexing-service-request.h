#ifndef __BEAGLE_INDEXING_SERVICE_REQUEST_H
#define __BEAGLE_INDEXING_SERVICE_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-request.h>
#include <beagle/beagle-indexable.h>

#define BEAGLE_TYPE_INDEXING_SERVICE_REQUEST            (beagle_indexing_service_request_get_type ())
#define BEAGLE_INDEXING_SERVICE_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST, BeagleIndexingServiceRequest))
#define BEAGLE_INDEXING_SERVICE_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST, BeagleIndexingServiceRequestClass))
#define BEAGLE_IS_INDEXING_SERVICE_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST))
#define BEAGLE_IS_INDEXING_SERVICE_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST))
#define BEAGLE_INDEXING_SERVICE_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST, BeagleIndexingServiceRequestClass))

typedef struct {
	BeagleRequest parent;
} BeagleIndexingServiceRequest;

typedef struct {
	BeagleRequestClass parent_class;
} BeagleIndexingServiceRequestClass;

GType        beagle_indexing_service_request_get_type     (void);
BeagleIndexingServiceRequest *beagle_indexing_service_request_new          (void);
void beagle_indexing_service_request_add (BeagleIndexingServiceRequest *request, 
					  BeagleIndexable *indexable);
void beagle_indexing_service_request_remove (BeagleIndexingServiceRequest *request, const char *uri);

#endif /* __BEAGLE_INDEXING_SERVICE_REQUEST_H */
