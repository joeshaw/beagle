#ifndef __BEAGLE_QUERY_PART_H
#define __BEAGLE_QUERY_PART_H

#include <glib.h>

#define BEAGLE_QUERY_PART_TARGET_ALL "_all"
#define BEAGLE_QUERY_PART_TARGET_TEXT "_text"
#define BEAGLE_QUERY_PART_TARGET_PROPERTIES "_prop"

typedef struct _BeagleQueryPart BeagleQueryPart;

BeagleQueryPart *beagle_query_part_new  (void);
void             beagle_query_part_free (BeagleQueryPart *part);

void beagle_query_part_set_target     (BeagleQueryPart *part,
				       const char      *target);
void beagle_query_part_set_text       (BeagleQueryPart *part,
				       const char      *text);
void beagle_query_part_set_keyword    (BeagleQueryPart *part,
				       gboolean         is_keyword);
void beagle_query_part_set_required   (BeagleQueryPart *part,
				       gboolean         is_required);
void beagle_query_part_set_prohibited (BeagleQueryPart *part,
				       gboolean         is_prohibited);

#endif /* __BEAGLE_QUERY_PART_H */
