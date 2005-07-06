/*
 * beagle-parser.h
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

#ifndef __BEAGLE_PARSER_H
#define __BEAGLE_PARSER_H

#include <glib.h>

#include "beagle-response.h"

typedef struct _BeagleParserContext BeagleParserContext;

typedef void (*BeagleParserStartElementFunction) (BeagleParserContext *ctx, const char **attrs);
typedef void (*BeagleParserEndElementFunction) (BeagleParserContext *ctx);


typedef struct {
	const char *name;
	int src_state;
	int dest_state;
	BeagleParserStartElementFunction start_element_func;
	BeagleParserEndElementFunction end_element_func;
} BeagleParserHandler;


BeagleParserContext *_beagle_parser_context_new (void);

BeagleResponse *_beagle_parser_context_get_request (BeagleParserContext *ctx);
char *_beagle_parser_context_get_text_buffer (BeagleParserContext *ctx);

void _beagle_parser_context_parse_chunk (BeagleParserContext *ctx,
					 const char *buf,
					 gsize byteS);

BeagleResponse *_beagle_parser_context_finished (BeagleParserContext *ctx);

extern int _beagle_parser_state_index;

#endif /* __BEAGLE_PARSER_H */
