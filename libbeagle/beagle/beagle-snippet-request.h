#ifndef __BEAGLE_SNIPPET_REQUEST_H
#define __BEAGLE_SNIPPET_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-request.h>
#include <beagle/beagle-hit.h>
#include <beagle/beagle-query.h>

#define BEAGLE_TYPE_SNIPPET_REQUEST            (beagle_snippet_request_get_type ())
#define BEAGLE_SNIPPET_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequest))
#define BEAGLE_SNIPPET_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequestClass))
#define BEAGLE_IS_SNIPPET_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_SNIPPET_REQUEST))
#define BEAGLE_IS_SNIPPET_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_SNIPPET_REQUEST))
#define BEAGLE_SNIPPET_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequestClass))

typedef struct {
	BeagleRequest parent;
} BeagleSnippetRequest;

typedef struct {
	BeagleRequestClass parent_class;
} BeagleSnippetRequestClass;

GType        beagle_snippet_request_get_type     (void);
BeagleSnippetRequest *beagle_snippet_request_new          (void);

void beagle_snippet_request_set_hit (BeagleSnippetRequest *request,
				     BeagleHit *hit);

void beagle_snippet_request_add_query_term (BeagleSnippetRequest *request,
					    const char           *text);

void beagle_snippet_request_set_query_terms_from_query (BeagleSnippetRequest *request,
							BeagleQuery          *query);

#endif /* __BEAGLE_SNIPPET_REQUEST_H */
