/*
 * beagle-shutdown-request.h
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

#ifndef __BEAGLE_SHUTDOWN_REQUEST_H
#define __BEAGLE_SHUTDOWN_REQUEST_H

#include <glib-object.h>

#include "beagle-request.h"

#define BEAGLE_TYPE_SHUTDOWN_REQUEST            (beagle_shutdown_request_get_type ())
#define BEAGLE_SHUTDOWN_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequest))
#define BEAGLE_SHUTDOWN_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequestClass))
#define BEAGLE_IS_SHUTDOWN_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST))
#define BEAGLE_IS_SHUTDOWN_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_SHUTDOWN_REQUEST))
#define BEAGLE_SHUTDOWN_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_SHUTDOWN_REQUEST, BeagleShutdownRequestClass))

typedef struct _BeagleShutdownRequest      BeagleShutdownRequest;
typedef struct _BeagleShutdownRequestClass BeagleShutdownRequestClass;

struct _BeagleShutdownRequest {
	BeagleRequest parent;
};

struct _BeagleShutdownRequestClass {
	BeagleRequestClass parent_class;
};

GType        beagle_shutdown_request_get_type     (void);
BeagleShutdownRequest *beagle_shutdown_request_new          (void);

#endif /* __BEAGLE_SHUTDOWN_REQUEST_H */

