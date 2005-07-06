#ifndef __BEAGLE_SNIPPET_RESPONSE_H
#define __BEAGLE_SNIPPET_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"

#define BEAGLE_TYPE_SNIPPET_RESPONSE            (beagle_snippet_response_get_type ())
#define BEAGLE_SNIPPET_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_SNIPPET_RESPONSE, BeagleSnippetResponse))
#define BEAGLE_SNIPPET_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_SNIPPET_RESPONSE, BeagleSnippetResponseClass))
#define BEAGLE_IS_SNIPPET_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_SNIPPET_RESPONSE))
#define BEAGLE_IS_SNIPPET_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_SNIPPET_RESPONSE))
#define BEAGLE_SNIPPET_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_SNIPPET_RESPONSE, BeagleSnippetResponseClass))

typedef struct {
	BeagleResponse parent;
} BeagleSnippetResponse;

typedef struct {
	BeagleResponseClass parent_class;
} BeagleSnippetResponseClass;

GType                 beagle_snippet_response_get_type    (void);
G_CONST_RETURN char * beagle_snippet_response_get_snippet (BeagleSnippetResponse *response);

#endif /* __BEAGLE_SNIPPET_RESPONSE_H */
