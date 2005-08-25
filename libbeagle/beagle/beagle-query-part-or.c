/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-or.c
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
#include "beagle-query-part-or.h"

typedef struct {
	GSList *subparts;
} BeagleQueryPartOrPrivate;

#define BEAGLE_QUERY_PART_OR_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_OR, BeagleQueryPartOrPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartOr, beagle_query_part_or, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_or_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartOrPrivate *priv = BEAGLE_QUERY_PART_OR_GET_PRIVATE (part);    
	GString *data = g_string_new (NULL);
	GSList *iter;
	
	_beagle_query_part_append_standard_header (data, part, "Or");

	g_string_append (data, "<SubParts>");

	for (iter = priv->subparts; iter != NULL; iter = iter->next) {
		BeagleQueryPart *subpart = BEAGLE_QUERY_PART (iter->data);
		GString *sub_data;
		GError *sub_err = NULL;

		sub_data = _beagle_query_part_to_xml (subpart);
		g_string_append_len (data, sub_data->str, sub_data->len);
		g_string_free (sub_data, TRUE);
	}

	g_string_append (data, "</SubParts>");
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_or_finalize (GObject *obj)
{
	BeagleQueryPartOr *or = obj;
	BeagleQueryPartOrPrivate *priv = BEAGLE_QUERY_PART_OR_GET_PRIVATE (or);

	g_slist_foreach (priv->subparts, (GFunc) g_object_unref, NULL);
	g_slist_free (priv->subparts);

        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_or_class_init (BeagleQueryPartOrClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->finalize = beagle_query_part_or_finalize;
        query_part_class->to_xml = beagle_query_part_or_to_xml;
	
        g_type_class_add_private (klass, sizeof (BeagleQueryPartOrPrivate));
}

static void
beagle_query_part_or_init (BeagleQueryPartOr *part)
{
    
}

/**
 * beagle_query_part_or_new:
 *
 * Creates a new #BeagleQueryPartOr.
 *
 * Return value: the newly created #BeagleQueryPartOr.
 **/
BeagleQueryPartOr *
beagle_query_part_or_new (void)
{
        BeagleQueryPartOr *part = g_object_new (BEAGLE_TYPE_QUERY_PART_OR, 0);
        return part;
}

/**
 * beagle_query_part_or_add_subpart:
 * @part: a #BeagleQueryPartOr
 * @subpart: a #BeagleQueryPart
 *
 * Adds a #BeagleQueryPart as a subpart to the #BeagleQueryPartOr.  It takes ownership
 * of the subpart, so you should not unref it afterward.
 **/
void
beagle_query_part_or_add_subpart (BeagleQueryPartOr *part,
				  BeagleQueryPart   *subpart)
{
	BeagleQueryPartOrPrivate *priv;
	
	g_return_if_fail (BEAGLE_IS_QUERY_PART_OR (part));
	g_return_if_fail (BEAGLE_IS_QUERY_PART (subpart));
	
	priv = BEAGLE_QUERY_PART_OR_GET_PRIVATE (part);    
	priv->subparts = g_slist_append (priv->subparts, subpart);
}
