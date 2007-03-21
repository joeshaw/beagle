/*
 * beagle-daemon-information-response.h
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

#ifndef __BEAGLE_DAEMON_INFORMATION_RESPONSE_H
#define __BEAGLE_DAEMON_INFORMATION_RESPONSE_H

#include <glib-object.h>

#include "beagle-response.h"
#include "beagle-scheduler-information.h"

#define BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE            (beagle_daemon_information_response_get_type ())
#define BEAGLE_DAEMON_INFORMATION_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponse))
#define BEAGLE_DAEMON_INFORMATION_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponseClass))
#define BEAGLE_IS_DAEMON_INFORMATION_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE))
#define BEAGLE_IS_DAEMON_INFORMATION_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE))
#define BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponseClass))

typedef struct _BeagleDaemonInformationResponse      BeagleDaemonInformationResponse;
typedef struct _BeagleDaemonInformationResponseClass BeagleDaemonInformationResponseClass;

struct _BeagleDaemonInformationResponse {
	BeagleResponse parent;
};

struct _BeagleDaemonInformationResponseClass {
	BeagleResponseClass parent_class;
};

GType        beagle_daemon_information_response_get_type     (void);

G_CONST_RETURN char *
beagle_daemon_information_response_get_version (BeagleDaemonInformationResponse *response);

BeagleSchedulerInformation *
beagle_daemon_information_response_get_scheduler_information (BeagleDaemonInformationResponse *response);

/* Kept for backward compatibility */
G_CONST_RETURN char *
beagle_daemon_information_response_get_human_readable_status (BeagleDaemonInformationResponse *response);

GSList *
beagle_daemon_information_response_get_index_status (BeagleDaemonInformationResponse *response);

G_CONST_RETURN char *
beagle_daemon_information_response_get_index_information (BeagleDaemonInformationResponse *response);

gboolean
beagle_daemon_information_response_is_indexing (BeagleDaemonInformationResponse *response);

#endif /* __BEAGLE_DAEMON_INFORMATION_RESPONSE_H */

