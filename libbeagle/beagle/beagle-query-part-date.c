/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-date.c
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
#include "beagle-query-part-date.h"

typedef struct {
	char *key;
	BeagleTimestamp *start_date;
	BeagleTimestamp *end_date;
	BeagleQueryPartLogic logic;
} BeagleQueryPartDatePrivate;

#define BEAGLE_QUERY_PART_DATE_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_DATE, BeagleQueryPartDatePrivate))

static GObjectClass *parent_class = NULL;

static GString *
beagle_query_part_date_to_xml (BeagleQueryPart *part, GError **err)
{
	BeagleQueryPartDatePrivate *priv;
	priv = BEAGLE_QUERY_PART_DATE_GET_PRIVATE (part);    
	
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, "Date", priv->logic);
	
	g_string_append_printf (data, "<StartDate>%s</StartDate>", priv->start_date);    
	g_string_append_printf (data, "<EndDate>%s</EndDate>", priv->end_date);
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

G_DEFINE_TYPE (BeagleQueryPartDate, beagle_query_part_date, BEAGLE_TYPE_QUERY_PART)

static void
beagle_query_part_date_finalize (GObject *obj)
{
        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_date_class_init (BeagleQueryPartDateClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->finalize = beagle_query_part_date_finalize;
        query_part_class->to_xml = beagle_query_part_date_to_xml;

        g_type_class_add_private (klass, sizeof (BeagleQueryPartDatePrivate));
}

static void
beagle_query_part_date_init (BeagleQueryPartDate *part)
{

}

BeagleQueryPartDate *
beagle_query_part_date_new (void)
{
        BeagleQueryPartDate *part = g_object_new (BEAGLE_TYPE_QUERY_PART_DATE, 0);
        return part;
}

void
beagle_query_part_date_set_start_date (BeagleQueryPartDate *part,
				       BeagleTimestamp *start_date)
{
	BeagleQueryPartDatePrivate *priv = BEAGLE_QUERY_PART_DATE_GET_PRIVATE (part);    
	priv->start_date = start_date;
}

void
beagle_query_part_date_set_end_date (BeagleQueryPartDate *part,
				     BeagleTimestamp *end_date)
{
	BeagleQueryPartDatePrivate *priv = BEAGLE_QUERY_PART_DATE_GET_PRIVATE (part);    
	priv->end_date = end_date;
}

void
beagle_query_part_date_set_logic (BeagleQueryPartDate *part,
				  BeagleQueryPartLogic logic)
{
	BeagleQueryPartDatePrivate *priv = BEAGLE_QUERY_PART_DATE_GET_PRIVATE (part);    
	priv->logic = logic;
}
