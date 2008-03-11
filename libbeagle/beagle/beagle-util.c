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

#include <locale.h>
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

const char *
beagle_util_get_home_dir ()
{
	const char *beagle_home;

	/* C# API: First try BEAGLE_HOME */
	beagle_home = g_getenv ("BEAGLE_HOME");
	
	/* Finally, beagle home is home dir */
	if (beagle_home == NULL)
		beagle_home = g_get_home_dir ();

	return beagle_home;
}

const char *
beagle_util_get_storage_dir ()
{
	const char *beagle_home;
	const char *beagle_storage_dir;

	/* Follow the C# API: First try BEAGLE_STORAGE */
	beagle_storage_dir = g_getenv ("BEAGLE_STORAGE");

	/* Then try BEAGLE_HOME/.beagle */
	if (beagle_storage_dir == NULL) {
		beagle_home = beagle_util_get_home_dir ();

		/* Construct beagle storage dir */
		beagle_storage_dir = g_build_filename (beagle_home, ".beagle", NULL);
	}

	return beagle_storage_dir;
}

char *
beagle_util_get_socket_path (const char *client_name)
{
	const gchar *beagle_storage_dir;
	gchar *socket_dir; /* this is same as remote_storage_dir in PathFinder.cs */
	gchar *socket_path;
	struct stat buf;
	
	if (!client_name) 
		client_name = "socket";

	beagle_storage_dir = beagle_util_get_storage_dir ();

	if (! beagle_util_is_path_on_block_device (beagle_storage_dir) ||
	    getenv ("BEAGLE_SYNCHRONIZE_LOCALLY") != NULL) {
		gchar *remote_storage_dir_location_file = g_build_filename (beagle_storage_dir, "remote_storage_dir", NULL);
		gchar *tmp;

		if (! g_file_test (remote_storage_dir_location_file, G_FILE_TEST_EXISTS)) {
			g_free (remote_storage_dir_location_file);
			return NULL;
		}

		if (! g_file_get_contents (remote_storage_dir_location_file, &socket_dir, NULL, NULL)) {
			g_free (remote_storage_dir_location_file);
			return NULL;
		}

		g_free (remote_storage_dir_location_file);

		/* There's a newline at the end that we want to strip off */
		tmp = strrchr (socket_dir, '\n');
		if (tmp != NULL)
			*tmp = '\0';

		if (! g_file_test (socket_dir, G_FILE_TEST_EXISTS | G_FILE_TEST_IS_DIR)) {
			g_free (socket_dir);
			return NULL;
		}
	} else {
		socket_dir = g_build_filename (beagle_storage_dir, NULL);
	}

	socket_path = g_build_filename (socket_dir, client_name, NULL);
	g_free (socket_dir);
	if (stat (socket_path, &buf) == -1 || !S_ISSOCK (buf.st_mode)) {
		g_free (socket_path);
		return NULL;
	}

	return socket_path;

}

gboolean
beagle_util_daemon_is_running (void)
{
	gchar *socket_path;
	int sockfd;
	struct sockaddr_un sun;

	socket_path = beagle_util_get_socket_path (NULL);

	if (socket_path == NULL)
		return FALSE;

	sockfd = _beagle_connect_timeout (socket_path, NULL);

	g_free (socket_path);

	if (sockfd < 0) {
		return FALSE;
	}

	close (sockfd);
	
	return TRUE;
}

char*
_beagle_util_set_c_locale ()
{
	char *old_locale, *saved_locale;

	/* Get the name of the current locale.  */
	old_locale = setlocale (LC_ALL, NULL);

	/* Copy the name so it won't be clobbered by setlocale. */
	saved_locale = strdup (old_locale);

	/* Now it is safe to change the locale temporarily. */
	setlocale (LC_ALL, "C");

	return saved_locale;
}

void _beagle_util_reset_locale (char *old_locale)
{
	/* Restore the original locale. */
	setlocale (LC_ALL, old_locale);

	free (old_locale);
}

