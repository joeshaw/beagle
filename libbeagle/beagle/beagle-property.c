/*
 * beagle-property.c
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

#include "beagle-property.h"
#include "beagle-private.h"

/**
 * beagle_property_new:
 * @key: a string
 * @value: a string
 *
 * Creates a new #BeagleProperty for the key and value.
 *
 * Return value: a newly allocated #BeagleProperty.
 **/
BeagleProperty *
beagle_property_new (const char *key, const char *value)
{
	BeagleProperty *prop = g_new0 (BeagleProperty, 1);

	prop->key = g_strdup (key);
	prop->value = g_strdup (value);

	return prop;
}

/**
 * beagle_property_free:
 * @prop: a #BeagleProperty
 *
 * Frees the memory allocated for the #BeagleProperty.
 **/
void
beagle_property_free (BeagleProperty *prop)
{
	g_return_if_fail (prop != NULL);

	g_free (prop->key);
	g_free (prop->value);
	g_free (prop);
}

/**
 * beagle_property_get_key:
 * @prop: a #BeagleProperty
 *
 * Fetches the key of the #BeagleProperty.
 *
 * Return value: the key name of the #BeagleProperty.
 **/
G_CONST_RETURN char *
beagle_property_get_key (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, NULL);

	return prop->key;
}

/**
 * beagle_property_set_key:
 * @prop: a #BeagleProperty
 * @key: a string
 *
 * Sets the key of the given #BeagleProperty to @key.
 **/
void
beagle_property_set_key (BeagleProperty *prop, const char *key)
{
	g_return_if_fail (prop != NULL);

	g_free (prop->key);
	prop->key = g_strdup (key);
}

/**
 * beagle_property_get_value:
 * @prop: a #BeagleProperty
 *
 * Fetches the value of the given #BeagleProperty.
 *
 * Return Value: the value of the #BeagleProperty.
 **/
G_CONST_RETURN char *
beagle_property_get_value (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, NULL);

	return prop->value;
}

/**
 * beagle_property_set_value:
 * @prop: a #BeagleProperty
 * @value: a string
 *
 * Sets the value of the given #BeagleProperty to @value.
 **/
void
beagle_property_set_value (BeagleProperty *prop, const char *value)
{
	g_return_if_fail (prop != NULL);

	g_free (prop->value);
	prop->key = g_strdup (value);
}

/**
 * beagle_property_get_is_searched:
 * @prop: a #BeagleProperty
 *
 * Fetches whether the given #BeagleProperty is searched.
 *
 * Return value: whether the #BeagleProperty is searched.
 **/
gboolean 
beagle_property_get_is_searched (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, FALSE);

	return prop->is_searched;
}

/**
 * beagle_property_set_is_searched:
 * @prop: a #BeagleProperty
 * @is_searched: a boolean
 *
 * Sets whether the given #BeagleProperty is searched.
 **/
void
beagle_property_set_is_searched (BeagleProperty *prop, gboolean is_searched)
{
	g_return_if_fail (prop != NULL);

	prop->is_searched = is_searched != FALSE;
}

/**
 * beagle_property_get_is_keyword:
 * @prop: a #BeagleProperty
 *
 * Fetches whether the given #BeagleProperty is a keyword.
 *
 * Return value: whether the #BeagleProperty is a keyword.
 **/
gboolean 
beagle_property_get_is_keyword (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, FALSE);

	return prop->is_keyword;
}

/**
 * beagle_property_set_is_keyword:
 * @prop: a #BeagleProperty
 * @is_keyword: a boolean
 *
 * Sets whether the given #BeagleProperty is a keyword.
 **/
void
beagle_property_set_is_keyword (BeagleProperty *prop, gboolean is_keyword)
{
	g_return_if_fail (prop != NULL);

	prop->is_keyword = is_keyword != FALSE;
}

static void
prop_to_xml (gpointer       key,
	     gpointer       value,
	     gpointer       user_data)
{
	BeagleProperty *prop = value;
	char *tmp;
	GString *data = user_data;

	g_string_append (data, "<Property ");

	tmp = g_markup_printf_escaped ("isSearched=\"%s\" isKeyword=\"%s\" "
				       "Key=\"%s\" Value=\"%s\"/>",
				       prop->is_searched ? "true" : "false",
				       prop->is_keyword ? "true" : "false",
				       prop->key, prop->value);

	g_string_append (data, tmp);
	g_free (tmp);
}

void 
_beagle_properties_to_xml (GHashTable *properties, GString *data)
{
	g_string_append (data, "<Properties>");

	if (properties)
		g_hash_table_foreach (properties, prop_to_xml, data);

	g_string_append (data, "</Properties>");

}
