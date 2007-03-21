/*
 * beagle-queryable-status.h
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

#ifndef __BEAGLE_QUERYABLE_STATUS_H
#define __BEAGLE_QUERYABLE_STATUS_H

#include <glib-object.h>

typedef struct _BeagleQueryableStatus BeagleQueryableStatus;

/* DEPRECATED: These will be removed in a future version */
typedef enum {
	BEAGLE_QUERYABLE_STATE_NA, /* Not Applicable */
	BEAGLE_QUERYABLE_STATE_IDLE,
	BEAGLE_QUERYABLE_STATE_CRAWLING,
	BEAGLE_QUERYABLE_STATE_INDEXING,
	BEAGLE_QUERYABLE_STATE_FLUSHING
} BeagleQueryableState;

BeagleQueryableStatus * beagle_queryable_status_ref (BeagleQueryableStatus *status);
void beagle_queryable_status_unref (BeagleQueryableStatus *status);

G_CONST_RETURN char *
beagle_queryable_status_get_name (BeagleQueryableStatus *status);

int
beagle_queryable_status_get_item_count (BeagleQueryableStatus *status);

BeagleQueryableState
beagle_queryable_status_get_state (BeagleQueryableStatus *status);

int
beagle_queryable_status_get_progress_percent (BeagleQueryableStatus *status);

gboolean
beagle_queryable_status_get_is_indexing (BeagleQueryableStatus *status);

#endif /* __BEAGLE_QUERYABLE_STATUS_H */

