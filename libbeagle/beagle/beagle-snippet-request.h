/*
 * beagle-snippet-request.h
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

#ifndef __BEAGLE_SNIPPET_REQUEST_H
#define __BEAGLE_SNIPPET_REQUEST_H

#include <glib-object.h>

#include <beagle/beagle-request.h>
#include <beagle/beagle-hit.h>
#include <beagle/beagle-query.h>

#define BEAGLE_TYPE_SNIPPET_REQUEST            (beagle_snippet_request_get_type ())
#define BEAGLE_SNIPPET_REQUEST(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequest))
#define BEAGLE_SNIPPET_REQUEST_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequestClass))
#define BEAGLE_IS_SNIPPET_REQUEST(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_SNIPPET_REQUEST))
#define BEAGLE_IS_SNIPPET_REQUEST_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_SNIPPET_REQUEST))
#define BEAGLE_SNIPPET_REQUEST_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_SNIPPET_REQUEST, BeagleSnippetRequestClass))

typedef struct _BeagleSnippetRequest      BeagleSnippetRequest;
typedef struct _BeagleSnippetRequestClass BeagleSnippetRequestClass;

struct _BeagleSnippetRequest {
	BeagleRequest parent;
};

struct _BeagleSnippetRequestClass {
	BeagleRequestClass parent_class;
};

GType        beagle_snippet_request_get_type     (void);
BeagleSnippetRequest *beagle_snippet_request_new          (void);

void beagle_snippet_request_set_hit (BeagleSnippetRequest *request,
				     BeagleHit *hit);

void beagle_snippet_request_set_query (BeagleSnippetRequest *request,
				       BeagleQuery          *query);

void beagle_snippet_request_set_full_text (BeagleSnippetRequest *request,
					   gboolean full_text);

void beagle_snippet_request_set_context_length (BeagleSnippetRequest *request,
						gint ctx_length);

void beagle_snippet_request_set_snippet_length (BeagleSnippetRequest *request,
						gint snp_length);

#endif /* __BEAGLE_SNIPPET_REQUEST_H */

