/*
 * beagle-indexing-service-request.c
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

#include "beagle-indexing-service-request.h"
#include "beagle-private.h"
#include "beagle-empty-response.h"

#include <libxml/parser.h>

typedef struct {
	char *source;
	GSList *to_add;
	GSList *to_remove;
} BeagleIndexingServiceRequestPrivate;

#define BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_INDEXING_SERVICE_REQUEST, BeagleIndexingServiceRequestPrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_indexing_service_request_to_xml (BeagleRequest *request, GError **err)
{
	BeagleIndexingServiceRequestPrivate *priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (request);
	GString *data = g_string_new (NULL);
	GSList *list;

	_beagle_request_append_standard_header (data, "IndexingServiceRequest");

	if (priv->source != NULL)
		g_string_append_printf (data, "<Source>%s</Source>", priv->source);

	g_string_append (data, "<ToAdd>");
	
	for (list = priv->to_add; list != NULL; list = list->next) {
		BeagleIndexable *indexable = list->data;

		_beagle_indexable_to_xml (indexable, data);
	}

	g_string_append (data, "</ToAdd>");

	g_string_append (data, "<ToRemove>");
	
	for (list = priv->to_remove; list != NULL; list = list->next) {
		char *str = list->data;

		g_string_append_printf (data, "<string>%s</string>", str);
	}
	g_string_append (data, "</ToRemove>");
	
	_beagle_request_append_standard_footer (data);

	return data;
}

G_DEFINE_TYPE (BeagleIndexingServiceRequest, beagle_indexing_service_request, BEAGLE_TYPE_REQUEST)

static void
beagle_indexing_service_request_finalize (GObject *obj)
{
	BeagleIndexingServiceRequestPrivate *priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (obj);

	g_free (priv->source);

	g_slist_foreach (priv->to_add, (GFunc) beagle_indexable_free, NULL);
	g_slist_free (priv->to_add);

	g_slist_foreach (priv->to_remove, (GFunc) g_free, NULL);
	g_slist_free (priv->to_remove);

	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_indexing_service_request_class_init (BeagleIndexingServiceRequestClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	BeagleRequestClass *request_class = BEAGLE_REQUEST_CLASS (klass);

	parent_class = g_type_class_peek_parent (klass);

	obj_class->finalize = beagle_indexing_service_request_finalize;
	request_class->to_xml = beagle_indexing_service_request_to_xml;

	g_type_class_add_private (klass, sizeof (BeagleIndexingServiceRequestPrivate));

	_beagle_request_class_set_response_types (request_class,
						  "EmptyResponse",
						  BEAGLE_TYPE_EMPTY_RESPONSE,
						  NULL);
}

static void
beagle_indexing_service_request_init (BeagleIndexingServiceRequest *indexing_service_request)
{
}

/**
 * beagle_indexing_service_request_new:
 *
 * Creates a new #BeagleIndexingServiceRequest.
 *
 * Return value: a newly created #BeagleIndexingServiceRequest.
 **/
BeagleIndexingServiceRequest *
beagle_indexing_service_request_new (void)
{
	BeagleIndexingServiceRequest *indexing_service_request = g_object_new (BEAGLE_TYPE_INDEXING_SERVICE_REQUEST, 0);

	BeagleIndexingServiceRequestPrivate *priv;
	priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (indexing_service_request);

	return indexing_service_request;
}

/**
 * beagle_indexing_service_request_add:
 * @request: a #BeagleIndexingServiceRequest
 * @indexable: a #BeagleIndexable
 *
 * Adds a #BeagleIndexable to the given #BeagleIndexingServiceRequest.
 **/
void
beagle_indexing_service_request_add (BeagleIndexingServiceRequest *request, BeagleIndexable *indexable)
{
	BeagleIndexingServiceRequestPrivate *priv;

	g_return_if_fail (BEAGLE_IS_INDEXING_SERVICE_REQUEST (request));
	g_return_if_fail (indexable != NULL);
	
	priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (request);
	
	priv->to_add = g_slist_prepend (priv->to_add, indexable);
}

/**
 * beagle_indexing_service_request_remove:
 * @request: a #BeagleIndexingServiceRequest
 * @uri: a string
 *
 * Adds the given @uri to the list of uris to be removed tothe given
 * #BeagleIndexingServiceRequest.
 **/
void 
beagle_indexing_service_request_remove (BeagleIndexingServiceRequest *request, const char *uri)
{
	BeagleIndexingServiceRequestPrivate *priv;

	g_return_if_fail (BEAGLE_IS_INDEXING_SERVICE_REQUEST (request));
	g_return_if_fail (uri != NULL);
	
	priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (request);
	
	priv->to_remove = g_slist_prepend (priv->to_remove, g_strdup (uri));
}

/**
 * beagle_indexing_service_request_set_source:
 * @request: a #BeagleIndexingServiceRequest
 * @source: the backend to send the request to
 *
 * Normally #BeagleIndexables sent through the indexing service are stored in a
 * dedicated index, the IndexingService index.  Sometimes, however, you might
 * want to send a #BeagleIndexable to another running backend.  A common
 * use-case for this is if you wanted to add or change metadata on an already
 * indexed document.  To do this, you would create a #BeagleIndexable with type
 * BEAGLE_INDEXABLE_TYPE_PROPERTY_CHANGE and set the source to your backend of
 * choice, like "Files".  The daemon will route your indexable to that backend
 * instead of the indexing service backend.  If the backend doesn't exist, you
 * will get a #BeagleErrorResponse.
 *
 **/
void
beagle_indexing_service_request_set_source (BeagleIndexingServiceRequest *request, const char *source)
{
	BeagleIndexingServiceRequestPrivate *priv;

	g_return_if_fail (BEAGLE_INDEXING_SERVICE_REQUEST (request));

	priv = BEAGLE_INDEXING_SERVICE_REQUEST_GET_PRIVATE (request);

	g_free (priv->source);
	priv->source = g_strdup (source);
}
