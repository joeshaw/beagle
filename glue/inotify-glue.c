/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 4 -*- */


/*
 * inotify-glue.c
 *
 * Copyright (C) 2004 The Free Software Foundation, Inc.
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
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <sys/select.h>

#include "inotify.h"

typedef void (* inotify_event_callback) (int wd, unsigned long mask, int cookie, const char *filename);

int inotify_glue_open_dev ()
{
	int fd;

	fd = open("/dev/inotify", O_RDONLY);

	if (fd < 0)
		perror ("open(\"/dev/inotify\", O_RDONLY) = ");

	return fd;
}

int inotify_glue_close_dev (int fd)
{
	int r;

	if ( (r = close (fd)) < 0)
		perror ("close (fd) = ");

	return r;
}


int inotify_glue_watch (int fd, const char *filename, unsigned long mask)
{
	struct inotify_watch_request iwr;
	iwr.dirname = strdup (filename);
	iwr.mask = mask;
	int wd;

	wd = ioctl(fd, INOTIFY_WATCH, &iwr);
	if (wd < 0) {
		printf("ioctl(fd, INOTIFY_WATCH, {%s, %ld}) =",
		       iwr.dirname, iwr.mask);
		fflush(stdout);
		perror (" ");
	}

	free (iwr.dirname);

#ifdef VERBOSE
	printf("%s WD=%d\n", filename, wd);
#endif
	return wd;
}

int inotify_glue_ignore (int fd, int wd)
{
	int r;

	r = ioctl (fd, INOTIFY_IGNORE, &wd);
	if (r < 0)
		perror("ioctl(fd, INOTIFY_IGNORE, &wid) = ");

	return r;
}

int inotify_glue_try_for_event (int fd, int sec, int usec, inotify_event_callback callback)
{
  	struct timeval timeout;
	int r;
	fd_set rfds;

    struct inotify_event event;
    char *event_buffer;
    int num_bytes, remaining;

    if (callback == NULL)
        return 0;

	timeout.tv_sec = sec;
	timeout.tv_usec = usec;

	FD_ZERO (&rfds);
	FD_SET (fd, &rfds);

	r = select (fd+1, &rfds, NULL, NULL, &timeout);
    
    /* We couldn't find an event. */
    if (r <= 0)
        return 0;

    event.wd = 0;
    event.mask = 0;

    /* FIXME: We need to check for errors, etc. */
    remaining = sizeof (struct inotify_event);
    event_buffer = (char *) &event;
    while (remaining > 0) {
        num_bytes = read (fd, event_buffer, remaining);
        event_buffer += num_bytes;
        remaining -= num_bytes;
    }

    callback (event.wd, event.mask, event.cookie, event.filename);

    return 1;
}
