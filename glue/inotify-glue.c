/*
 * inotify-glue.c
 *
 * Copyright (C) 2004 Novell, Inc.
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

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <fcntl.h>
#include <unistd.h>
#include <errno.h>
#include <sys/ioctl.h>
#include <sys/select.h>

#include "inotify.h"

int 
inotify_glue_watch (int fd, const char *filename, unsigned long mask)
{
	struct inotify_watch_request iwr;
	iwr.dirname = strdup (filename);
	iwr.mask = mask;
	int wd;

	wd = ioctl (fd, INOTIFY_WATCH, &iwr);
	if (wd < 0) {
		fprintf (stderr, "ioctl(%d, INOTIFY_WATCH, {%s, %ld}) failed",
		        fd, iwr.dirname, iwr.mask);
		perror ("ioctl");
	}

	free (iwr.dirname);

	return wd;
}

int 
inotify_glue_ignore (int fd, int wd)
{
	int ret;

	ret = ioctl (fd, INOTIFY_IGNORE, &wd);
	if (ret < 0) {
		fprintf (stderr, "ioctl(%d, INOTIFY_IGNORE, %d) failed",
			 fd, wd);
		perror ("ioctl");
	}

	return ret;
}

int
inotify_snarf_events (int fd, struct inotify_event *buffer, int buffer_len, int timeout_secs)
{
    struct timeval timeout;
    fd_set read_fds;
    int N, ready_bytes, total_read, max_read, select_retval;
    struct inotify_event *buffer_p;

    timeout.tv_sec = timeout_secs;
    timeout.tv_usec = 0;

    total_read = 0;
    max_read = buffer_len;
    buffer_p = buffer;

    FD_ZERO (&read_fds);
    FD_SET (fd, &read_fds);
    
    select_retval = select (fd+1, &read_fds, NULL, NULL, &timeout);

    /* If we time out, just return */
    if (select_retval == 0)
	return 0;

    N = read (fd, buffer, buffer_len * sizeof (struct inotify_event));

    return N / sizeof (struct inotify_event);
}

