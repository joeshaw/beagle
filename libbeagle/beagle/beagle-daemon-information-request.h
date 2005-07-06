/*
 * beagle-daemon-information-request.h
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

#ifndef __BEAGLE_DAEMON_INFORMATION_REQUEST_H
#define __BEAGLE_DAEMON_INFORMATION_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-request.h>

#define BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST            (beagle_daemon_information_request_get_type ())
#define BEAGLE_DAEMON_INFORMATION_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequest))
#define BEAGLE_DAEMON_INFORMATION_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestClass))
#define BEAGLE_IS_DAEMON_INFORMATION_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST))
#define BEAGLE_IS_DAEMON_INFORMATION_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST))
#define BEAGLE_DAEMON_INFORMATION_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestClass))

typedef struct {
	BeagleRequest parent;
} BeagleDaemonInformationRequest;

typedef struct {
	BeagleRequestClass parent_class;
} BeagleDaemonInformationRequestClass;

GType        beagle_daemon_information_request_get_type     (void);
BeagleDaemonInformationRequest *beagle_daemon_information_request_new          (void);

#endif /* __BEAGLE_DAEMON_INFORMATION_REQUEST_H */
