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

#include <string.h>

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
	gboolean ret;
	const char *value;

	g_return_val_if_fail (hit != NULL, NULL);

	ret = beagle_hit_get_one_property (hit, "beagle:HitType", &value);
	g_return_val_if_fail (ret == TRUE, NULL);

	return value;
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
	gboolean ret;
	const char *value;

	g_return_val_if_fail (hit != NULL, NULL);

	ret = beagle_hit_get_one_property (hit, "beagle:MimeType", &value);
	g_return_val_if_fail (ret == TRUE, NULL);

	return value;
}

/**
 * beagle_hit_get_file_type:
 * @hit: a #BeagleHit
 *
 * For hits based on files, fetches the type of file for the given #BeagleHit.
 *
 * Return value: the file type of the #BeagleHit.
 **/
G_CONST_RETURN char *
beagle_hit_get_file_type (BeagleHit *hit)
{
	gboolean ret;
	const char *value;

	g_return_val_if_fail (hit != NULL, NULL);

	ret = beagle_hit_get_one_property (hit, "beagle:FileType", &value);
	g_return_val_if_fail (ret == TRUE, NULL);

	return value;
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
	gboolean ret;
	const char *value;

	g_return_val_if_fail (hit != NULL, NULL);

	ret = beagle_hit_get_one_property (hit, "beagle:Source", &value);
	g_return_val_if_fail (ret == TRUE, NULL);

	return value;
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

	hit->properties = NULL;

	return hit;
}

void
_beagle_hit_add_property (BeagleHit *hit, BeagleProperty *prop)
{
	hit->properties = g_slist_insert_sorted (hit->properties, prop, (GCompareFunc) _beagle_property_compare);
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

		if (hit->timestamp)
			beagle_timestamp_free (hit->timestamp);

		if (hit->properties) {
			g_slist_foreach (hit->properties, (GFunc) beagle_property_free, NULL);
			g_slist_free (hit->properties);
		}

		g_free (hit);
	}
}

/**
 * beagle_hit_get_one_property:
 * @hit: a #BeagleHit
 * @key: a string
 * @value: pointer to a string where value is stored
 *
 * Puts the value of the property @key of the given #BeagleHit in the string pointed to by @value.
 * The value of @value is set to NULL if FALSE is returned.
 *
 * This is a shortcut method for getting the value of a property when you know ahead of time that
 * only one property for a given key exists.  This function will fail if the key isn't found or
 * if there is more than one value for a given key.
 *
 * Return value: TRUE if exactly one property with @key was found, else FALSE.
 **/ 
gboolean
beagle_hit_get_one_property (BeagleHit *hit, const char *key, const char **value)
{
	BeagleProperty *property;
	GSList *pointer_first_property, *next;

	g_return_val_if_fail (hit != NULL, FALSE);
	g_return_val_if_fail (key != NULL, FALSE);
	g_return_val_if_fail (value != NULL, FALSE);

	*value = NULL;

	if (! hit->properties)
		return FALSE;
	
	pointer_first_property = g_slist_find_custom (hit->properties, key, (GCompareFunc) _beagle_property_key_compare);

	if (pointer_first_property == NULL)
		return FALSE;

	next = g_slist_next (pointer_first_property);

	if (next != NULL) {
		BeagleProperty *next_prop = (BeagleProperty *) next->data;
		const char *next_key = beagle_property_get_key (next_prop);

		if (strcmp (key, next_key) == 0)
			return FALSE;
	}

	property = (BeagleProperty *) pointer_first_property->data;
	*value = beagle_property_get_value (property);

	return TRUE;
}

/**
 * beagle_hit_get_properties:
 * @hit: a #BeagleHit
 * @key: a string
 *
 * Fetches all values of the property @key of the given #BeagleHit.
 *
 * Return value: A list of values (char *) of the of property @key.  The values
 * contained within the list should not be freed.
 **/ 
GSList *
beagle_hit_get_properties (BeagleHit *hit, const char *key)
{
	GSList *property_list = NULL;
	GSList *iterator_properties;

	g_return_val_if_fail (hit != NULL, NULL);
	g_return_val_if_fail (key != NULL, NULL);
	
	if (! hit->properties)
		return NULL;
	
	iterator_properties = g_slist_find_custom (hit->properties, key, (GCompareFunc) _beagle_property_key_compare);

	while (iterator_properties != NULL) {
		BeagleProperty *property = (BeagleProperty *) iterator_properties->data;
		const char *prop_key = beagle_property_get_key (property);
		
		if (strcmp (prop_key, key) != 0)
			break;

		property_list = g_slist_prepend (property_list, (gpointer) beagle_property_get_value (property));

		iterator_properties = g_slist_next (iterator_properties);
	}

	return property_list;
}

/**
 * beagle_hit_get_all_properties:
 * @hit: a #BeagleHit
 *
 * Fetches all properties of the given #BeagleHit
 *
 * Return value: A list of all properties (BeagleProperty *) of @hit.  The values
 * contained within the list should not be freed.
 **/ 
GSList *
beagle_hit_get_all_properties (BeagleHit *hit)
{
	g_return_val_if_fail (hit != NULL, NULL);

	return g_slist_copy (hit->properties);
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

	g_string_append_printf (data, " Uri=\"%s\"", hit->uri);

	if (hit->parent_uri)
		g_string_append_printf (data, " ParentUri=\"%s\"", 
				hit->parent_uri);

	/* Temporarily set the locale to "C" to convert floating point numbers. */
	char * old_locale = _beagle_util_set_c_locale ();
	g_string_append_printf (data, " Score=\"%f\"",
				hit->score);
	_beagle_util_reset_locale (old_locale);

	g_string_append (data, ">");

	_beagle_properties_to_xml (hit->properties, data);

	g_string_append (data, "</Hit>");

}
