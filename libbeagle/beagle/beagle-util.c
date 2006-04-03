/*
 * beagle-util.c
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

#include <stdio.h>
#include <stdlib.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <unistd.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-util.h"

GQuark
beagle_error_quark (void)
{
	static GQuark quark;

	if (!quark)
		quark = g_quark_from_static_string ("BEAGLE_ERROR");

	return quark;
}

gboolean
beagle_util_is_path_on_block_device (const char *path)
{
	struct stat st;

	if (stat (path, &st) < 0)
		return FALSE;

	return (st.st_dev >> 8 != 0);
}

gboolean
beagle_util_daemon_is_running (void)
{
	const gchar *beagle_home;
	gchar *socket_dir;
	gchar *socket_path;
	int sockfd;
	struct sockaddr_un sun;

	beagle_home = g_getenv ("BEAGLE_HOME");
	if (beagle_home == NULL)
		beagle_home = g_get_home_dir ();
	
	socket_dir = g_build_filename (beagle_home, ".beagle", NULL);
	socket_path = g_build_filename (socket_dir, "socket", NULL);

	bzero (&sun, sizeof (sun));
	sun.sun_family = AF_UNIX;
	snprintf (sun.sun_path, sizeof (sun.sun_path), socket_path);

	g_free (socket_path);
	g_free (socket_dir);

	sockfd = socket (AF_UNIX, SOCK_STREAM, 0);
	if (sockfd < 0) {
		return FALSE;
	}

	if (connect (sockfd, (struct sockaddr *) &sun, sizeof (sun)) < 0) {
		return FALSE;
	}

	close (sockfd);
	
	return TRUE;
}
