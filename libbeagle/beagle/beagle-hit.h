/*
 * beagle-hit.h
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

#ifndef __BEAGLE_HIT_H
#define __BEAGLE_HIT_H

#include <glib.h>
#include <beagle/beagle-property.h>
#include <beagle/beagle-timestamp.h>

#define BEAGLE_HIT(x) ((BeagleHit *) x)

typedef struct _BeagleHit BeagleHit;

BeagleHit * beagle_hit_ref (BeagleHit *hit);
void beagle_hit_unref (BeagleHit *hit);

G_CONST_RETURN char *beagle_hit_get_uri (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_type (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_mime_type (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_file_type (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_source (BeagleHit *hit);
G_CONST_RETURN char *beagle_hit_get_parent_uri (BeagleHit *hit);

BeagleTimestamp *beagle_hit_get_timestamp (BeagleHit *hit);

double beagle_hit_get_score (BeagleHit *hit);

gboolean  beagle_hit_get_one_property   (BeagleHit   *hit,
					 const char  *key,
					 const char **value);
GSList   *beagle_hit_get_properties     (BeagleHit   *hit,
					 const char  *key);
GSList   *beagle_hit_get_all_properties (BeagleHit   *hit);

#endif /* __BEAGLE_HIT_H */

