#include <unistd.h>
#include <dirent.h>
#include <string.h>

int
beagled_utils_readdir (void *dir, char *name, int max_len)
{
    struct dirent* entry = readdir ((DIR*) dir);

    if (entry == NULL) {
	if (max_len > 0)
	    *name = '\0';
	return -1;
    }
    
    strncpy (name, entry->d_name, max_len);
    return 0;
}
