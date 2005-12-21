/*
 * beagle-hit.c
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

#include "beagle-hit.h"
#include "beagle-private.h"
#include "beagle-property.h"

/**
 * beagle_hit_get_uri:
 * @hit: a #BeagleHit
 * 
 * Fetches the URI of the given #BeagleHit.
 * 
 * Return value: the URI of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_uri (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->uri;
}

/**
 * beagle_hit_get_parent_uri:
 * @hit: a #BeagleHit
 * 
 * Fetches the parent URI of the given #BeagleHit.
 * 
 * Return value: the parent URI of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_parent_uri (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->parent_uri;
}

/**
 * beagle_hit_get_timestamp:
 * @hit: a #BeagleHit
 *
 * Fetches the timestamp of the given #BeagleHit.
 *
 * Return value: the timestamp as a string of the #BeagleHit.
 **/
BeagleTimestamp *
beagle_hit_get_timestamp (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->timestamp;
}

/**
 * beagle_hit_get_type:
 * @hit: a #BeagleHit
 *
 * Fetches the type of the given #BeagleHit.
 *
 * Return value: the type of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_type (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->type;
}

/**
 * beagle_hit_get_mime_type:
 * @hit: a #BeagleHit
 *
 * Fetches the mime type of the given #BeagleHit.
 *
 * Return value: the mime type of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_mime_type (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->mime_type;
}

/**
 * beagle_hit_get_source:
 * @hit: a #BeagleHit
 *
 * Fetches the source of the given #BeagleHit.
 *
 * Return value: the source of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_source (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return hit->source;
}

/**
 * beagle_hit_get_score:
 * @hit: a #BeagleHit
 *
 * Fetches the score of the given #BeagleHit.
 *
 * Return value: the score of the #BeagleHit.
 **/

double
beagle_hit_get_score (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, -1);

	return hit->score;
}

BeagleHit *
_beagle_hit_new (void)
{
	BeagleHit *hit = g_new0 (BeagleHit, 1);
	
	hit->ref_count = 1;

	hit->uri = NULL;
	hit->timestamp = NULL;
	hit->type = NULL;
	hit->mime_type = NULL;
	hit->source = NULL;

	return hit;
}

void
_beagle_hit_add_property (BeagleHit *hit, BeagleProperty *prop)
{
	if (!hit->properties)
		hit->properties = g_hash_table_new_full (g_str_hash, g_str_equal, NULL, (GDestroyNotify)beagle_property_free);

	g_hash_table_replace (hit->properties, prop->key, prop);
}


void 
_beagle_hit_list_free (GSList *list)
{
	g_slist_foreach (list, (GFunc)beagle_hit_unref, NULL);

	g_slist_free (list);
}

/**
 * beagle_hit_ref:
 * @hit: a #BeagleHit
 *
 * Increases the reference count of the #BeagleHit.
 *
 * Return value: the #BeagleHit.
 **/
BeagleHit * 
beagle_hit_ref (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	hit->ref_count ++;

	return hit;
}

/**
 * beagle_hit_unref:
 * @hit: a #BeagleHit.
 *
 * Decreases the reference count of the #BeagleHit. When its reference count drops to 0, it is freed.
 **/
void 
beagle_hit_unref (BeagleHit *hit)
{
	g_return_if_fail (hit != NULL);
	g_return_if_fail (hit->ref_count > 0);

	hit->ref_count--;

	if (hit->ref_count == 0) {
		g_free (hit->uri);
		g_free (hit->parent_uri);
		g_free (hit->type);
		g_free (hit->mime_type);
		g_free (hit->source);

		if (hit->timestamp)
			beagle_timestamp_free (hit->timestamp);

		if (hit->properties)
			g_hash_table_destroy (hit->properties);
		
		g_free (hit);
	}
}

/**
 * beagle_hit_get_property:
 * @hit: a #BeagleHit
 * @key: a string
 *
 * Fetches the value of the property @key of the given #BeagleHit.
 *
 * Return value: the value of property @key.
 **/ 
G_CONST_RETURN char *
beagle_hit_get_property (BeagleHit *hit, const char *key)
{
	BeagleProperty *property;

	g_return_val_if_fail (hit != NULL, NULL);
	g_return_val_if_fail (key != NULL, NULL);
	
	property = beagle_hit_lookup_property (hit, key);

	if (property) {
		return property->value;
	}

	return NULL;
}

/**
 * beagle_hit_lookup_property:
 * @hit: a #BeagleHit
 * @key: a string
 *
 * Fetches the property @key of the given #BeagleHit.
 *
 * Return value: the #BeagleProperty matching @key of the #BeagleHit.
 **/
BeagleProperty *
beagle_hit_lookup_property (BeagleHit *hit, const char *key)
{
	g_return_val_if_fail (hit != NULL, NULL);
	g_return_val_if_fail (key != NULL, NULL);

	if (!hit->properties)
		return NULL;

	return g_hash_table_lookup (hit->properties, key);
}

void 
_beagle_hit_to_xml (BeagleHit *hit, GString *data)
{
	char *tmp;

	if (hit->timestamp)
		tmp = _beagle_timestamp_to_string (hit->timestamp);
	else
		tmp = _beagle_timestamp_get_start ();

	g_string_append_printf (data, "<Hit Timestamp=\"%s\"",
				tmp);

	g_free (tmp);

	g_string_append_printf (data, " Uri=\"%s\" Type=\"%s\" MimeType=\"%s\"", 
				hit->uri, hit->type, hit->mime_type);

	g_string_append_printf (data, " Source=\"%s\"", 
				hit->source);

	g_string_append_printf (data, " Score=\"%f\"",
				hit->score);

	g_string_append (data, ">");

	_beagle_properties_to_xml (hit->properties, data);

	g_string_append (data, "</Hit>");

}
