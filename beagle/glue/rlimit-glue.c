/*
 * rlimit-glue.c: Functions for setting rlimits
 *
 * Copyright (C) 2007 Novell, Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
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
