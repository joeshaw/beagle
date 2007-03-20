/*
 * beaglefs/file.c - beaglefs's file operations
 *
 * Robert Love <rml@novell.com>
 *
 * Copyright (C) 2006 Robert Love
 *
 * Licensed under the terms of the GNU GPL v2
 */

#define FUSE_USE_VERSION 25
#include <fuse.h>

#include <string.h>
#include <errno.h>
#include <unistd.h>

#include <glib.h>

#include "file.h"
#include "inode.h"
#include "dir.h"
#include "hit.h"

/*
 * stat_new_from_inode - populate a stat object from a beagle inode
 *
 * The caller needs to hold the directory read lock.
 */
static void
stat_new_from_inode (struct stat **sp,
		     beagle_inode_t *inode)
{
	struct stat *sb = *sp;

	g_return_if_fail (sb);
	g_return_if_fail (inode);

	memset (sb, 0, sizeof (struct stat));

	sb->st_mode = S_IFLNK | 0777;
	sb->st_nlink = 1;
	sb->st_size = strlen (beagle_inode_get_target (inode));
	sb->st_uid = fuse_get_context()->uid;
	sb->st_gid = fuse_get_context()->gid;
	sb->st_atime = sb->st_mtime = sb->st_ctime =
		beagle_inode_get_time (inode);
}

static int
beagle_getattr (const char *path,
		struct stat *sb)
{
	beagle_inode_t *inode;
	int ret;

	if (!strcmp (path, G_DIR_SEPARATOR_S)) {
		memset (sb, 0, sizeof (struct stat));
		sb->st_mode = S_IFDIR | 0755;
		sb->st_nlink = 2;
		sb->st_uid = fuse_get_context()->uid;
		sb->st_gid = fuse_get_context()->gid;
		sb->st_atime = sb->st_mtime = sb->st_ctime = time (NULL);
		return 0;
	}

	ret = -ENOENT;
	beagle_dir_read_lock ();
	inode = beagle_dir_get_inode (path);
	if (inode) {
		stat_new_from_inode (&sb, inode);
		ret = 0;
	}
	beagle_dir_read_unlock ();

	return ret;
}

static int
beagle_statfs (G_GNUC_UNUSED const char *path,
	       struct statvfs *buf)
{
	static int pagesize;

	memset (buf, 0, sizeof (struct statvfs));

	buf->f_bsize = pagesize ? : (pagesize = getpagesize ());

	beagle_dir_read_lock ();
	buf->f_files = beagle_dir_get_count () + 2;
	beagle_dir_read_unlock ();

	return 0;
}

typedef struct {
	fuse_fill_dir_t filler;
	void *buf;
} do_fill_dir_t;

static void
do_fill_dir (gpointer key,
	     G_GNUC_UNUSED gpointer value,
	     gpointer user)
{
	const char *name = (const char *) key + 1;	
	do_fill_dir_t *fill = user;

	fill->filler (fill->buf, name, NULL, 0);
}

static int
beagle_readdir (const char *path,
		void *buf,
		fuse_fill_dir_t filler,
		G_GNUC_UNUSED off_t offset,
		G_GNUC_UNUSED struct fuse_file_info *fi)
{
	do_fill_dir_t user = { filler, buf };

	if (strcmp (path, G_DIR_SEPARATOR_S) != 0)
		return -ENOENT;

	filler (buf, ".", NULL, 0);
	filler (buf, "..", NULL, 0);

	beagle_dir_read_lock ();
	beagle_dir_for_each_inode (do_fill_dir, &user);
	beagle_dir_read_unlock ();

	return 0;
}

static int
beagle_readlink (const char *path,
		 char *buf,
		 size_t len)
{
	beagle_inode_t *inode;
	int ret;

	if (len <= 0)
		return -EINVAL;

	ret = -ENOENT;
	beagle_dir_read_lock ();
	inode = beagle_dir_get_inode (path);
	if (inode) {
		g_strlcpy (buf, beagle_inode_get_target (inode), len);
		ret = 0;
	}
	beagle_dir_read_unlock ();

	return ret;
}

/*
 * copy_xattr - copy a given xattr value into the buffer of the given len,
 * behaving per POSIX.
 */
static int
copy_xattr (char *buf,
	    const char *value,
	    size_t len)
{
	size_t size;

	if (!value)
		return -ENODATA;

	size = strlen (value) + 1;
	if (len) {
		if (len >= size)
			memcpy (buf, value, size);
		if (len < size)
			return -ERANGE;
	}

	return size - 1;
}

#define BEAGLEFS_XATTR_PREFIX		"system.Beagle."
#define BEAGLEFS_XATTR_PREFIX_LEN	14

static int
beagle_getxattr (const char *path,
		 const char *key,
		 char *buf,
		 size_t len)
{
	beagle_inode_t *inode;
	int ret;

	if (strlen (key) < BEAGLEFS_XATTR_PREFIX_LEN + 1)
		return -ENODATA;
	if (strncmp (key, BEAGLEFS_XATTR_PREFIX, BEAGLEFS_XATTR_PREFIX_LEN))
		return -ENODATA;
	key += BEAGLEFS_XATTR_PREFIX_LEN;

	ret = -ENOENT;
	beagle_dir_read_lock ();
	inode = beagle_dir_get_inode (path);
	if (inode) {
		if (!strcmp (key, "mime_type"))
			ret = copy_xattr (buf,
					  beagle_inode_get_mime_type (inode),
					  len);
		else if (!strcmp (key, "type"))
			ret = copy_xattr (buf,
					  beagle_inode_get_type (inode),
					  len);
		else if (!strcmp (key, "uri"))
			ret = copy_xattr (buf,
					  beagle_inode_get_uri (inode),
					  len);
		else if (!strcmp (key, "source"))
			ret = copy_xattr (buf,
					  beagle_inode_get_source (inode),
					  len);
		else if (!strcmp (key, "score"))
			ret = copy_xattr (buf,
					  beagle_inode_get_score (inode),
					  len);
		else
			ret = -ENODATA;
	}
	beagle_dir_read_unlock ();

	return ret;
}

static int
beagle_listxattr (G_GNUC_UNUSED const char *path,
		  char *buf,
		  size_t len)
{
	const char list[] = BEAGLEFS_XATTR_PREFIX"mime_type\0"
			    BEAGLEFS_XATTR_PREFIX"type\0"
			    BEAGLEFS_XATTR_PREFIX"score\0"
			    BEAGLEFS_XATTR_PREFIX"source\0"
			    BEAGLEFS_XATTR_PREFIX"uri";
	size_t size = sizeof (list);

	if (len) {
		if (len >= size)
			memcpy (buf, list, size);
		if (len < size)
			return -ERANGE;
	}

	return size;
}

static void *
beagle_init (void)
{
	beagle_dir_init ();
	beagle_hit_init ();
	return NULL;
}

static void
beagle_destroy (G_GNUC_UNUSED void *ignore)
{
	beagle_hit_destroy ();
	beagle_dir_destroy ();
}

struct fuse_operations beagle_file_ops = {
	.getattr = beagle_getattr,
	.statfs = beagle_statfs,
	.readdir = beagle_readdir,
	.readlink = beagle_readlink,
	.getxattr = beagle_getxattr,
	.listxattr = beagle_listxattr,
	.init = beagle_init,
	.destroy = beagle_destroy
};
