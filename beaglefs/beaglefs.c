/*
 * beaglefs/beaglefs.c - entry point for beaglefs
 *
 * Robert Love <rml@novell.com>
 *
 * Copyright (C) 2006 Robert Love
 *
 * Licensed under the terms of the GNU GPL v2
 */

#define FUSE_USE_VERSION 25
#include <fuse.h>

#include <glib.h>

#include "hit.h"
#include "file.h"

static int opt_process (G_GNUC_UNUSED void *data,
			const char *arg,
			int key,
			G_GNUC_UNUSED struct fuse_args *outargs)
{
	static gboolean found;

	/*
	 * Grab the first non-option argument as the query text, but make sure
	 * to leave the second argument (the mount point) alone.
	 */
	if (!found && key == FUSE_OPT_KEY_NONOPT) {
		found = TRUE;
		beagle_hit_set_query (arg);
		return 0;
	}

	return 1;
}

int
main (int argc, char *argv[])
{
	struct fuse_args args = FUSE_ARGS_INIT (argc, argv);

	g_log_set_always_fatal (G_LOG_LEVEL_CRITICAL | G_LOG_LEVEL_ERROR);

	if (fuse_opt_parse (&args, NULL, NULL, opt_process) == -1)
		g_critical ("usage: %s <query> <mount point>", argv[0]);

	return fuse_main (args.argc, args.argv, &beagle_file_ops);
}
