/*
 * beagle-request.h
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

#ifndef __BEAGLE_REQUEST_H
#define __BEAGLE_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-response.h>

#define BEAGLE_TYPE_REQUEST            (beagle_request_get_type ())
#define BEAGLE_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_REQUEST, BeagleRequest))
#define BEAGLE_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_REQUEST, BeagleRequestClass))
#define BEAGLE_IS_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_REQUEST))
#define BEAGLE_IS_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_REQUEST))
#define BEAGLE_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_REQUEST, BeagleRequestClass))

typedef struct {
	GObject parent;
} BeagleRequest;

typedef struct {
	GObjectClass parent_class;

	GHashTable *response_types;

	/* Virtual methods */
	GString *(* to_xml) (BeagleRequest *request, GError **err);

	/* Signals */
	void (* closed) (BeagleRequest *request);
	void (* response) (BeagleRequest *request, BeagleResponse *response);
	void (* error) (BeagleRequest *request, GError *error);
} BeagleRequestClass;

GType    beagle_request_get_type (void);


#endif /* __BEAGLE_REQUEST_H */
