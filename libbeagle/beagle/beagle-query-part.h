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
#define BEAGLE_QUERY_PART_TARGET_TEXT "_text"
#define BEAGLE_QUERY_PART_TARGET_PROPERTIES "_prop"

typedef struct _BeagleQueryPart BeagleQueryPart;

BeagleQueryPart *beagle_query_part_new  (void);
void             beagle_query_part_free (BeagleQueryPart *part);

void beagle_query_part_set_target     (BeagleQueryPart *part,
				       const char      *target);
void beagle_query_part_set_text       (BeagleQueryPart *part,
				       const char      *text);
void beagle_query_part_set_keyword    (BeagleQueryPart *part,
				       gboolean         is_keyword);
void beagle_query_part_set_required   (BeagleQueryPart *part,
				       gboolean         is_required);
void beagle_query_part_set_prohibited (BeagleQueryPart *part,
				       gboolean         is_prohibited);

#endif /* __BEAGLE_QUERY_PART_H */
