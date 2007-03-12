/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-uri.c
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
#include "beagle-query-part-uri.h"

typedef struct {
	const char *uri;
} BeagleQueryPartUriPrivate;

#define BEAGLE_QUERY_PART_URI_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_URI, BeagleQueryPartUriPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartUri, beagle_query_part_uri, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_uri_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartUriPrivate *priv = BEAGLE_QUERY_PART_URI_GET_PRIVATE (part);    
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, part, "Uri");

	g_string_append (data, "<Uri>");
	g_string_append (data, priv->uri);
	g_string_append (data, "</Uri>");
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_uri_finalize (GObject *obj)
{
        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_uri_class_init (BeagleQueryPartUriClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);

        parent_class = g_type_class_peek_parent (klass);

        obj_class->finalize = beagle_query_part_uri_finalize;
        query_part_class->to_xml = beagle_query_part_uri_to_xml;

        g_type_class_add_private (klass, sizeof (BeagleQueryPartUriPrivate));
}

static void
beagle_query_part_uri_init (BeagleQueryPartUri *part)
{
	
}

BeagleQueryPartUri *
beagle_query_part_uri_new (void)
{
        BeagleQueryPartUri *part = g_object_new (BEAGLE_TYPE_QUERY_PART_URI, 0);
        return part;
}

/**
 * beagle_query_part_uri_set_uri:
 * @part: a #BeagleQueryPartUri
 * @uri: a #const char *
 *
 * Sets the uri of a #BeagleQueryPartUri.  This should be used to obtain
 * beagle indexed metadata for a given uri. The uri should be properly escaped
 * and be exactly the same that beagle would return.
 *
 **/
void
beagle_query_part_uri_set_uri (BeagleQueryPartUri *part,
			       const char         *uri)
{
	BeagleQueryPartUriPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_URI (part));
	g_return_if_fail (uri != NULL);
	
	priv = BEAGLE_QUERY_PART_URI_GET_PRIVATE (part);    
	priv->uri = uri;
}
