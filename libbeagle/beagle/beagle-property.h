/*
 * beagle-property.h
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

#ifndef __BEAGLE_PROPERTY_H
#define __BEAGLE_PROPERTY_H

#include <glib.h>

typedef struct _BeagleProperty BeagleProperty;

BeagleProperty *beagle_property_new (const char *key, const char *value);
void beagle_property_free (BeagleProperty *prop);

G_CONST_RETURN char *beagle_property_get_key (BeagleProperty *prop);
void beagle_property_set_key (BeagleProperty *prop, const char *key);

G_CONST_RETURN char *beagle_property_get_value (BeagleProperty *prop);
void beagle_property_set_value (BeagleProperty *prop, const char *value);

gboolean beagle_property_get_is_searched (BeagleProperty *prop);
void beagle_property_set_is_searched (BeagleProperty *prop, gboolean is_searched);

gboolean beagle_property_get_is_keyword (BeagleProperty *prop);
void beagle_property_set_is_keyword (BeagleProperty *prop, gboolean is_keyword);

#endif /* __BEAGLE_PROPERTY_H */
