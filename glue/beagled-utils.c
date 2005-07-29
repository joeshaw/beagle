#include <unistd.h>
#include <dirent.h>
#include <string.h>

int
beagled_utils_readdir (void *dir, char *name)
{
    struct dirent* entry = readdir ((DIR*) dir);

    if (entry == NULL) {
	name = NULL;
	return -1;
    }
    
    strcpy (name, entry->d_name);
    return 0;
}
