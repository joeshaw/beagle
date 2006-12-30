/*
 * beagle-informational-messages-request.c
 *
 * Copyright (C) 2006 Novell, Inc.
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

#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-private.h"
#include "beagle-informational-messages-request.h"
#include "beagle-indexing-status-response.h"

enum {
	INDEXING_STATUS,
	LAST_SIGNAL
};

static GObjectClass *parent_class = NULL;
static guint signals [LAST_SIGNAL] = { 0 };

static GString *
beagle_informational_messages_request_to_xml (BeagleRequest *request, GError **err)
{
	GString *data = g_string_new (NULL);

	_beagle_request_append_standard_header (data, 
						"InformationalMessagesRequest");

	_beagle_request_append_standard_footer (data);

	return data;
}

static void
beagle_informational_messages_request_response (BeagleRequest *request, BeagleResponse *response)
{
	if (BEAGLE_IS_INDEXING_STATUS_RESPONSE (response))
		g_signal_emit (request, signals [INDEXING_STATUS], 0, response);
}

G_DEFINE_TYPE (BeagleInformationalMessagesRequest, beagle_informational_messages_request, BEAGLE_TYPE_REQUEST)

static void
beagle_informational_messages_request_class_init (BeagleInformationalMessagesRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	request_class->to_xml = beagle_informational_messages_request_to_xml;
	request_class->response = beagle_informational_messages_request_response;

	signals [INDEXING_STATUS] =
		g_signal_new ("indexing_status",
			      G_TYPE_FROM_CLASS (klass),
			      G_SIGNAL_RUN_LAST,
			      G_STRUCT_OFFSET (BeagleInformationalMessagesRequestClass, indexing_status),
			      NULL, NULL,
			      g_cclosure_marshal_VOID__OBJECT,
			      G_TYPE_NONE, 1,
			      BEAGLE_TYPE_INDEXING_STATUS_RESPONSE);

	_beagle_request_class_set_response_types (request_class,
						  "IndexingStatusResponse",
						  BEAGLE_TYPE_INDEXING_STATUS_RESPONSE,
						  NULL);
}

static void
beagle_informational_messages_request_init (BeagleInformationalMessagesRequest *informational_messages_request)
{
}

/*
 * beagle_informational_messages_request_new:
 *
 * Creates a new #BeagleInformationalMessagesRequest message.  You will need to
 * connect to the signals on this message for it to be useful.
 *
 * Return value: a newly created #BeagleInformationalMessagesRequest.
 **/
BeagleInformationalMessagesRequest *
beagle_informational_messages_request_new (void)
{
	return g_object_new (BEAGLE_TYPE_INFORMATIONAL_MESSAGES_REQUEST, 0);
}

