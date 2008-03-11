/*
 * beagle-daemon-information-response.c
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

#include "beagle-scheduler-information.h"
#include "beagle-queryable-status.h"
#include "beagle-daemon-information-response.h"
#include "beagle-private.h"

typedef struct {
	char *version;					/* Version. */
	gboolean is_indexing;				/* Currently indexing ? */
	BeagleSchedulerInformation *scheduler_information;	/* Current task information. */
	GSList *index_status;				/* List of BeagleQueryableStatus. */
} BeagleDaemonInformationResponsePrivate;

#define BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_DAEMON_INFORMATION_RESPONSE, BeagleDaemonInformationResponsePrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleDaemonInformationResponse, beagle_daemon_information_response, BEAGLE_TYPE_RESPONSE)

static void
beagle_daemon_information_response_finalize (GObject *obj)
{
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (obj);

	g_free (priv->version);

	if (priv->scheduler_information)
		beagle_scheduler_information_unref (priv->scheduler_information);

	if (priv->index_status) {
		g_slist_foreach (priv->index_status, (GFunc) beagle_queryable_status_unref, NULL);
		g_slist_free (priv->index_status);
	}

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
end_version (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	priv->version = _beagle_parser_context_get_text_buffer (ctx);
}

static void
end_is_indexing (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	char *buf;
	
	buf = _beagle_parser_context_get_text_buffer (ctx);

	priv->is_indexing = (strcmp (buf, "true") == 0);

	g_free (buf);
}

static void
start_scheduler_information (BeagleParserContext *ctx, const char **attrs)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	int i;
	BeagleSchedulerInformation *scheduler_information;

	scheduler_information = _beagle_scheduler_information_new ();

	for (i = 0; attrs[i] != NULL; i += 2) {
		if (strcmp (attrs[i], "TotalTaskCount") == 0)
			scheduler_information->total_task_count = (int) g_ascii_strtod (attrs[i + 1], NULL);
		else if (strcmp (attrs[i], "StatusString") == 0)
			scheduler_information->status_string = g_strdup (attrs[i + 1]);
		else
			g_warning ("unknown attribute \"%s\" with value \"%s\"", attrs[i], attrs[i + 1]);
	}

	priv->scheduler_information = scheduler_information;
}

static void
end_scheduler_information (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
}

static void
end_pending_task (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	char *buf;
	
	buf = _beagle_parser_context_get_text_buffer (ctx);

	priv->scheduler_information->pending_task = g_slist_prepend (priv->scheduler_information->pending_task, buf);
}

static void
end_pending_tasks (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	// Fix the order of tasks
	priv->scheduler_information->pending_task = g_slist_reverse (priv->scheduler_information->pending_task);
}

static void
end_future_task (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	char *buf;
	
	buf = _beagle_parser_context_get_text_buffer (ctx);

	priv->scheduler_information->future_task = g_slist_prepend (priv->scheduler_information->future_task, buf);
}

static void
end_future_tasks (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	// Fix the order of tasks
	priv->scheduler_information->future_task = g_slist_reverse (priv->scheduler_information->future_task);
}

static void
end_blocked_task (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	char *buf;
	
	buf = _beagle_parser_context_get_text_buffer (ctx);

	priv->scheduler_information->blocked_task = g_slist_prepend (priv->scheduler_information->blocked_task, buf);
}

static void
end_blocked_tasks (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	// Fix the order of tasks
	priv->scheduler_information->blocked_task = g_slist_reverse (priv->scheduler_information->blocked_task);
}

static void
end_index_status (BeagleParserContext *ctx)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);

	// Reverse the list to maintain the same order as returned by the daemon
	priv->index_status = g_slist_reverse (priv->index_status);
}

static void
start_queryable_status (BeagleParserContext *ctx, const char **attrs)
{
	BeagleDaemonInformationResponse *response = BEAGLE_DAEMON_INFORMATION_RESPONSE (_beagle_parser_context_get_response (ctx));
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);

	int i;
	BeagleQueryableStatus *queryable_status = _beagle_queryable_status_new ();

	for (i = 0; attrs[i] != NULL; i += 2) {	
		if (strcmp (attrs[i], "Name") == 0)
			queryable_status->name = g_strdup (attrs[i + 1]);
		else if (strcmp (attrs[i], "ItemCount") == 0)
			queryable_status->item_count = (int) g_ascii_strtod (attrs[i + 1], NULL);
		else if (strcmp (attrs[i], "ProgressPercent") == 0)
			queryable_status->progress_percent = (int) g_ascii_strtod (attrs[i + 1], NULL);
		else if (strcmp (attrs[i], "IsIndexing") == 0)
			queryable_status->is_indexing = strcmp (attrs[i + 1], "true") == 0;
		else
			g_warning ("could not handle %s", attrs[i]);
	}

	priv->index_status = g_slist_prepend (priv->index_status, queryable_status);
}

enum {
	PARSER_STATE_DAEMON_INFORMATION_VERSION,
	PARSER_STATE_DAEMON_INFORMATION_IS_INDEXING,
	PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION,
	PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_PENDING_TASKS,
	PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_FUTURE_TASKS,
	PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_BLOCKED_TASKS,
	PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_TASK,
	PARSER_STATE_DAEMON_INFORMATION_INDEX_STATUS,
	PARSER_STATE_DAEMON_INFORMATION_QUERYABLE_STATUS,
};

static BeagleParserHandler parser_handlers[] = {
	{ "Version",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_VERSION,
	  NULL,
	  end_version },

	{ "IsIndexing",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_IS_INDEXING,
	  NULL,
	  end_is_indexing },

	{ "SchedulerInformation",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION,
	  start_scheduler_information,
	  NULL},
	
	{ "PendingTasks",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_PENDING_TASKS,
	  NULL,
	  end_pending_tasks },
	
	{ "PendingTask",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_PENDING_TASKS,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_TASK,
	  NULL,
	  end_pending_task },
	
	{ "FutureTasks",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_FUTURE_TASKS,
	  NULL,
	  end_future_tasks },
	
	{ "FutureTask",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_FUTURE_TASKS,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_TASK,
	  NULL,
	  end_future_task },
	
	{ "BlockedTasks",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_BLOCKED_TASKS,
	  NULL,
	  end_blocked_tasks },
	
	{ "BlockedTask",
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_BLOCKED_TASKS,
	  PARSER_STATE_DAEMON_INFORMATION_SCHEDULER_INFORMATION_TASK,
	  NULL,
	  end_blocked_task },
	
	{ "IndexStatus",
	  -1,
	  PARSER_STATE_DAEMON_INFORMATION_INDEX_STATUS,
	  NULL,
	  end_index_status },

	{ "QueryableStatus",
	  PARSER_STATE_DAEMON_INFORMATION_INDEX_STATUS,
	  PARSER_STATE_DAEMON_INFORMATION_QUERYABLE_STATUS,
	  start_queryable_status,
	  NULL },
	{ 0 }
};

static void
beagle_daemon_information_response_class_init (BeagleDaemonInformationResponseClass *klass)
{
	GObjectClass        *obj_class = G_OBJECT_CLASS (klass);
	BeagleResponseClass *response_class = BEAGLE_RESPONSE_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_daemon_information_response_finalize;

	_beagle_response_class_set_parser_handlers (response_class,
						    parser_handlers);

	g_type_class_add_private (klass, sizeof (BeagleDaemonInformationResponsePrivate));
}

static void
beagle_daemon_information_response_init (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);

	priv->version = NULL;
	priv->scheduler_information = NULL;
	priv->index_status = NULL;
	priv->is_indexing = FALSE;
}

/**
 * beagle_daemon_information_response_get_version:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the version string of the given #BeagleDaemonInformationResponse.
 *
 * Return value: the version string of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_version (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	g_return_val_if_fail (priv->version, NULL);
	
	return priv->version;
}

/**
 * beagle_daemon_information_response_get_scheduler_information:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the current scheduler information from the given #BeagleDaemonInformationResponse.
 *
 * Return value: the current scheduler information from the #BeagleDaemonInformationResponse.
 **/
BeagleSchedulerInformation *
beagle_daemon_information_response_get_scheduler_information (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	g_return_val_if_fail (priv->scheduler_information, NULL);

	return priv->scheduler_information;
}

/**
 * beagle_daemon_information_response_get_human_readable_status:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the status string of the given #BeagleDaemonInformationResponse.
 *
 * Return value: the status of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_human_readable_status (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;
	BeagleSchedulerInformation *process_info;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	process_info = priv->scheduler_information;
	
	return beagle_scheduler_information_to_human_readable_string (process_info);
}

/**
 * beagle_daemon_information_response_get_index_status:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches the list of #QueryableStatus from each of the currently running backends.
 *
 * Return value: the index information of the #BeagleDaemonInformationResponse.
 **/
GSList *
beagle_daemon_information_response_get_index_status (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);

	return priv->index_status;
}

/**
 * beagle_daemon_information_response_get_index_information:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Fetches a human-readable string describing the index information
 * of the given #BeagleDaemonInformationResponse.
 *
 * Return value: string describing the index information of the #BeagleDaemonInformationResponse.
 **/
G_CONST_RETURN char *
beagle_daemon_information_response_get_index_information (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;
	BeagleQueryableStatus *status;
	GString *tmp;
	GSList *iter;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), NULL);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);

	tmp = g_string_new ("\n");

	for (iter = priv->index_status; iter != NULL; iter = iter->next) {
		status = iter->data;
		g_string_append_printf (tmp, "Name: %s\nCount: %d\nIndexing: %s\n\n",
						status->name,
						status->item_count,
						(status->is_indexing ? "Yes" : "No"));
	}

	return g_string_free (tmp, FALSE);
}

/**
 * beagle_daemon_information_response_is_indexing:
 * @response: a #BeagleDaemonInformationResponse
 *
 * Returns whether the daemon is in the process of indexing data.
 *
 * Return value: a boolean indicating whether the daemon is indexing.
 **/
gboolean
beagle_daemon_information_response_is_indexing (BeagleDaemonInformationResponse *response)
{
	BeagleDaemonInformationResponsePrivate *priv;

	g_return_val_if_fail (BEAGLE_IS_DAEMON_INFORMATION_RESPONSE (response), FALSE);

	priv = BEAGLE_DAEMON_INFORMATION_RESPONSE_GET_PRIVATE (response);
	
	return priv->is_indexing;
}
