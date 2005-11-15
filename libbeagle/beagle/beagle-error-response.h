/*
 * beagle-error-response.h
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

#ifndef __BEAGLE_ERROR_RESPONSE_H
#define __BEAGLE_ERROR_RESPONSE_H

#include <glib-object.h>
#include "beagle-response.h"

#define BEAGLE_TYPE_ERROR_RESPONSE            (beagle_error_response_get_type ())
#define BEAGLE_ERROR_RESPONSE(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), BEAGLE_TYPE_ERROR_RESPONSE, BeagleErrorResponse))
#define BEAGLE_ERROR_RESPONSE_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), BEAGLE_TYPE_ERROR_RESPONSE, BeagleErrorResponseClass))
#define BEAGLE_IS_ERROR_RESPONSE(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), BEAGLE_TYPE_ERROR_RESPONSE))
#define BEAGLE_IS_ERROR_RESPONSE_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), BEAGLE_TYPE_ERROR_RESPONSE))
#define BEAGLE_ERROR_RESPONSE_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), BEAGLE_TYPE_ERROR_RESPONSE, BeagleErrorResponseClass))

typedef struct _BeagleErrorResponse      BeagleErrorResponse;
typedef struct _BeagleErrorResponseClass BeagleErrorResponseClass;

struct _BeagleErrorResponse {
	BeagleResponse parent;
};

struct _BeagleErrorResponseClass {
	BeagleResponseClass parent_class;
};

GType beagle_error_response_get_type (void);

G_CONST_RETURN char *beagle_error_response_get_message (BeagleErrorResponse *response);

#endif /* __BEAGLE_ERROR_RESPONSE_H */

