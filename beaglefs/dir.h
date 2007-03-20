#ifndef _BEAGLEFS_DIR_H
#define _BEAGLEFS_DIR_H

unsigned int beagle_dir_get_count (void);

void beagle_dir_init (void);
void beagle_dir_destroy (void);

beagle_inode_t * beagle_dir_get_inode (const char *name);

void beagle_dir_add_inode (beagle_inode_t *inode);

void beagle_dir_remove_inode_by_name (const char *name);

void beagle_dir_for_each_inode (GHFunc func, gpointer user);

void beagle_dir_read_lock (void);
void beagle_dir_read_unlock (void);
void beagle_dir_write_lock (void);
void beagle_dir_write_unlock (void);

#endif	/* _BEAGLEFS_DIR_H */
