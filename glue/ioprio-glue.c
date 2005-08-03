/*
 * ioprio-glue.c - I/O priority wrappers for the big dog
 *
 * Robert Love	<rml@novell.com>
 *
 * Copyright (C) 2005 Novell, Inc.
 */

#if defined(__i386__)
# define __NR_ioprio_set	289
# define __NR_ioprio_get	290
#elif defined(__ppc__)
# define __NR_ioprio_set	273
# define __NR_ioprio_get	274
#elif defined(__x86_64__)
# define __NR_ioprio_set	251
# define __NR_ioprio_get	252
#elif defined(__ia64__)
# define __NR_ioprio_set	1274
# define __NR_ioprio_get	1275
#else
# error "Unsupported archiecture!"
#endif

static inline int ioprio_set (int which, int who, int ioprio)
{
	return syscall (__NR_ioprio_set, which, who, ioprio);
}

static inline int ioprio_get (int which, int who)
{
	return syscall (__NR_ioprio_get, which, who);
}

enum {
	IOPRIO_CLASS_NONE,
	IOPRIO_CLASS_RT,
	IOPRIO_CLASS_BE,
	IOPRIO_CLASS_IDLE,
};

enum {
	IOPRIO_WHO_PROCESS = 1,
	IOPRIO_WHO_PGRP,
	IOPRIO_WHO_USER,
};

#define IOPRIO_CLASS_SHIFT	13
#define IOPRIO_PRIO_MASK	0xff

int set_io_priority_idle (void)
{
	int ioprio, ioclass;

	ioprio = 7; /* priority is ignored with idle class */
	ioclass = IOPRIO_CLASS_IDLE << IOPRIO_CLASS_SHIFT;

	return ioprio_set (IOPRIO_WHO_PROCESS, 0, ioprio | ioclass);
}
