#ifndef __BEAGLE_TIMESTAMP_H
#define __BEAGLE_TIMESTAMP_H

#include <sys/time.h>
#include <glib.h>

typedef struct _BeagleTimestamp BeagleTimestamp;

BeagleTimestamp *beagle_timestamp_new_from_string (const char *str);
BeagleTimestamp *beagle_timestamp_new_from_unix_time (time_t time);

void beagle_timestamp_free (BeagleTimestamp *timestamp);
gboolean beagle_timestamp_to_unix_time (BeagleTimestamp *timestamp, time_t *time);
 
#endif /* __BEAGLE_TIMESTAMP_H */
