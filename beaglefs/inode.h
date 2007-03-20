#ifndef _BEAGLEFS_INODE_H
#define _BEAGLEFS_INODE_H

#include <time.h>

typedef struct beagle_inode beagle_inode_t;

const char * beagle_inode_get_name (beagle_inode_t *inode);
const char * beagle_inode_get_target (beagle_inode_t *inode);
time_t beagle_inode_get_time (beagle_inode_t *inode);
const char * beagle_inode_get_mime_type (beagle_inode_t *inode);
const char * beagle_inode_get_type (beagle_inode_t *inode);
const char * beagle_inode_get_uri (beagle_inode_t *inode);
const char * beagle_inode_get_source (beagle_inode_t *inode);
const char * beagle_inode_get_score (beagle_inode_t *inode);

beagle_inode_t * beagle_inode_new (const char *uri, time_t time,
				   const char *mime_type, const char *type,
				   const char *source, double score);

void beagle_inode_free (beagle_inode_t *inode);

#endif	/* _BEAGLEFS_INODE_H */
