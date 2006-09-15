/*
 * beagle-scheduler-information.c
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

#include "beagle-scheduler-information.h"
#include "beagle-private.h"

BeagleSchedulerInformation *
_beagle_scheduler_information_new ()
{
	BeagleSchedulerInformation *sched_info;

	sched_info = g_new0 (BeagleSchedulerInformation, 1);

	sched_info->ref_count = 1;

	sched_info->total_task_count = -1;
	sched_info->status_string = NULL;
	sched_info->pending_task = NULL;
	sched_info->future_task = NULL;
	sched_info->blocked_task = NULL;

	return sched_info;
}

/**
 * beagle_scheduler_information_ref:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Increases the reference count of the #BeagleSchedulerInformation.
 *
 * Return value: the #BeagleSchedulerInformation
 **/
BeagleSchedulerInformation *
beagle_scheduler_information_ref (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, NULL);

	sched_info->ref_count ++;

	return sched_info;
}

/**
 * beagle_scheduler_information_unref:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Decreases the reference count of the #BeagleSchedulerInformation. When the reference count drops to 0, it is freed.
 **/
void
beagle_scheduler_information_unref (BeagleSchedulerInformation *sched_info)
{
	g_return_if_fail (sched_info != NULL);
	g_return_if_fail (sched_info->ref_count > 0);

	sched_info->ref_count --;

	if (sched_info->ref_count == 0) {
		g_free (sched_info->status_string);
                
		if (sched_info->pending_task) {
			g_slist_foreach (sched_info->pending_task, (GFunc) g_free, NULL);
			g_slist_free (sched_info->pending_task);
		}
		
		if (sched_info->future_task) {
			g_slist_foreach (sched_info->future_task, (GFunc) g_free, NULL);
			g_slist_free (sched_info->future_task);
		}
		
		if (sched_info->blocked_task) {
			g_slist_foreach (sched_info->blocked_task, (GFunc) g_free, NULL);
			g_slist_free (sched_info->blocked_task);
		}
		
		g_free (sched_info);
	}
}

/**
 * beagle_scheduler_information_get_total_task_count:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches the total number of tasks from the given #BeagleSchedulerInformation.
 *
 * Return value: the number of tasks from the #BeagleSchedulerInformation.
 **/
int
beagle_scheduler_information_get_total_task_count (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, -1);

	return sched_info->total_task_count;
}

/**
 * beagle_scheduler_information_get_status_string:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches the status string from the given #BeagleSchedulerInformation.
 *
 * Return value: the status string from the #BeagleSchedulerInformation.
 **/
G_CONST_RETURN char *
beagle_scheduler_information_get_status_string (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, NULL);

	return sched_info->status_string;
}

/**
 * beagle_scheduler_information_get_pending_tasks:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches the list of pending tasks as strings from the given #BeagleSchedulerInformation.
 *
 * Return value: the list of pending tasks from the #BeagleSchedulerInformation.
 **/
GSList *
beagle_scheduler_information_get_pending_tasks (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, NULL);

	return sched_info->pending_task;
}

/**
 * beagle_scheduler_information_get_future_tasks:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches the list of future tasks as strings from the given #BeagleSchedulerInformation.
 *
 * Return value: the list of future tasks from the #BeagleSchedulerInformation.
 **/
GSList *
beagle_scheduler_information_get_future_tasks (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, NULL);

	return sched_info->future_task;
}

/**
 * beagle_scheduler_information_get_blocked_tasks:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches the list of blocked tasks as strings from the given #BeagleSchedulerInformation.
 *
 * Return value: the list of blocked tasks from the #BeagleSchedulerInformation.
 **/
GSList *
beagle_scheduler_information_get_blocked_tasks (BeagleSchedulerInformation *sched_info)
{
	g_return_val_if_fail (sched_info != NULL, NULL);

	return sched_info->blocked_task;
}

/**
 * beagle_scheduler_information_to_human_readable_string:
 * @sched_info: a #BeagleSchedulerInformation
 *
 * Fetches a string version of the given #BeagleSchedulerInformation.
 *
 * Return value: a string version from the #BeagleSchedulerInformation.
 **/
G_CONST_RETURN char *
beagle_scheduler_information_to_human_readable_string (BeagleSchedulerInformation *sched_info)
{
	int pos;
	char *task;
	GSList *iter;
	GString *tmp = g_string_new (NULL);

	g_string_append_printf (tmp, "Scheduler:\nCount: %d\n", sched_info->total_task_count);

	if (sched_info->status_string)
		g_string_append_printf (tmp, "Status: %s\n", sched_info->status_string);

	pos = 1;

	g_string_append (tmp, "\nPending Tasks:\n");
	if (g_slist_length (sched_info->pending_task) > 0) {
		iter = sched_info->pending_task;
		while (iter != NULL) {
			task = iter->data;
			g_string_append_printf (tmp, "%d %s\n", pos, task);
			iter = iter->next;
			pos ++;
		}
	} else
		g_string_append (tmp, "Scheduler queue is empty.\n");

	if (g_slist_length (sched_info->future_task) > 0) {
		g_string_append (tmp, "\nFuture Tasks:\n");
		iter = sched_info->future_task;
		while (iter != NULL) {
			task = iter->data;
			g_string_append_printf (tmp, "%s\n", task);
			iter = iter->next;
		}
	}

	if (g_slist_length (sched_info->blocked_task) > 0) {
		g_string_append (tmp, "\nBlocked Tasks:\n");
		iter = sched_info->blocked_task;
		while (iter != NULL) {
			task = iter->data;
			g_string_append_printf (tmp, "%s\n", task);
			iter = iter->next;
		}
	}

	return g_string_free (tmp, FALSE);
}

void _task_to_xml (GSList *task_list, const char *list_name, const char *list_item_name, GString *data)
{
	char *task_data;
	GSList *iter;

	g_string_append_printf (data, "<%s>", list_name);
	
	if (task_list != NULL) {
		iter = task_list;
		while (iter != NULL) {
			task_data = iter->data;
			g_string_append_printf (data, "<%s>%s</%s>", list_item_name, task_data, list_item_name);
			iter = iter->next;
		}
	}
	
	g_string_append_printf (data, "</%s>", list_name);
}

void
_beagle_scheduler_information_to_xml (BeagleSchedulerInformation *sched_info, GString *data)
{
	char *tmp, *task;
	GSList *iter;

	g_string_append_printf (data, "<SchedulerInformation");

	g_string_append_printf (data, " TotalTaskCount=\"%d\"", sched_info->total_task_count);
	
	if (sched_info->status_string)
		g_string_append_printf (data, " StatusString=\"%s\"", sched_info->status_string);

	g_string_append (data, ">");

	_task_to_xml (sched_info->pending_task, "PendingTasks", "PendingTask", data);
	_task_to_xml (sched_info->future_task, "FutureTasks", "FutureTask", data);
	_task_to_xml (sched_info->blocked_task, "BlockedTasks", "BlockedTask", data);
		
	g_string_append (data, "</SchedulerInformation>");
}
