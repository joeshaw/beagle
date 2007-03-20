/*
 * beaglefs/inode.c - the beaglefs inode object
 *
 * Robert Love <rml@novell.com>
 *
 * Copyright (C) 2006 Robert Love
 *
 * Licensed under the terms of the GNU GPL v2
 */

#include <string.h>

#include <glib.h>

#include "inode.h"

struct beagle_inode {
	char *name;		/* filename */
	char *target;		/* target of the symlink, absolute file path */
	time_t time;		/* inode's m_time, c_time, and a_time */
	char *mime_type;	/* xattr: MIME Type */
	char *type;		/* xattr: Hit Type */
	char *uri;		/* xattr: URI */
	char *source;		/* xattr: Source */
	char *score;		/* xattr: Hit Score */
};

const char *
beagle_inode_get_name (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->name;
}

const char *
beagle_inode_get_target (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->target;
}

time_t
beagle_inode_get_time (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, -1);
	return inode->time;
}

const char *
beagle_inode_get_mime_type (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->mime_type;
}

const char *
beagle_inode_get_type (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->type;
}

const char *
beagle_inode_get_uri (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->uri;
}

const char *
beagle_inode_get_source (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->source;
}

const char *
beagle_inode_get_score (beagle_inode_t *inode)
{
	g_return_val_if_fail (inode, NULL);
	return inode->score;
}

/*
 * beagle_inode_new - Allocate and return a new inode object, initializing it
 * with the provided values, of which fresh copies are made.
 *
 * Returns the newly allocated inode object, which must be freed via a call to
 * beagle_inode_free().  The inode is not automatically added to the directory
 * hash; you probably want to call beagle_dir_add_inode() next.
 */
beagle_inode_t *
beagle_inode_new (const char *uri,
		  time_t timestamp,
		  const char *mime_type,
		  const char *type,
		  const char *source,
		  double score)
{
	beagle_inode_t *inode;
	char *target;

	g_return_val_if_fail (uri && *uri != '\0', NULL);	
	g_return_val_if_fail (timestamp != (time_t)(-1), NULL);
	g_return_val_if_fail (mime_type && *mime_type != '\0', NULL);
	g_return_val_if_fail (type && *type != '\0', NULL);
	g_return_val_if_fail (source && *source != '\0', NULL);

#if GLIB_CHECK_VERSION(2,10,0)
	inode = g_slice_new (beagle_inode_t);
# else
	inode = g_new (beagle_inode_t, 1);
#endif

	target = g_filename_from_uri (uri, NULL, NULL);
	if (!target)
		target = g_strdup (uri + 7); /* try to convert it manually */

	inode->name = g_strdup (strrchr (target, G_DIR_SEPARATOR));
	inode->target = target;
	inode->time = timestamp;
	inode->mime_type = g_strdup (mime_type);
	inode->type = g_strdup (type);
	inode->uri = g_strdup (uri);
	inode->source = g_strdup (source);
	inode->score = g_strdup_printf ("%.4lf", score);

	return inode;
}

/*
 * beagle_inode_free - free an inode object and its constituents
 */
void
beagle_inode_free (beagle_inode_t *inode)
{
	g_return_if_fail (inode);

	g_free (inode->score);
	g_free (inode->source);
	g_free (inode->uri);
	g_free (inode->type);
	g_free (inode->mime_type);
	g_free (inode->target);
	g_free (inode->name);

#if GLIB_CHECK_VERSION(2,10,0)
	g_slice_free (beagle_inode_t, inode);
# else
	g_free (inode);
#endif
}
