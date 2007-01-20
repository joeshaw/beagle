/*
 * rlimit-glue.c: Functions for setting rlimits
 *
 * Woof.
 *
 * Copyright (C) 2007 Novell, Inc.
 */

#include <errno.h>
#include <sys/time.h>
#include <sys/resource.h>

/*
 * Define a resouce mapping to something isn't system dependent.  If you
 * change these, make sure to adjust them in Util/SystemPriorities.cs too.
 */
enum {
	BEAGLE_RLIMIT_CPU  = 0,
	BEAGLE_RLIMIT_AS = 1
};

/*
 * Simple wrapper around setrlimit(2) that does what we need.  Avoids
 * 64-bit issues in pinvoking and dealing with structures.
 */
int set_rlimit (int beagle_resource, int limit)
{
	int resource;
	struct rlimit rlim;

	switch (beagle_resource) {
	case BEAGLE_RLIMIT_CPU:
		resource = RLIMIT_CPU;
		break;

	case BEAGLE_RLIMIT_AS:
		resource = RLIMIT_AS;
		break;

	default:
		errno = EINVAL;
		return -1;
	}
	
	rlim.rlim_cur = limit;
	rlim.rlim_max = limit;

	return setrlimit (resource, &rlim);
}
