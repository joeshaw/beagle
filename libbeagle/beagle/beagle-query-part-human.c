/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-human.c
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
#include "beagle-query-part-human.h"

typedef struct {
	const char *string;
} BeagleQueryPartHumanPrivate;

#define BEAGLE_QUERY_PART_HUMAN_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_HUMAN, BeagleQueryPartHumanPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartHuman, beagle_query_part_human, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_human_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartHumanPrivate *priv = BEAGLE_QUERY_PART_HUMAN_GET_PRIVATE (part);    
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, part, "Human");

	g_string_append (data, "<QueryString>");
	g_string_append (data, priv->string);
	g_string_append (data, "</QueryString>");
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_human_finalize (GObject *obj)
{
        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_human_class_init (BeagleQueryPartHumanClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);

        parent_class = g_type_class_peek_parent (klass);

        obj_class->finalize = beagle_query_part_human_finalize;
        query_part_class->to_xml = beagle_query_part_human_to_xml;

        g_type_class_add_private (klass, sizeof (BeagleQueryPartHumanPrivate));
}

static void
beagle_query_part_human_init (BeagleQueryPartHuman *part)
{
	
}

/**
 * beagle_query_part_human_new:
 *
 * Creates a new #BeagleQueryPartHuman.
 *
 * Return value: a newly created #BeagleQueryPartHuman.
 */
BeagleQueryPartHuman *
beagle_query_part_human_new (void)
{
        BeagleQueryPartHuman *part = g_object_new (BEAGLE_TYPE_QUERY_PART_HUMAN, 0);
        return part;
}

/**
 * beagle_query_part_human_set_string:
 * @part: a #BeagleQueryPartHuman
 * @string: a #const char *
 *
 * Sets the "human" string on a #BeagleQueryPartHuman.  This should be used
 * for user input as it can contain query modifiers like "OR".
 **/
void
beagle_query_part_human_set_string (BeagleQueryPartHuman *part,
				    const char           *string)
{
	BeagleQueryPartHumanPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_HUMAN (part));
	g_return_if_fail (string != NULL);
	
	priv = BEAGLE_QUERY_PART_HUMAN_GET_PRIVATE (part);    
	priv->string = string;
}
