/*
 * beagle-query.h
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

#ifndef __BEAGLE_QUERY_H
#define __BEAGLE_QUERY_H

#include <glib-object.h>

#include <beagle/beagle-request.h>
#include <beagle/beagle-hits-added-response.h>
#include <beagle/beagle-hits-subtracted-response.h>
#include <beagle/beagle-finished-response.h>
#include <beagle/beagle-query-part.h>

#define BEAGLE_TYPE_QUERY            (beagle_query_get_type ())
#define BEAGLE_QUERY(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_QUERY, BeagleQuery))
#define BEAGLE_QUERY_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_QUERY, BeagleQueryClass))
#define BEAGLE_IS_QUERY(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_QUERY))
#define BEAGLE_IS_QUERY_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_QUERY))
#define BEAGLE_QUERY_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_QUERY, BeagleQueryClass))

typedef enum {
	BEAGLE_QUERY_DOMAIN_LOCAL        = 1,
	BEAGLE_QUERY_DOMAIN_SYSTEM       = 2,
	BEAGLE_QUERY_DOMAIN_NEIGHBORHOOD = 4,
	BEAGLE_QUERY_DOMAIN_GLOBAL       = 8
} BeagleQueryDomain;

typedef struct _BeagleQuery       BeagleQuery;
typedef struct _BeagleQueryClass  BeagleQueryClass;

struct _BeagleQuery {
	BeagleRequest parent;
};

struct _BeagleQueryClass {
	BeagleRequestClass parent_class;

	void (*hits_added)      (BeagleQuery *query, BeagleHitsAddedResponse *response);
	void (*hits_subtracted) (BeagleQuery *query, BeagleHitsSubtractedResponse *response);
	void (*finished)        (BeagleQuery *query, BeagleFinishedResponse *response);
};

GType        beagle_query_get_type     (void);
BeagleQuery *beagle_query_new          (void);
void         beagle_query_add_part     (BeagleQuery     *query, 
					BeagleQueryPart *part);
void         beagle_query_add_text     (BeagleQuery     *query,
					const char      *str);

void         beagle_query_set_domain    (BeagleQuery *query,
					 BeagleQueryDomain domain);
void         beagle_query_add_domain    (BeagleQuery *query,
					 BeagleQueryDomain domain);
void         beagle_query_remove_domain (BeagleQuery *query,
					 BeagleQueryDomain domain);

void	     beagle_query_set_max_hits (BeagleQuery *query, 
					int max_hits);
int          beagle_query_get_max_hits (BeagleQuery *query);

GSList      *beagle_query_get_exact_text   (BeagleQuery *query);
GSList      *beagle_query_get_stemmed_text (BeagleQuery *query);

#endif /* __BEAGLE_QUERY_H */

