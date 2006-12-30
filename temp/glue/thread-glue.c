#include <sys/types.h>
#include <unistd.h>
#include <linux/unistd.h>
#include <errno.h>

#ifdef __NR_gettid
static pid_t gettid (void)
{
	return syscall(__NR_gettid);
}
#else
static pid_t gettid (void)
{
	return 0;
}
#endif

pid_t
wrap_gettid (void)
{
	return gettid ();
}
