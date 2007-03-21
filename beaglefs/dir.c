/*
 * beaglefs/dir.c - The beaglefs directory object aka a hash table of inodes
 *
 * Robert Love <rml@novell.com>
 *
 * Copyright (C) 2006 Robert Love
 *
 * Licensed under the terms of the GNU GPL v2
 */

#include <glib.h>

#include "inode.h"
#include "dir.h"

/* the hash table of inodes in the beaglefs */
static GHashTable *dir_hash;

/* the read-write lock that protects said hash AND its contents */
static GStaticRWLock dir_lock = G_STATIC_RW_LOCK_INIT;

/*
 * beagle_dir_get_count - Return the number of entries (hits) in the directory.
 *
 * Caller must hold the directory read lock.
 */
unsigned int
beagle_dir_get_count (void)
{
	return g_hash_table_size (dir_hash);
}

void
beagle_dir_read_lock (void)
{
	g_static_rw_lock_reader_lock (&dir_lock);
}

void
beagle_dir_read_unlock (void)
{
	g_static_rw_lock_reader_unlock (&dir_lock);
}

void
beagle_dir_write_lock (void)
{
	g_static_rw_lock_writer_lock (&dir_lock);
}

void
beagle_dir_write_unlock (void)
{
	g_static_rw_lock_writer_unlock (&dir_lock);
}

/*
 * beagle_dir_get_inode - Return the inode matching the given name, or NULL if
 * no such inode exists.
 *
 * The caller must hold the directory read lock across the call and for however
 * long the returned inode is referenced.  The returned inode is read-only.
 */
beagle_inode_t *
beagle_dir_get_inode (const char *name)
{
	g_return_val_if_fail (name, NULL);
	return g_hash_table_lookup (dir_hash, name);
}

/*
 * beagle_dir_for_each_inode - Invoke a function on each inode in the
 * directory.  Said function must match the prototype
 *
 *	void f (gpointer key, gpointer value, gpointer user);
 *
 * Where 'key' is the path name of the inode, 'value' is an inode object, and
 * 'user' is the 'user' parameter provided to beagle_dir_for_each_inode().
 *
 * The caller must hold the directory read lock.  The invoked function should
 * not write to the inode or remove it from the hash.
 */
void
beagle_dir_for_each_inode (GHFunc func,
			   gpointer user)
{
	g_hash_table_foreach (dir_hash, func, user);
}

/*
 * beagle_dir_add_inode - Add the given inode object to the hash of inodes.
 *
 * The caller must hold the directory write lock.
 */
void
beagle_dir_add_inode (beagle_inode_t *inode)
{
	char *key;

	g_return_if_fail (inode);

	/*
	 * FIXME: We end up removing separate hits that point to the same
	 * filename but different paths.  Ideally, we should rename subsequent
	 * matches to file-2, file-3, et cetera, if the two 'name' values match
	 * but the 'target' values do not (if 'target' does not differ, the
	 * current behavior is correct).
	 *
	 * We would also require some way to map the original filename to the
	 * new mangled name on hit remove.  The obvious solution is a second
	 * hash table, keying off of URI.  I do not want to do that.
	 */

	key = g_strdup (beagle_inode_get_name (inode));
	g_hash_table_replace (dir_hash, key, inode);
}

/*
 * beagle_dir_remove_inode_by_name - Remove any inodes from the inode list that
 * match the given name.  We do this when live-removing hits.
 *
 * Caller must hold the directory write lock.
 */
void
beagle_dir_remove_inode_by_name (const char *name)
{
	g_return_if_fail (name);
	g_hash_table_remove (dir_hash, name);
}

static void
value_destroy_func (void *data)
{
	beagle_inode_t *inode = data;
	beagle_inode_free (inode);
}

/*
 * beagle_dir_init - Initialize the directory hash.
 */
void
beagle_dir_init (void)
{
	dir_hash = g_hash_table_new_full (g_str_hash,
					  g_str_equal,
					  g_free,
					  value_destroy_func);
}

/*
 * beagle_dir_destroy - Unref the directory hash, which frees the hash table
 * and all of the constituent inodes, and free the lock.
 */
void
beagle_dir_destroy (void)
{
	g_hash_table_destroy (dir_hash);
	g_static_rw_lock_free (&dir_lock);
}
