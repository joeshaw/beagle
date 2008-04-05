/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-property.c
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

#include "beagle-private.h"
#include "beagle-query-part.h"
#include "beagle-query-part-property.h"

typedef struct {
	char *key;
	char *value;
	BeaglePropertyType prop_type;
} BeagleQueryPartPropertyPrivate;

#define BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_PROPERTY, BeagleQueryPartPropertyPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartProperty, beagle_query_part_property, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_property_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartPropertyPrivate *priv = BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE (part);    
	
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, part, "Property");
	
	switch (priv->prop_type) {
	case BEAGLE_PROPERTY_TYPE_TEXT:
		g_string_append (data, "<Type>Text</Type>");
		break;
	case BEAGLE_PROPERTY_TYPE_KEYWORD:
		g_string_append (data, "<Type>Keyword</Type>");
		break;
	case BEAGLE_PROPERTY_TYPE_DATE:
		g_string_append (data, "<Type>Date</Type>");
		break;
	default:
		g_assert_not_reached ();
	}

	g_string_append_printf (data, "<Key>%s</Key>", priv->key);
	g_string_append_printf (data, "<Value>%s</Value>", priv->value);
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_property_finalize (GObject *obj)
{
	BeagleQueryPartProperty *part = BEAGLE_QUERY_PART_PROPERTY (obj);
	BeagleQueryPartPropertyPrivate *priv = BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE (part);

	g_free (priv->key);
	g_free (priv->value);	/* No-op if NULL */

        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_property_class_init (BeagleQueryPartPropertyClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->finalize = beagle_query_part_property_finalize;
        query_part_class->to_xml = beagle_query_part_property_to_xml;
	
        g_type_class_add_private (klass, sizeof (BeagleQueryPartPropertyPrivate));
}

static void
beagle_query_part_property_init (BeagleQueryPartProperty *part)
{
}

/**
 * beagle_query_part_property_new:
 *
 * Creates a new #BeagleQueryPartProperty.
 *
 * Return value: a newly created #BeagleQueryPartProperty.
 */
BeagleQueryPartProperty *
beagle_query_part_property_new (void)
{
        BeagleQueryPartProperty *part = g_object_new (BEAGLE_TYPE_QUERY_PART_PROPERTY, 0);
        return part;
}

/**
 * beagle_query_part_property_set_key:
 * @part: a #BeagleQueryPartProperty
 * @key: a #const char *
 *
 * Sets the key of the #BeagleQueryPartProperty to be queried against.  For
 * example, "beagle:Type".
 **/
void
beagle_query_part_property_set_key (BeagleQueryPartProperty *part,
				    const char *key)
{
	BeagleQueryPartPropertyPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_PROPERTY (part));
	g_return_if_fail (key != NULL);

	priv = BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE (part);
	g_free (priv->key);
	priv->key = g_strdup (key);
}

/**
 * beagle_query_part_property_set_value:
 * @part: a #BeagleQueryPartProperty
 * @value: a #const char *
 *
 * Sets the value of the #BeagleQueryPartProperty to be queried for.  For
 * example, "MailMessage".
 **/
void
beagle_query_part_property_set_value (BeagleQueryPartProperty *part,
				      const char *value)
{
	BeagleQueryPartPropertyPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_PROPERTY (part));

	priv = BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE (part);
	g_free (priv->value);
	priv->value = g_strdup (value);
}

/**
 * beagle_query_part_property_set_property_type:
 * @part: a #BeagleQueryPartProperty
 * @prop_type: a value of #BeaglePropertyType
 *
 * Sets the property type of the #BeagleQueryPartProperty to be queried for.
 **/
void
beagle_query_part_property_set_property_type (BeagleQueryPartProperty *part,
					      BeaglePropertyType prop_type)
{
	BeagleQueryPartPropertyPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_PROPERTY (part));

	priv = BEAGLE_QUERY_PART_PROPERTY_GET_PRIVATE (part);
	priv->prop_type = prop_type;
}
