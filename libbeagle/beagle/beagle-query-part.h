/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part.h
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

#ifndef __BEAGLE_QUERY_PART_H
#define __BEAGLE_QUERY_PART_H

#include <glib.h>

#define BEAGLE_QUERY_PART_TARGET_ALL "_all"

typedef enum {
        BEAGLE_QUERY_PART_LOGIC_REQUIRED = 1,
	BEAGLE_QUERY_PART_LOGIC_PROHIBITED = 2
} BeagleQueryPartLogic;

#define BEAGLE_TYPE_QUERY_PART            (beagle_query_part_get_type ())
#define BEAGLE_QUERY_PART(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_QUERY_PART, BeagleQueryPart))
#define BEAGLE_QUERY_PART_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_QUERY_PART, BeagleQueryPartClass))
#define BEAGLE_IS_QUERY_PART(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_QUERY_PART))
#define BEAGLE_IS_QUERY_PART_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_QUERY_PART))
#define BEAGLE_QUERY_PART_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_QUERY_PART, BeagleQueryPartClass))

typedef struct {
	GObject parent;
} BeagleQueryPart;

typedef struct {
	GObjectClass parent_class;
	
        GString *(* to_xml) (BeagleQueryPart *part);
} BeagleQueryPartClass;

GType    beagle_query_part_get_type  (void);

void     beagle_query_part_set_logic (BeagleQueryPart *part,
				      BeagleQueryPartLogic logic);


#endif /* __BEAGLE_QUERY_PART_H */
