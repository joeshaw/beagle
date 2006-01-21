/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

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

#include <string.h>

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
beagle_property_new (BeaglePropertyType type, const char *key, const char *value)
{
	BeagleProperty *prop = g_new0 (BeagleProperty, 1);

	prop->type = type;
	prop->key = g_strdup (key);
	prop->value = g_strdup (value);

	prop->is_searched = TRUE;
	prop->is_stored = TRUE;

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
 * beagle_property_get_type:
 * @prop: a #BeagleProperty
 *
 * Fetches the type of the #BeagleProperty.
 *
 * Return value: the #BeaglePropertyType of the #BeagleProperty.
 **/
BeaglePropertyType
beagle_property_get_type (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, BEAGLE_PROPERTY_TYPE_UNKNOWN);

	return prop->type;
}

/**
 * beagle_property_set_type:
 * @prop: a #BeagleProperty
 * @type: a #BeaglePropertyType
 *
 * Sets the type of the given #BeagleProperty to @type.
 **/
void
beagle_property_set_type (BeagleProperty *prop, BeaglePropertyType type)
{
	g_return_if_fail (prop != NULL);

	prop->type = type;
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
 * Sets whether the given #BeagleProperty is searched.  By default, properties
 * are searched.
 **/
void
beagle_property_set_is_searched (BeagleProperty *prop, gboolean is_searched)
{
	g_return_if_fail (prop != NULL);

	prop->is_searched = is_searched != FALSE;
}

/**
 * beagle_property_get_is_mutable:
 * @prop: a #BeagleProperty
 *
 * Fetches whether the given #BeagleProperty is mutable.
 *
 * Return value: whether the #BeagleProperty is mutable.
 **/
gboolean 
beagle_property_get_is_mutable (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, FALSE);

	return prop->is_mutable;
}

/**
 * beagle_property_set_is_mutable:
 * @prop: a #BeagleProperty
 * @is_mutable: a boolean
 *
 * Sets whether the given #BeagleProperty is mutable.
 **/
void
beagle_property_set_is_mutable (BeagleProperty *prop, gboolean is_mutable)
{
	g_return_if_fail (prop != NULL);

	prop->is_mutable = is_mutable != FALSE;
}

/**
 * beagle_property_get_is_stored:
 * @prop: a #BeagleProperty
 *
 * Fetches whether the given #BeagleProperty is stored in the index, or just a
 * hint to filters.
 *
 * Return value: whether the #BeagleProperty is stored.
 **/
gboolean 
beagle_property_get_is_stored (BeagleProperty *prop)
{
	g_return_val_if_fail (prop != NULL, FALSE);

	return prop->is_stored;
}

/**
 * beagle_property_set_is_stored:
 * @prop: a #BeagleProperty
 * @is_stored: a boolean
 *
 * Sets whether the given #BeagleProperty is stored in the index, or just a
 * hint to filters.  By default, properties are stored.
 **/
void
beagle_property_set_is_stored (BeagleProperty *prop, gboolean is_stored)
{
	g_return_if_fail (prop != NULL);

	prop->is_stored = is_stored != FALSE;
}

/**
 * Compares two BeagleProperty based on their keys.
 */
int
_beagle_property_compare (BeagleProperty *prop_a, BeagleProperty *prop_b)
{
    return strcmp (prop_a->key, prop_b->key);
}

/**
 * Compares a BeagleProperty (wrt its key) and another given key.
 * Useful when trying to search for a property with a given key
 * in a list of BeagleProperty elements.
 */
int _beagle_property_key_compare (BeagleProperty *prop_a, char *key)
{
    return strcmp (prop_a->key, key);
}

static const char * const property_types[] = {
	NULL,
	"Text",
	"Keyword",
	"Date"
};

static void
prop_to_xml (gpointer value, gpointer user_data)
{
	BeagleProperty *prop = value;
	char *tmp;
	GString *data = user_data;

	if (prop->type <= BEAGLE_PROPERTY_TYPE_UNKNOWN ||
	    prop->type >= BEAGLE_PROPERTY_TYPE_LAST)
		return;

	g_string_append (data, "<Property ");

	tmp = g_markup_printf_escaped ("Type=\"%s\" isSearched=\"%s\" isMutable=\"%s\" "
				       "Key=\"%s\" Value=\"%s\"/>",
				       property_types[prop->type],
				       prop->is_searched ? "true" : "false",
				       prop->is_mutable ? "true" : "false",
				       prop->key, prop->value);

	g_string_append (data, tmp);
	g_free (tmp);
}

void 
_beagle_properties_to_xml (GSList *properties, GString *data)
{
	g_string_append (data, "<Properties>");

	if (properties != NULL)
		g_slist_foreach (properties, prop_to_xml, data);

	g_string_append (data, "</Properties>");

}
