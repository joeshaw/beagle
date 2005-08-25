/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-date.h
 *
 * Copyright (C) 2005 Novell, Inc.
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

#ifndef __BEAGLE_QUERY_PART_DATE_H
#define __BEAGLE_QUERY_PART_DATE_H

#include <glib.h>

#include "beagle-query-part.h"

#define BEAGLE_TYPE_QUERY_PART_DATE            (beagle_query_part_date_get_type ())
#define BEAGLE_QUERY_PART_DATE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_QUERY_PART_DATE, BeagleQueryPartDate))
#define BEAGLE_QUERY_PART_DATE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_QUERY_PART_DATE, BeagleQueryPartDateClass))
#define BEAGLE_IS_QUERY_PART_DATE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_QUERY_PART_DATE))
#define BEAGLE_IS_QUERY_PART_DATE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_QUERY_PART_DATE))
#define BEAGLE_QUERY_PART_DATE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_QUERY_PART_DATE, BeagleQueryPartDateClass))

typedef struct {
	BeagleQueryPart parent;
} BeagleQueryPartDate;

typedef struct {
        BeagleQueryPartClass parent_class;
} BeagleQueryPartDateClass;

GType                 beagle_query_part_date_get_type    (void);
BeagleQueryPartDate * beagle_query_part_date_new         (void);
void                  beagle_query_part_date_free        (BeagleQueryPartDate *part);

void                  beagle_query_part_date_set_start_date (BeagleQueryPartDate *part,
							     BeagleTimestamp *start_date);
void                  beagle_query_part_date_set_end_date   (BeagleQueryPartDate *part,
							     BeagleTimestamp *end_date);
#endif /* __BEAGLE_QUERY_PART_DATE_H */
