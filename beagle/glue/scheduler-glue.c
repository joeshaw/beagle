/*
 * scheduler-glue.c: Functions for setting Linux scheduler policies.
 *
 * Woof.
 *
 * Copyright (C) 2007 Novell, Inc.
 */

#define _GNU_SOURCE
#include <sched.h>

#ifndef SCHED_BATCH
#warning SCHED_BATCH is not defined; it probably will not work.
#define SCHED_BATCH 3 // see /usr/include/bits/sched.h
#endif

int set_scheduler_policy_batch (void)
{
	struct sched_param param;

	param.sched_priority = 0;

	return sched_setscheduler (0, SCHED_BATCH, &param);
}

int set_scheduler_policy_other (void)
{
	struct sched_param param;

	param.sched_priority = 0;

	return sched_setscheduler (0, SCHED_OTHER, &param);
}
