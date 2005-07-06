#ifndef __BEAGLE_INDEXABLE_H
#define __BEAGLE_INDEXABLE_H

#include <glib-object.h>
#include <beagle/beagle-property.h>
#include <beagle/beagle-timestamp.h>

typedef struct _BeagleIndexable BeagleIndexable;

typedef enum {
	BEAGLE_INDEXABLE_FILTERING_ALWAYS,
	BEAGLE_INDEXABLE_FILTERING_AUTOMATIC,
	BEAGLE_INDEXABLE_FILTERING_NEVER
} BeagleIndexableFiltering;

BeagleIndexable *beagle_indexable_new              (const char      *uri);

void             beagle_indexable_free             (BeagleIndexable *indexable);

void             beagle_indexable_add_property     (BeagleIndexable *indexable,
						    BeagleProperty  *prop);

G_CONST_RETURN char *
beagle_indexable_get_uri                           (BeagleIndexable *indexable);
void             beagle_indexable_set_uri          (BeagleIndexable *indexable,
						    const char      *uri);

G_CONST_RETURN char *
beagle_indexable_get_content_uri                   (BeagleIndexable *indexable);

void             beagle_indexable_set_content_uri  (BeagleIndexable *indexable,
						    const char      *content_uri);

G_CONST_RETURN char *
beagle_indexable_get_hot_content_uri               (BeagleIndexable *indexable);
void           
beagle_indexable_set_hot_content_uri               (BeagleIndexable *indexable,
						    const char      *hot_content_uri);

gboolean beagle_indexable_get_delete_content       (BeagleIndexable *indexable);
void     beagle_indexable_set_delete_content       (BeagleIndexable *indexable,
						    gboolean         delete_content);

gboolean         beagle_indexable_get_crawled      (BeagleIndexable *indexable);
void             beagle_indexable_set_crawled      (BeagleIndexable *indexable, gboolean crawled);

gboolean         beagle_indexable_get_no_content   (BeagleIndexable *indexable);
void             beagle_indexable_set_no_content   (BeagleIndexable *indexable, 
						    gboolean         no_content);

gboolean         beagle_indexable_get_cache_content(BeagleIndexable *indexable);
void             beagle_indexable_set_cache_content(BeagleIndexable *indexable,
						    gboolean cache_content);

BeagleIndexableFiltering
beagle_indexable_get_filtering                     (BeagleIndexable *indexable);
void             beagle_indexable_set_filtering    (BeagleIndexable *indexable,
						    BeagleIndexableFiltering filtering);

G_CONST_RETURN char * beagle_indexable_get_type    (BeagleIndexable *indexable);
void                  beagle_indexable_set_type    (BeagleIndexable *indexable, 
						    const char      *type);

G_CONST_RETURN char *beagle_indexable_get_mime_type(BeagleIndexable *indexable);
void             beagle_indexable_set_mime_type    (BeagleIndexable *indexable, 
						    const char      *mime_type);

BeagleTimestamp *beagle_indexable_get_timestamp (BeagleIndexable *indexable);
void             beagle_indexable_set_timestamp (BeagleIndexable *indexable,
						 BeagleTimestamp *timestamp);
#endif /* __BEAGLE_INDEXABLE_H */
