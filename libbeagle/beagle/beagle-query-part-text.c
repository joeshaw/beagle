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
	BeagleQueryPartLogic logic;
} BeagleQueryPartTextPrivate;

#define BEAGLE_QUERY_PART_TEXT_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_TEXT, BeagleQueryPartTextPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartText, beagle_query_part_text, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_text_to_xml (BeagleQueryPartText *part, GError **err)
{
	BeagleQueryPartTextPrivate *priv;
	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, "Text", priv->logic);

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
beagle_query_part_text_finalize (GObject *obj)
{
        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_text_class_init (BeagleQueryPartTextClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->finalize = beagle_query_part_text_finalize;
        query_part_class->to_xml = beagle_query_part_text_to_xml;
	
        g_type_class_add_private (klass, sizeof (BeagleQueryPartTextPrivate));
}

static void
beagle_query_part_text_init (BeagleQueryPartText *part)
{
    
}

BeagleQueryPartText *
beagle_query_part_text_new (void)
{
        BeagleQueryPartText *part = g_object_new (BEAGLE_TYPE_QUERY_PART_TEXT, 0);
        return part;
}

void
beagle_query_part_text_set_text (BeagleQueryPartText *part,
				 const char          *text)
{
	BeagleQueryPartTextPrivate *priv;
	
	g_return_if_fail (text != NULL);
	
	priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->text = text;
}

void
beagle_query_part_text_set_search_full_text (BeagleQueryPartText *part,
					     gboolean            search_full_text)
{
	BeagleQueryPartTextPrivate *priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->search_full_text = search_full_text;
}

void
beagle_query_part_text_set_search_properties (BeagleQueryPartText *part,
					      gboolean            search_properties)
{
	BeagleQueryPartTextPrivate *priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->search_properties = search_properties;
}

void
beagle_query_part_text_set_logic (BeagleQueryPartText  *part,
				  BeagleQueryPartLogic logic)
{
	BeagleQueryPartTextPrivate *priv = BEAGLE_QUERY_PART_TEXT_GET_PRIVATE (part);    
	priv->logic = logic;
}
