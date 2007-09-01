/*
 * beagle-daemon-information-request.c
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

#include <stdlib.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <sys/un.h>

#include "beagle-private.h"
#include "beagle-daemon-information-request.h"
#include "beagle-daemon-information-response.h"

typedef struct {
	gboolean get_version : 1;
	gboolean get_sched_info : 1;
	gboolean get_index_status : 1;
	gboolean get_is_indexing : 1;
} BeagleDaemonInformationRequestPrivate;

#define BEAGLE_DAEMON_INFORMATION_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, BeagleDaemonInformationRequestPrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_daemon_information_request_to_xml (BeagleRequest *request, GError **err)
{
	BeagleDaemonInformationRequestPrivate *priv = BEAGLE_DAEMON_INFORMATION_REQUEST_GET_PRIVATE (request);
	GString *data = g_string_new (NULL);

	_beagle_request_append_standard_header (data, 
						"DaemonInformationRequest");

	if (priv->get_version)
		g_string_append (data, "<GetVersion>true</GetVersion>");
	else
		g_string_append (data, "<GetVersion>false</GetVersion>");

	if (priv->get_sched_info)
		g_string_append (data, "<GetSchedInfo>true</GetSchedInfo>");
	else
		g_string_append (data, "<GetSchedInfo>false</GetSchedInfo>");

	if (priv->get_index_status)
		g_string_append (data, "<GetIndexStatus>true</GetIndexStatus>");
	else
		g_string_append (data, "<GetIndexStatus>false</GetIndexStatus>");

	if (priv->get_is_indexing)
		g_string_append (data, "<GetIsIndexing>true</GetIsIndexing>");
	else
		g_string_append (data, "<GetIsIndexing>false</GetIsIndexing>");

	_beagle_request_append_standard_footer (data);

	return data;
}

G_DEFINE_TYPE (BeagleDaemonInformationRequest, beagle_daemon_information_request, BEAGLE_TYPE_REQUEST)

static void
beagle_daemon_information_request_finalize (GObject *obj)
{
	if (G_OBJECT_CLASS (parent_class)->finalize) {
		G_OBJECT_CLASS (parent_class)->finalize (obj);
	}
}

static void
beagle_daemon_information_request_class_init (BeagleDaemonInformationRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_daemon_information_request_finalize;
	request_class->to_xml = beagle_daemon_information_request_to_xml;

	g_type_class_add_private (klass, sizeof (BeagleDaemonInformationRequestPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "DaemonInformationResponse",
						  BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE,
						  NULL);
}

static void
beagle_daemon_information_request_init (BeagleDaemonInformationRequest *daemon_information_request)
{
}

/**
 * beagle_daemon_information_request_new:
 *
 * Creates a new #BeagleDaemonInformationRequest requesting all fields.
 *
 * Return value: a newly created #BeagleDaemonInformationRequest.
 **/
BeagleDaemonInformationRequest *
beagle_daemon_information_request_new (void)
{
	return beagle_daemon_information_request_new_specific (TRUE, TRUE, TRUE, TRUE);
}

/**
 * beagle_daemon_information_request_new_specific:
 * @get_version: Whether to retrieve version of the daemon.
 * @get_sched_info: Whether to retrieve information about the current jobs in the daemon.
 * @get_index_status: Whether to retrieve information about the indexes.
 * @get_is_indexing: Whether to retrieve if any of the backends is doing any indexing now.
 *
 * Creates a new #BeagleDaemonInformationRequest allowing retrieval of specific fields.
 *
 * Return value: a newly created #BeagleDaemonInformationRequest.
 **/
BeagleDaemonInformationRequest *
beagle_daemon_information_request_new_specific (gboolean get_version,
						gboolean get_sched_info,
						gboolean get_index_status,
						gboolean get_is_indexing)
{
	BeagleDaemonInformationRequest *daemon_information_request = g_object_new (BEAGLE_TYPE_DAEMON_INFORMATION_REQUEST, 0);

	BeagleDaemonInformationRequestPrivate *priv
		= BEAGLE_DAEMON_INFORMATION_REQUEST_GET_PRIVATE (daemon_information_request);

	priv->get_version = get_version;
	priv->get_sched_info = get_sched_info;
	priv->get_index_status = get_index_status;
	priv->get_is_indexing = get_is_indexing;

	return daemon_information_request;
}
