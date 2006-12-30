/*
 * beagle-timestamp.h
 *
 * Copyright (C) 2005 Novell, Inc.
 *
 */

/*
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

