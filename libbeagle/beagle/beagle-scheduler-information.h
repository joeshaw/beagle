/*
 * beagle-scheduler-information.h
 *
 * Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
 *
 */

/*
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */

#ifndef __BEAGLE_SCHEDULER_INFORMATION_H
#define __BEAGLE_SCHEDULER_INFORMATION_H

#include <glib-object.h>

typedef struct _BeagleSchedulerInformation BeagleSchedulerInformation;

BeagleSchedulerInformation * beagle_scheduler_information_ref (BeagleSchedulerInformation *status);
void beagle_scheduler_information_unref (BeagleSchedulerInformation *status);

int
beagle_scheduler_information_get_total_task_count (BeagleSchedulerInformation *status);

G_CONST_RETURN char *
beagle_scheduler_information_get_status_string (BeagleSchedulerInformation *status);

GSList *
beagle_scheduler_information_get_pending_tasks (BeagleSchedulerInformation *status);

GSList *
beagle_scheduler_information_get_future_tasks (BeagleSchedulerInformation *status);

GSList *
beagle_scheduler_information_get_blocked_tasks (BeagleSchedulerInformation *status);

G_CONST_RETURN char *
beagle_scheduler_information_to_human_readable_string (BeagleSchedulerInformation *status);

#endif /* __BEAGLE_SCHEDULER_INFORMATION_H */

