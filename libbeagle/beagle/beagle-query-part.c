/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part.c
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

typedef struct {
    BeagleQueryPartLogic logic;
} BeagleQueryPartPrivate;

#define BEAGLE_QUERY_PART_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART, BeagleQueryPartPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPart, beagle_query_part, G_TYPE_OBJECT)

static void
beagle_query_part_finalize (GObject *obj)
{
        BeagleQueryPartPrivate *priv = BEAGLE_QUERY_PART_GET_PRIVATE (obj);
	
	if (G_OBJECT_CLASS (parent_class)->finalize)
		G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void 
beagle_query_part_class_init (BeagleQueryPartClass *klass)
{
	GObjectClass *obj_class = G_OBJECT_CLASS (klass);
	
	parent_class = g_type_class_peek_parent (klass);
	
	obj_class->finalize = beagle_query_part_finalize;
	
	g_type_class_add_private (klass, sizeof (BeagleQueryPartPrivate));
}

static void
beagle_query_part_init (BeagleQueryPart *part)
{
	BeagleQueryPartPrivate *priv = BEAGLE_QUERY_PART_GET_PRIVATE (part);
	priv->logic = 0;
}

/**
 * beagle_query_part_set_logic:
 * @part: a #BeagleQueryPart
 * @logic: a value in enum #BeagleQueryLogic
 *
 * Set the #BeagleQueryLogic for a #BeagleQueryPart.  This is used to determine whether
 * this part should be required or prohibited.
 **/
void
beagle_query_part_set_logic (BeagleQueryPart *part,
			     BeagleQueryPartLogic logic)
{
	BeagleQueryPartPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART (part));

	priv = BEAGLE_QUERY_PART_GET_PRIVATE (part);

	priv->logic = logic;
}

GString *
_beagle_query_part_to_xml (BeagleQueryPart *part)
{
	g_return_val_if_fail (BEAGLE_IS_QUERY_PART (part), NULL);

	return BEAGLE_QUERY_PART_GET_CLASS (part)->to_xml (part);
}

void
_beagle_query_part_append_standard_header (GString *data,
					   BeagleQueryPart *part,
					   const char *xsi_type)
{
	BeagleQueryPartPrivate *priv = BEAGLE_QUERY_PART_GET_PRIVATE (part);

	g_string_append_printf (data, "<Part xsi:type=\"QueryPart_%s\">", xsi_type);

	switch (priv->logic) {
	    case BEAGLE_QUERY_PART_LOGIC_REQUIRED:
		    g_string_append (data, "<Logic>Required</Logic>");
		    break;
	    case BEAGLE_QUERY_PART_LOGIC_PROHIBITED:
		    g_string_append (data, "<Logic>Prohibited</Logic>");
		    break;
	}
}

void
_beagle_query_part_append_standard_footer (GString *data)
{
        g_string_append (data, "</Part>");
}
