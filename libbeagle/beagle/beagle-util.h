#ifndef __BEAGLE_UTIL_H
#define __BEAGLE_UTIL_H

#include <glib.h>

#define BEAGLE_ERROR (beagle_error_quark ())

GQuark beagle_error_quark (void);

typedef enum {
	BEAGLE_ERROR_DAEMON_ERROR,
} BeagleError;

#endif /* __BEAGLE_UTIL_H */
