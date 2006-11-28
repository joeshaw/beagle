/*
 * beagle-queryable-status.c
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

#include "beagle-queryable-status.h"
#include "beagle-private.h"

BeagleQueryableStatus *
_beagle_queryable_status_new (void)
{
	BeagleQueryableStatus *status;

	status = g_new0 (BeagleQueryableStatus, 1);

	status->ref_count = 1;

	status->name = NULL;
	status->item_count = -1;
	status->state = BEAGLE_QUERYABLE_STATE_NA;
	status->progress_percent = -1;
	status->is_indexing = FALSE;

	return status;
}

/**
 * beagle_queryable_status_ref:
 * @status: a #BeagleQueryableStatus
 *
 * Increases the reference count of the #BeagleQueryableStatus.
 *
 * Return value: the #BeagleQueryableStatus
 **/
BeagleQueryableStatus *
beagle_queryable_status_ref (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, NULL);

	status->ref_count ++;

	return status;
}

/**
 * beagle_queryable_status_unref:
 * @status: a #BeagleQueryableStatus
 *
 * Decreases the reference count of the #BeagleQueryableStatus. When the reference count drops to 0, it is freed.
 **/
void
beagle_queryable_status_unref (BeagleQueryableStatus *status)
{
	g_return_if_fail (status != NULL);
	g_return_if_fail (status->ref_count > 0);

	status->ref_count --;

	if (status->ref_count == 0) {
		g_free (status->name);
		g_free (status);
	}
}

/**
 * beagle_queryable_status_get_name:
 * @status: a #BeagleQueryableStatus
 *
 * Fetches the name of the backend for the given #BeagleQueryableStatus.
 *
 * Return value: the name of the backend for the #BeagleQueryableStatus.
 **/
G_CONST_RETURN char *
beagle_queryable_status_get_name (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, NULL);

	return status->name;
}

/**
 * beagle_queryable_status_get_item_count:
 * @status: a #BeagleQueryableStatus
 *
 * Fetches the number of items in the backend for the given #BeagleQueryableStatus.
 *
 * Return value: the number of items in the backend for the #BeagleQueryableStatus.
 **/
int
beagle_queryable_status_get_item_count (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, -1);

	return status->item_count;
}

/**
 * beagle_queryable_status_get_state:
 * @status: a #BeagleQueryableStatus
 *
 * DEPRECATED: This function will be removed in a future version.  At
 * present, this function will always reutrn BEAGLE_QUERYABLE_STATE_NA.
 *
 * Return value: BEAGLE_QUERYABLE_STATE_NA.
 **/
BeagleQueryableState
beagle_queryable_status_get_state (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, BEAGLE_QUERYABLE_STATE_NA);

	return status->state;
}

/**
 * beagle_queryable_status_get_progress_percent:
 * @status: a #BeagleQueryableStatus
 *
 * Fetches the progress in percent of the backend for the given #BeagleQueryableStatus.
 *
 * Return value: the progress of the backend for the #BeagleQueryableStatus.
 **/
int
beagle_queryable_status_get_progress_percent (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, -1);

	return status->progress_percent;
}

/**
 * beagle_queryable_status_get_is_indexing:
 * @status: a #BeagleQueryableStatus
 *
 * Fetches whether the backend for the given #BeagleQueryableStatus is currently indexing.
 *
 * Return value: whether the backend for the #BeagleQueryableStatus is currently indexing.
 **/
gboolean
beagle_queryable_status_get_is_indexing (BeagleQueryableStatus *status)
{
	g_return_val_if_fail (status != NULL, FALSE);

	return status->is_indexing;
}

void
_beagle_queryable_status_to_xml (BeagleQueryableStatus *status, GString *data)
{
	char *tmp;

	g_string_append_printf (data, "<IndexInformation");

	if (status->name)
		g_string_append_printf (data, " Name=\"%s\"", status->name);
		
	g_string_append_printf (data, " ItemCount=\"%d\"", status->item_count);

	g_string_append_printf (data, " ProgressPercent=\"%d\"", status->progress_percent);

	g_string_append_printf (data, " IsIndexing=\"%s\"",
					status->is_indexing ? "true" : "false");

	g_string_append (data, ">");

	g_string_append (data, "</IndexInformation>");
}
