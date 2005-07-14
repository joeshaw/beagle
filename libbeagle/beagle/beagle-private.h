/*
 * beagle-private.h
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

#ifndef __BEAGLE_PRIVATE_H
#define __BEAGLE_PRIVATE_H

#include "beagle-hit.h"
#include "beagle-parser.h"
#include "beagle-query-part.h"
#include "beagle-indexable.h"
#include "beagle-request.h"
#include "beagle-error-response.h"
#include "beagle-timestamp.h"

struct _BeagleHit {
	int ref_count;

	int id;
	char *uri;
	char *parent_uri;
	BeagleTimestamp *timestamp;
	char *type;
	char *mime_type;
	char *source;
	char *source_object_name;

	double score_multiplier;
	double score_raw;

	long revision;

	GHashTable *properties;
};

struct _BeagleProperty {
	char *key;
	char *value;
	
	gboolean is_searched;
	gboolean is_keyword;
};

BeagleHit *_beagle_hit_new (void);

void _beagle_hit_set_property (BeagleHit *hit, const char *name, const char *value);

void _beagle_hit_list_free (GSList *list);

void _beagle_response_class_set_parser_handlers (BeagleResponseClass *klass,
						 BeagleParserHandler *handlers);


void _beagle_query_part_to_xml (BeagleQueryPart *part,
				GString         *data);
void _beagle_hit_add_property (BeagleHit *hit, BeagleProperty *prop);
void _beagle_hit_list_free    (GSList *list);

void _beagle_hit_to_xml (BeagleHit *hit, GString *data);

void _beagle_properties_to_xml (GHashTable *properties, GString *data);

void _beagle_indexable_to_xml (BeagleIndexable *indexable, GString *data);

BeagleResponse *_beagle_parser_context_get_response (BeagleParserContext *ctx);

BeagleResponse *_beagle_request_send (BeagleRequest *request,
				      const char *socket_path,
				      GError **err);

void _beagle_request_class_set_response_types (BeagleRequestClass *klass,
					       const char *beagle_type,
					       GType gobject_type,
					       ...);

gboolean _beagle_request_send_async (BeagleRequest  *request, 
				     const char     *socket_path, 
				     GError        **err);
void _beagle_request_append_standard_header (GString    *data, 
					     const char *xsi_type);
void _beagle_request_append_standard_footer (GString *data);

void _beagle_error_response_to_g_error (BeagleErrorResponse *response,
					GError **error);

char *_beagle_timestamp_to_string (BeagleTimestamp *timestamp);
char *_beagle_timestamp_get_start (void);

#endif /* __BEAGLE_PRIVATE_H */
