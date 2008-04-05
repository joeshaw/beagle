/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-text.c
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
#include "beagle-query-part-text.h"

typedef struct {
	char *text;
	gboolean search_full_text : 1;
	gboolean search_properties : 1;
} BeagleQueryPartTextPrivate;

#define BEAGLE_QUERY_PART_TEXT_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_TEXT, BeagleQueryPartTextPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartText, beagle_query_part_text, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_text_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartTextPrivate *priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, part, "Text");

	g_string_append (data, "<Text>");
	g_string_append (data, priv->text);
	g_string_append (data, "</Text>");
	
	if (priv->search_full_text)
		g_string_append (data, "<SearchFullText>true</SearchFullText>");
	else
		g_string_append (data, "<SearchFullText>false</SearchFullText>");
	
	if (priv->search_properties)
		g_string_append (data, "<SearchTextProperties>true</SearchTextProperties>");
	else
		g_string_append (data, "<SearchTextProperties>false</SearchTextProperties>");
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_text_constructed (GObject *obj)
{
	BeagleQueryPartText *part = BEAGLE_QUERY_PART_TEXT (obj);
	BeagleQueryPartTextPrivate *priv;

	if (G_OBJECT_CLASS (parent_class)->constructed)
		G_OBJECT_CLASS (parent_class)->constructed (obj);

	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);
	priv->text = NULL;
}

static void
beagle_query_part_text_finalize (GObject *obj)
{
	BeagleQueryPartText *part = BEAGLE_QUERY_PART_TEXT (obj);
	BeagleQueryPartTextPrivate *priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);

	g_free (priv->text);

        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_text_class_init (BeagleQueryPartTextClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->constructed = beagle_query_part_text_constructed;
        obj_class->finalize = beagle_query_part_text_finalize;
        query_part_class->to_xml = beagle_query_part_text_to_xml;
	
        g_type_class_add_private (klass, sizeof (BeagleQueryPartTextPrivate));
}

static void
beagle_query_part_text_init (BeagleQueryPartText *part)
{
    
}

/**
 * beagle_query_part_text_new:
 *
 * Creates a new #BeagleQueryPartText.
 *
 * Return value: a newly created #BeagleQueryPartText.
 */
BeagleQueryPartText *
beagle_query_part_text_new (void)
{
        BeagleQueryPartText *part = g_object_new (BEAGLE_TYPE_QUERY_PART_TEXT, 0);
        return part;
}

/**
 * beagle_query_part_text_set_text:
 * @part: a #BeagleQueryPartText
 * @text: a #const char *
 *
 * Sets the text to search for in a #BeagleQueryPartText.  This should only
 * be used for programmatically built queries, because it does not use the
 * query language and doesn't handle things like "OR".  If you are getting
 * input from a user, you should use #BeagleQueryPartHuman instead.
 **/
void
beagle_query_part_text_set_text (BeagleQueryPartText *part,
				 const char          *text)
{
	BeagleQueryPartTextPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_TEXT (part));
	g_return_if_fail (text != NULL);
	
	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	g_free (priv->text);
	priv->text = g_strdup (text);
}

/**
 * beagle_query_part_text_set_search_full_text:
 * @part: a #BeagleQueryPartText
 * @search_full_text: a #gboolean
 *
 * Sets whether to search the full text of documents to find the text part
 * of this #BeagleQueryPartText.
 **/
void
beagle_query_part_text_set_search_full_text (BeagleQueryPartText *part,
					     gboolean            search_full_text)
{
	BeagleQueryPartTextPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_TEXT (part));

	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->search_full_text = search_full_text;
}

/**
 * beagle_query_part_text_set_search_properties:
 * @part: a #BeagleQueryPartText
 * @search_properties: a #gboolean
 *
 * Sets whether to search the properties of documents to find the text part
 * of this #BeagleQueryPartText.
 **/
void
beagle_query_part_text_set_search_properties (BeagleQueryPartText *part,
					      gboolean            search_properties)
{
	BeagleQueryPartTextPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_TEXT (part));

	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->search_properties = search_properties;
}

