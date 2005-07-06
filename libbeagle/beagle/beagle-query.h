#ifndef __BEAGLE_QUERY_H
#define __BEAGLE_QUERY_H

#include <glib-object.h>

#include <beagle/beagle-request.h>
#include <beagle/beagle-hits-added-response.h>
#include <beagle/beagle-hits-subtracted-response.h>
#include <beagle/beagle-finished-response.h>
#include <beagle/beagle-query-part.h>

#define BEAGLE_TYPE_QUERY            (beagle_query_get_type ())
#define BEAGLE_QUERY(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_QUERY, BeagleQuery))
#define BEAGLE_QUERY_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_QUERY, BeagleQueryClass))
#define BEAGLE_IS_QUERY(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_QUERY))
#define BEAGLE_IS_QUERY_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_QUERY))
#define BEAGLE_QUERY_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_QUERY, BeagleQueryClass))

typedef struct {
	BeagleRequest parent;
} BeagleQuery;

typedef struct {
	BeagleRequestClass parent_class;

	void (*hits_added)      (BeagleQuery *query, BeagleHitsAddedResponse *response);
	void (*hits_subtracted) (BeagleQuery *query, BeagleHitsSubtractedResponse *response);
	void (*cancelled)       (BeagleQuery *query);
	void (*finished)        (BeagleQuery *query, BeagleFinishedResponse *response);
} BeagleQueryClass;

GType        beagle_query_get_type     (void);
BeagleQuery *beagle_query_new          (void);
void         beagle_query_add_part     (BeagleQuery     *query, 
					BeagleQueryPart *part);
void         beagle_query_add_text     (BeagleQuery     *query,
					const char      *str);

void	     beagle_query_add_mime_type (BeagleQuery *query,
					 const char  *mime_type);
void	     beagle_query_add_hit_type  (BeagleQuery *query,
					 const char  *hit_type);

void         beagle_query_add_source (BeagleQuery *query,
				      const char  *source);

void	     beagle_query_set_max_hits (BeagleQuery *query, 
					int max_hits);
int          beagle_query_get_max_hits (BeagleQuery *query);

#endif /* __BEAGLE_QUERY_H */
