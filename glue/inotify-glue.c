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

#define SYSFS_PREFIX           "/sys/class/misc/inotify"

#define SYSFS_MAX_USER_DEVICES  SYSFS_PREFIX "/max_user_devices"
#define SYSFS_MAX_USER_WATCHES  SYSFS_PREFIX "/max_user_watches"
#define SYSFS_MAX_QUEUED_EVENTS SYSFS_PREFIX "/max_queued_events"

/* Inotify sysfs knobs, initialized to their pre-sysfs defaults */
static int max_user_devices = 8;
static int max_user_watches = 8192;
static unsigned int max_queued_events = 256;

/* Paranoid code to read an integer from a sysfs (well, any) file. */
static void
read_int (const char *filename, int *var)
{
	int fd, n;
	char buffer[32];
	char *buffer_endptr = NULL;

	fd = open (filename, O_RDONLY);
	if (fd == -1)
		return;
	if (read (fd, buffer, 31) > 0) {
		n = (int) strtol (buffer, &buffer_endptr, 10);
		if (*buffer != '\0' && *buffer_endptr == '\0')
			*var = n;
	}
	close (fd);
}


void
inotify_glue_init (void)
{
	static int initialized = 0;
	if (initialized)
		return;
	initialized = 1;

	read_int (SYSFS_MAX_USER_DEVICES, &max_user_devices);
	read_int (SYSFS_MAX_USER_WATCHES, &max_user_watches);
	read_int (SYSFS_MAX_QUEUED_EVENTS, &max_queued_events);
}


int
inotify_glue_watch (int fd, const char *filename, __u32 mask)
{
	struct inotify_watch_request iwr;
	int file_fd, wd;

	file_fd = open (filename, O_RDONLY);
	if (file_fd < 0) {
		perror ("open");
		return -1;
	}

	iwr.fd = file_fd;
	iwr.mask = mask;

	wd = ioctl (fd, INOTIFY_WATCH, &iwr);
	if (wd < 0)
		perror ("ioctl");

	if (close (file_fd))
		perror ("close");

	return wd;
}


int
inotify_glue_ignore (int fd, __s32 wd)
{
	int ret;

	ret = ioctl (fd, INOTIFY_IGNORE, &wd);
	if (ret < 0)
		perror ("ioctl");

	return ret;
}


#define MAX_PENDING_COUNT           5
#define PENDING_PAUSE_MICROSECONDS  2000
#define PENDING_THRESHOLD(qsize)    ((qsize) >> 1)
#define PENDING_MARGINAL_COST(p)    ((unsigned int)(1 << (p)))

void
inotify_snarf_events (int fd, int timeout_secs, int *nr, void **buffer_out)
{
	struct timeval timeout;
	fd_set read_fds;
	int select_retval;
	unsigned int prev_pending = 0, pending_count = 0;
	static struct inotify_event *buffer = NULL;
	static size_t buffer_size;

	/* Allocate our buffer the first time we try to read events. */
	if (buffer == NULL) {
		/* guess the avg len */
		buffer_size = sizeof (struct inotify_event) + 16;
		buffer_size *= max_queued_events;
		buffer = malloc (buffer_size);
		if (!buffer) {
			perror ("malloc");
			*buffer_out = NULL;
			return;
		}
	}

	/* Set nr to 0, so it will be sure to contain something
	   valid if the select times out. */
	*nr = 0;

	/* Wait for the file descriptor to be ready to read. */

	timeout.tv_sec = timeout_secs;
	timeout.tv_usec = 0;

	FD_ZERO (&read_fds);
	FD_SET (fd, &read_fds);

	select_retval = select (fd + 1, &read_fds, NULL, NULL, &timeout);

	/* If we time out or get an error, just return */
	if (select_retval <= 0)
		return;

	/* Reading events in groups significantly helps performance.
	 * If there are some events (but not too many!) ready, wait a
	 * bit more to see if more events come in. */

	while (pending_count < MAX_PENDING_COUNT) {
		unsigned int pending;

		if (ioctl (fd, FIONREAD, &pending) == -1)
			break;
		pending /= sizeof (struct inotify_event) + 16;	/* guess len */

		/* Don't wait if the number of pending events is too close
		 * to the maximum queue size. */
		if (pending > PENDING_THRESHOLD (max_queued_events))
			break;

		/* With each successive iteration, the minimum rate for
		 * further sleep doubles. */
		if (pending-prev_pending < PENDING_MARGINAL_COST(pending_count))
			break;

		prev_pending = pending;
		++pending_count;

		timeout.tv_sec = 0;
		timeout.tv_usec = PENDING_PAUSE_MICROSECONDS;
		select (0, NULL, NULL, NULL, &timeout);
	}

	*nr = read (fd, buffer, buffer_size);

	*buffer_out = buffer;
}
