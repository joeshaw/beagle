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
#include <sys/ioctl.h>
#include <sys/select.h>

#include "inotify.h"

typedef void (* inotify_event_callback) (int wd, unsigned long mask, int cookie, const char *filename);

int inotify_glue_open_dev ()
{
	int fd;

	fd = open("/dev/inotify", O_RDONLY);
	if (fd < 0)
		perror ("open");

	return fd;
}

int inotify_glue_close_dev (int fd)
{
	int r;

	r = close (fd);
	if (r < 0)
		perror ("close");

	return r;
}

int inotify_glue_watch (int fd, const char *filename, unsigned long mask)
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

int inotify_glue_ignore (int fd, int wd)
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

int inotify_glue_try_for_event (int fd, int sec, int usec,
				inotify_event_callback callback)
{
  	struct timeval timeout;
	fd_set rfds;
	struct inotify_event event;
	char *event_buffer;
	int num_bytes, remaining, ret;

	if (!callback)
        	return 0;

	timeout.tv_sec = sec;
	timeout.tv_usec = usec;

	FD_ZERO (&rfds);
	FD_SET (fd, &rfds);

	ret = select (fd + 1, &rfds, NULL, NULL, &timeout);

	/* We couldn't find an event. */
	if (ret <= 0)
		return 0;

	event.wd = 0;
	event.mask = 0;

	remaining = sizeof (struct inotify_event);
	while (remaining > 0) {
        	num_bytes = read (fd, &event, remaining);
		/*
		 * If num_bytes==0, this would be an unexpected EOF, resulting
		 * in a partial read of the inotify_event structure, so return.
		 */
		if (!num_bytes) {
			fprintf (stderr, "Unexpected EOF on read()\n");
			return 0;
		}
		/* If num_bytes<0, we have an error.  Return there, too. */
		if (num_bytes < 0) {
			perror ("read");
			return 0;
		}
		event_buffer += num_bytes;
		remaining -= num_bytes;
	}

	callback (event.wd, event.mask, event.cookie, event.filename);

	return 1;
}
