/*
 * beagle-indexable.c
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

#include "beagle-indexable.h"
#include "beagle-private.h"
#include "beagle-timestamp.h"

struct _BeagleIndexable {
	char *uri;
	char *content_uri;
	char *hot_content_uri;

	BeagleTimestamp *timestamp;
	long revision;

	gboolean delete_content;
	gboolean crawled;
	gboolean no_content;
	gboolean cache_content;
	
	BeagleIndexableFiltering filtering;

	char *type;
	char *mime_type;

	GHashTable *properties;
};

/**
 * beagle_indexable_new:
 * @uri: a string
 *
 * Creates a new #BeagleIndexable for the given @uri.
 *
 * Return value: the newly created #BeagleIndexable.
 **/
BeagleIndexable *
beagle_indexable_new (const char *uri)
{
	BeagleIndexable *indexable;

	g_return_val_if_fail (uri != NULL, NULL);

	indexable = g_new0 (BeagleIndexable, 1);
	indexable->uri = g_strdup (uri);
	
	indexable->revision = -1;
	indexable->crawled = TRUE;
	indexable->cache_content = TRUE;
	indexable->type = g_strdup ("File");

	indexable->filtering = BEAGLE_INDEXABLE_FILTERING_AUTOMATIC;

	return indexable;
}

/**
 * beagle_indexable_free:
 * @indexable: a #BeagleIndexable
 *
 * Frees the memory allocated by the #BeagleIndexable.
 **/
void
beagle_indexable_free (BeagleIndexable *indexable)
{
	g_return_if_fail (indexable != NULL);

	if (indexable->timestamp)
		beagle_timestamp_free (indexable->timestamp);

	g_free (indexable->uri);
	g_free (indexable->content_uri);
	g_free (indexable->hot_content_uri);
	
	g_free (indexable->type);
	g_free (indexable->mime_type);

	if (indexable->properties)
		g_hash_table_destroy (indexable->properties);

	g_free (indexable);
}

/**
 * beagle_indexable_add_property:
 * @indexable: a #BeagleIndexable
 * @prop: a #BeagleProperty
 *
 * Adds the #BeagleProperty to the given #BeagleIndexable.
 **/
void
beagle_indexable_add_property (BeagleIndexable *indexable, BeagleProperty *prop)
{
	g_return_if_fail (indexable != NULL);
	g_return_if_fail (prop != NULL);

	if (!indexable->properties)
		indexable->properties = g_hash_table_new_full (g_str_hash, g_str_equal, NULL, (GDestroyNotify)beagle_property_free);
	g_hash_table_replace (indexable->properties, prop->key, prop);
}

/**
 * beagle_indexable_get_uri:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the URI for the given #BeagleIndexable.
 *
 * Return value: the URI of the #BeagleIndexable.
 **/
G_CONST_RETURN char *
beagle_indexable_get_uri (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);

	return indexable->uri;
}

/**
 * beagle_indexable_set_uri:
 * @indexable: a #BeagleIndexable
 * @uri: a string
 *
 * Sets the URI of the #BeagleIndexable to @uri.
 **/
void
beagle_indexable_set_uri (BeagleIndexable *indexable, const char *uri)
{
	g_return_if_fail (indexable != NULL);
	g_return_if_fail (uri != NULL);

	g_free (indexable->uri);
	indexable->uri = g_strdup (uri);
}

/**
 * beagle_indexable_get_content_uri:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the content URI for the given #BeagleIndexable.
 *
 * Return value: the content URI of the #BeagleIndexable.
 **/
G_CONST_RETURN char *
beagle_indexable_get_content_uri (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);

	return indexable->content_uri;
}

/**
 * beagle_indexable_set_content_uri:
 * @indexable: a #BeagleIndexable
 * @content_uri: a string
 *
 * Sets the content URI of the given #BeagleIndexable to @content_uri.
 **/
void
beagle_indexable_set_content_uri (BeagleIndexable *indexable, const char *content_uri)
{
	g_return_if_fail (indexable != NULL);

	g_free (indexable->content_uri);
	indexable->content_uri = g_strdup (content_uri);
}

/**
 * beagle_indexable_get_hot_content_uri:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the hot content URI of the given #BeagleIndexable.
 *
 * Return value: the hot content URI of the #BeagleIndexable.
 **/
G_CONST_RETURN char *
beagle_indexable_get_hot_content_uri (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);

	return indexable->hot_content_uri;
}

/**
 * beagle_indexable_set_hot_content_uri:
 * @indexable: a #BeagleIndexable
 * @hot_content_uri: a string
 *
 * Sets the hot content URI of the given #BeagleIndexable to @hot_content_uri.
 **/
void
beagle_indexable_set_hot_content_uri (BeagleIndexable *indexable, const char *hot_content_uri)
{
	g_return_if_fail (indexable != NULL);

	g_free (indexable->hot_content_uri);
	indexable->hot_content_uri = g_strdup (hot_content_uri);
}

/**
 * beagle_indexable_get_delete_content:
 * @indexable: a #BeagleIndexable
 *
 * Fetches whether content of the given #BeagleIndexable should be deleted after it has been indexed.
 *
 * Return value: whether content should be deleted for the #BeagleIndexable.
 **/
gboolean 
beagle_indexable_get_delete_content (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, FALSE);

	return indexable->delete_content;
}

/**
 * beagle_indexable_set_delete_content:
 * @indexable: a #BeagleIndexable
 * @delete_content: a boolean
 *
 * Sets whether content of the given #BeagleIndexable should be deleted after it has been indexed.
 **/
void
beagle_indexable_set_delete_content (BeagleIndexable *indexable, gboolean delete_content)
{
	g_return_if_fail (indexable != NULL);

	indexable->delete_content = delete_content != FALSE;
}

/**
 * beagle_indexable_get_crawled:
 * @indexable: a #BeagleIndexable
 *
 * Fetches whether the given #BeagleIndexable is in crawl mode. 
 *
 * Return value: whether the #BeagleIndexable is crawled.
 **/
gboolean
beagle_indexable_get_crawled (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, FALSE);

	return indexable->crawled;
}

/**
 * beagle_indexable_set_crawled:
 * @indexable: a #BeagleIndexable
 * @crawled: a boolean
 *
 * Sets whether the given #BeagleIndexable is in crawl mode.
 **/
void 
beagle_indexable_set_crawled (BeagleIndexable *indexable, gboolean crawled)
{
	g_return_if_fail (indexable != NULL);

	indexable->crawled = crawled != FALSE;
}

/**
 * beagle_indexable_get_no_content:
 * @indexable: a #BeagleIndexable
 *
 * Fetches whether the given #BeagleIndexable has no content.
 *
 * Return value: whether the #BeagleIndexable has no content.
 **/
gboolean
beagle_indexable_get_no_content (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, FALSE);
	
	return indexable->no_content;
}

/**
 * beagle_indexable_set_no_content:
 * @indexable: a #BeagleIndexable
 * @no_content: a boolean
 *
 * Sets whether the given #BeagleIndexable has no content.
 **/
void
beagle_indexable_set_no_content (BeagleIndexable *indexable, 
				 gboolean         no_content)
{
	g_return_if_fail (indexable != NULL);

	indexable->no_content = no_content != FALSE;
}


/**
 * beagle_indexable_get_cache_content:
 * @indexable: a #BeagleIndexable
 *
 * Fetches whether the given #BeagleIndexable consists of cached contents.
 *
 * Return value: whether the #BeagleIndexable contains cached contents.
 **/
gboolean
beagle_indexable_get_cache_content (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, FALSE);
	
	return indexable->cache_content;
}

/**
 * beagle_indexable_set_cache_content:
 * @indexable: a #BeagleIndexable
 * @cache_content: a boolean
 *
 * Sets whether the given #BeagleIndexable consists of cached contents.
 **/
void
beagle_indexable_set_cache_content (BeagleIndexable *indexable, gboolean cache_content)
{
	g_return_if_fail (indexable != NULL);

	indexable->cache_content = cache_content != FALSE;
}


/**
 * beagle_indexable_get_filtering:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the #BeagleIndexableFiltering of the given #BeagleIndexable.
 *
 * Return value: Fetches the #BeagleIndexableFiltering of the #BeagleIndexable.
 **/
BeagleIndexableFiltering 
beagle_indexable_get_filtering (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, BEAGLE_INDEXABLE_FILTERING_AUTOMATIC);
	
	return indexable->filtering;
}

/**
 * beagle_indexable_set_filtering:
 * @indexable: a #BeagleIndexable
 * @filtering: a #BeagleIndexableFiltering
 *
 * Sets the #BeagleIndexableFiltering of the given #BeagleIndexable.
 **/
void 
beagle_indexable_set_filtering (BeagleIndexable *indexable, BeagleIndexableFiltering filtering)
{
	g_return_if_fail (indexable != NULL);
	g_return_if_fail (filtering >= BEAGLE_INDEXABLE_FILTERING_AUTOMATIC && filtering <= BEAGLE_INDEXABLE_FILTERING_NEVER);

	indexable->filtering = filtering;
}

/**
 * beagle_indexable_get_type:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the type of the given #BeagleIndexable.
 *
 * Return value: the type of the #BeagleIndexable.
 **/

G_CONST_RETURN char *
beagle_indexable_get_type (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);

	return indexable->type;
}

/**
 * beagle_indexable_set_type:
 * @indexable: a #BeagleIndexable
 * @type: a string
 *
 * Sets the type of the given #BeagleIndexable to @type.
 **/
void 
beagle_indexable_set_type (BeagleIndexable *indexable, const char *type)
{
	g_return_if_fail (indexable != NULL);

	g_free (indexable->type);
	indexable->type = g_strdup (type);
}


/**
 * beagle_indexable_get_mime_type:
 * @indexable: a #BeagleIndexable
 *
 * Fetches the mime type of the given #BeagleIndexable.
 *
 * Return value: the mime type of the #BeagleIndexable.
 **/
G_CONST_RETURN char *
beagle_indexable_get_mime_type (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);

	return indexable->mime_type;
}

/**
 * beagle_indexable_set_mime_type:
 * @indexable: a #BeagleIndexable
 * @mime_type: a string
 *
 * Sets the mime type of the given #BeagleIndexable to @mime_type.
 **/
void 
beagle_indexable_set_mime_type (BeagleIndexable *indexable, const char *mime_type)
{
	g_return_if_fail (indexable != NULL);

	g_free (indexable->mime_type);
	indexable->mime_type = g_strdup (mime_type);
}


BeagleTimestamp *
beagle_indexable_get_timestamp (BeagleIndexable *indexable)
{
	g_return_val_if_fail (indexable != NULL, NULL);
	
	return indexable->timestamp;
}

void
beagle_indexable_set_timestamp (BeagleIndexable *indexable,
				BeagleTimestamp *timestamp)
{
	g_return_if_fail (indexable != NULL);
	g_return_if_fail (timestamp != NULL);

	if (indexable->timestamp)
		beagle_timestamp_free (indexable->timestamp);

	indexable->timestamp = timestamp;
}

void
_beagle_indexable_to_xml (BeagleIndexable *indexable, GString *data)
{
	char *tmp;

	if (indexable->timestamp)
		tmp = _beagle_timestamp_to_string (indexable->timestamp);
	else
		tmp = _beagle_timestamp_get_start ();

	g_string_append_printf (data, "<Indexable Timestamp=\"%s\" Revision=\"%ld\" Uri=\"%s\"",
				tmp, indexable->revision, 
				indexable->uri);

	g_free (tmp);

	g_string_append_printf (data, " ContentUri=\"%s\" HotContentUri=\"%s\"",
				indexable->content_uri ? indexable->content_uri : indexable->uri,
				indexable->hot_content_uri ? indexable->hot_content_uri : "");

	g_string_append_printf (data, " DeleteContent=\"%s\" Crawled=\"%s\" NoContent=\"%s\" "
				"CacheContent=\"%s\"",
				indexable->delete_content ? "true" : "false",
				indexable->crawled ? "true" : "false",
				indexable->no_content ? "true" : "false",
				indexable->cache_content ? "true" : "false");

	switch (indexable->filtering) {
	case BEAGLE_INDEXABLE_FILTERING_ALWAYS:
		tmp = "Always";
		break;
	case BEAGLE_INDEXABLE_FILTERING_AUTOMATIC:
		tmp = "Automatic";
		break;
	case BEAGLE_INDEXABLE_FILTERING_NEVER:
		tmp = "Never";
		break;
	default:
		g_assert_not_reached ();
	}

	g_string_append_printf (data, " Filtering=\"%s\"", tmp);
	
	if (indexable->type)
		g_string_append_printf (data, " Type=\"%s\"", indexable->type);

	g_string_append (data, ">");

	_beagle_properties_to_xml (indexable->properties, data);

	g_string_append (data, "</Indexable>");
}
