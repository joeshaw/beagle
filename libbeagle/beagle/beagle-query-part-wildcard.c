/* -*- Mode: C; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8; -*- */

/*
 * beagle-query-part-wildcard.c
 *
 * Copyright (C) 2005-2006 Novell, Inc.
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
#include "beagle-query-part-wildcard.h"

typedef struct {
	char *query_string;
} BeagleQueryPartWildcardPrivate;

#define BEAGLE_QUERY_PART_WILDCARD_GET_PRIVATE(o) (G_TYPE_INSTANCE_GET_PRIVATE ((o), BEAGLE_TYPE_QUERY_PART_WILDCARD, BeagleQueryPartWildcardPrivate))

static GObjectClass *parent_class = NULL;

G_DEFINE_TYPE (BeagleQueryPartWildcard, beagle_query_part_wildcard, BEAGLE_TYPE_QUERY_PART)

static GString *
beagle_query_part_wildcard_to_xml (BeagleQueryPart *part)
{
	BeagleQueryPartWildcardPrivate *priv = BEAGLE_QUERY_PART_WILDCARD_GET_PRIVATE (part);    
	GString *data = g_string_new (NULL);
	
	_beagle_query_part_append_standard_header (data, part, "Wildcard");

	g_string_append (data, "<QueryString>");
	g_string_append (data, priv->query_string);
	g_string_append (data, "</QueryString>");
	
	_beagle_query_part_append_standard_footer (data);
	
	return data;
}

static void
beagle_query_part_wildcard_finalize (GObject *obj)
{
	BeagleQueryPartWildcard *part = BEAGLE_QUERY_PART_WILDCARD (obj);
	BeagleQueryPartWildcardPrivate *priv = BEAGLE_QUERY_PART_WILDCARD_GET_PRIVATE (part);

	g_free (priv->query_string);

        if (G_OBJECT_CLASS (parent_class)->finalize)
                G_OBJECT_CLASS (parent_class)->finalize (obj);
}

static void
beagle_query_part_wildcard_class_init (BeagleQueryPartWildcardClass *klass)
{
        GObjectClass *obj_class = G_OBJECT_CLASS (klass);
        BeagleQueryPartClass *query_part_class = BEAGLE_QUERY_PART_CLASS (klass);
	
        parent_class = g_type_class_peek_parent (klass);
	
        obj_class->finalize = beagle_query_part_wildcard_finalize;
        query_part_class->to_xml = beagle_query_part_wildcard_to_xml;
	
        g_type_class_add_private (klass, sizeof (BeagleQueryPartWildcardPrivate));
}

static void
beagle_query_part_wildcard_init (BeagleQueryPartWildcard *part)
{
}

/**
 * beagle_query_part_wildcard_new:
 *
 * Creates a new #BeagleQueryPartWildcard.
 *
 * Return value: a newly created #BeagleQueryPartWildcard.
 */
BeagleQueryPartWildcard *
beagle_query_part_wildcard_new (void)
{
        BeagleQueryPartWildcard *part = g_object_new (BEAGLE_TYPE_QUERY_PART_WILDCARD, 0);
        return part;
}

/**
 * beagle_query_part_wildcard_set_query_string:
 * @part: a #BeagleQueryPartWildcard
 * @query_string: a #const char *
 *
 * Sets the wildcard string to search for in a #BeagleQueryPartWildcard.  The
 * text should contain an asterisk (*) as the wildcard character, for matching
 * zero or more items.
 *
 * While wildcards in the middle or end of a term are fine, using wildcards
 * at the beginning of a term is strongly discouraged, as it cannot be
 * efficiently done inside Lucene, the Beagle text indexer.  It will cause
 * a very large Lucene query to be built and will be very slow and could
 * possibly cause a TooManyClauses exception inside Lucene.  Try to avoid
 * them whenever possible.
 **/
void
beagle_query_part_wildcard_set_query_string (BeagleQueryPartWildcard *part,
					     const char              *query_string)
{
	BeagleQueryPartWildcardPrivate *priv;

	g_return_if_fail (BEAGLE_IS_QUERY_PART_WILDCARD (part));
	g_return_if_fail (query_string != NULL);
	
	priv = BEAGLE_QUERY_PART_WILDCARD_GET_PRIVATE (part);    
	g_free (priv->query_string);
	priv->query_string = g_strdup (query_string);
}
