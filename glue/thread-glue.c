#include <sys/types.h>
#include <linux/unistd.h>
#include <errno.h>

_syscall0(pid_t,gettid)

pid_t
wrap_gettid (void)
{
	return gettid ();
}
