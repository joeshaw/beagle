/*
 * beagle-parser.c
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

#include <libxml/parser.h>
#include <glib.h>
#include <string.h>

#include "beagle-private.h"
#include "beagle-parser.h"
#include "beagle-request.h"
#include "beagle-response.h"

#undef PARSER_DEBUG

/* I would kill a man for some reflection */

enum {
	/* Always present layers */
	BEAGLE_PARSER_STATE_TOPLEVEL,
	BEAGLE_PARSER_STATE_RESPONSE_WRAPPER,
	BEAGLE_PARSER_STATE_MESSAGE,
	BEAGLE_PARSER_LAST_STATE = BEAGLE_PARSER_STATE_MESSAGE
};

int _beagle_parser_state_index = BEAGLE_PARSER_LAST_STATE;

typedef enum {

	/* Specific message types */
	/* ErrorResponse */
	PARSER_STATE_ERROR_MESSAGE,

	/* HitsAddedResponse */
	PARSER_STATE_HITS_ADDED_HITS,
	PARSER_STATE_HITS_ADDED_HIT,
	PARSER_STATE_HITS_ADDED_PROPERTIES,
	PARSER_STATE_HITS_ADDED_PROPERTY,

	/* HitsSubtractedResponse */
	PARSER_STATE_HITS_SUBTRACTED_URIS,
	PARSER_STATE_HITS_SUBTRACTED_URI,

	/* DaemonInformationResponse */
} ParserState;

struct _BeagleParserContext {
	xmlParserCtxt *xml_context;

	ParserState state;

	char *text_buffer;
	int buffer_len;

	char *message_type;
	BeagleResponse *response;

#ifdef PARSER_DEBUG
	/* Used for debugging */
	GString *debug_str;
#endif
};

static void
start_message (BeagleParserContext *ctx, const char **attrs)
{
	int i;
	int num_gtypes;
	GType *gtypes;
	GType gtype_to_match = 0;

	for (i = 0; attrs [i] != NULL; i += 2) {
		if (strcmp (attrs [i], "xsi:type") == 0)
			ctx->message_type = g_strdup (attrs [i + 1]);
	}

	gtypes = g_type_children (BEAGLE_TYPE_REQUEST, &num_gtypes);

	for (i = 0; i < num_gtypes; i++) {
		BeagleRequestClass *klass = g_type_class_peek (gtypes [i]);

		if (klass == NULL)
			printf ("null class!\n");

		gtype_to_match = (GType) g_hash_table_lookup (klass->response_types,
							      ctx->message_type);

		if (gtype_to_match != 0)
			break;
	}

	g_free (gtypes);
	g_assert (gtype_to_match != 0);

	ctx->response = g_object_new (gtype_to_match, 0);
}


static BeagleParserHandler parser_handlers[] = {
	/* Always present layers */
	{ "ResponseWrapper",
	  BEAGLE_PARSER_STATE_TOPLEVEL,
	  BEAGLE_PARSER_STATE_RESPONSE_WRAPPER,
	  NULL,
	  NULL },

	{ "Message",
	  BEAGLE_PARSER_STATE_RESPONSE_WRAPPER,
	  BEAGLE_PARSER_STATE_MESSAGE,
	  start_message,
	  NULL },

	{ 0 }
};

static void
sax_start_document (void *data)
{
	BeagleParserContext *ctx = (BeagleParserContext *) data;

	ctx->state = BEAGLE_PARSER_STATE_TOPLEVEL;
}

static void
sax_end_document (void *data)
{
	BeagleParserContext *ctx = (BeagleParserContext *) data;

	if (ctx->state != BEAGLE_PARSER_STATE_TOPLEVEL)
		printf ("Invalid document!\n");
}

static BeagleParserHandler *
find_handler (BeagleParserContext *ctx, BeagleParserHandler *handlers, const xmlChar *name, gboolean src)
{
	int i;

	for (i = 0; handlers [i].name != NULL; i++) {
		BeagleParserHandler handler = handlers [i];
		int state;

		if (src)
			state = handler.src_state;
		else
			state = handler.dest_state;

		/* -1 here is last state before we get to the per
                    message elements */
		if (state == -1)
			state = BEAGLE_PARSER_LAST_STATE;

		if (state == ctx->state &&
		    strcmp (handler.name, name) == 0) {

			return &handlers [i];
		}
	}

	return NULL;
}

static BeagleParserHandler *
get_handler (BeagleParserContext *ctx, const xmlChar *name, gboolean src)
{
	BeagleResponseClass *response_class;
	BeagleParserHandler *handler;

	handler = find_handler (ctx, parser_handlers, name, src);

	if (handler)
		return handler;

	/* Now try the per-object handlers */
	if (ctx->response) {
		response_class = BEAGLE_RESPONSE_GET_CLASS (ctx->response);
		
		if (response_class->parser_handlers) {
			handler = find_handler (ctx, response_class->parser_handlers, name, src);
			
			if (handler)
				return handler;
		}
	}

	return NULL;
}

static void
sax_start_element (void *data, const xmlChar *name, const xmlChar **attrs)
{
	BeagleParserContext *ctx = (BeagleParserContext *) data;
	BeagleParserHandler *handler;

	
	handler = get_handler (ctx, name, TRUE);

	if (handler != NULL) {
		ctx->state = handler->dest_state;
		
		if (handler->start_element_func != NULL)
			handler->start_element_func (ctx, (const char **)attrs);
	} else {
		g_warning ("Unhandled element: %s!\n", name);
	}

	g_free (ctx->text_buffer);
	ctx->text_buffer = NULL;
}

static void
sax_end_element (void *data, const xmlChar *name)
{
	BeagleParserContext *ctx = (BeagleParserContext *) data;
	BeagleParserHandler *handler;

	handler = get_handler (ctx, name, FALSE);
	if (handler != NULL) {
		if (handler->end_element_func != NULL)
			handler->end_element_func (ctx);

		if (handler->src_state == -1)
			ctx->state = BEAGLE_PARSER_LAST_STATE;
		else
			ctx->state = handler->src_state;
	}

	g_free (ctx->text_buffer);
	ctx->text_buffer = NULL;
}

static void
sax_characters (void *data, const xmlChar *ch, int len)
{
	BeagleParserContext *ctx = (BeagleParserContext *) data;

	if (ctx->text_buffer != NULL) {
		char *buf = g_malloc0 (ctx->buffer_len + len + 1);
		strcpy (buf, ctx->text_buffer);
		strncpy (buf + ctx->buffer_len, ch, len);
		g_free (ctx->text_buffer);
		ctx->text_buffer = buf;
		ctx->buffer_len += len;
	} else {
		ctx->text_buffer = g_strndup (ch, len);
		ctx->buffer_len = len;
	}
}

static void
sax_warning (void *data, const char *msg, ...)
{
	va_list args;

	va_start (args, msg);

	printf ("warning: ");
	vprintf (msg, args);

	va_end (args);
}

static void
sax_error (void *data, const char *msg, ...)
{
	va_list args;
#ifdef PARSER_DEBUG
	BeagleParserContext *ctx = (BeagleParserContext *)data;
	
	g_print ("String is: %s\n", ctx->debug_str->str);
#endif
	va_start (args, msg);

	printf ("error: ");
	vprintf (msg, args);

	va_end (args);

	g_warning ("got parser error");
}

static xmlSAXHandler sax_handler = {
    NULL,                /* internalSubset */
    NULL,                /* isStandalone */
    NULL,                /* hasInternalSubset */
    NULL,                /* hasExternalSubset */
    NULL,                /* resolveEntity */
    NULL,                /* getEntity */
    NULL,                /* entityDecl */
    NULL,                /* notationDecl */
    NULL,                /* attributeDecl */
    NULL,                /* elementDecl */
    NULL,                /* unparsedEntityDecl */
    NULL,                /* setDocumentLocator */
    sax_start_document,  /* startDocument */
    sax_end_document,    /* endDocument */
    sax_start_element,   /* startElement */
    sax_end_element,     /* endElement */
    NULL,                /* reference */
    sax_characters,      /* characters */
    NULL,                /* ignorableWhitespace */
    NULL,                /* processingInstruction */
    NULL,                /* comment */
    sax_warning,         /* warning */
    sax_error,           /* error */
    sax_error,           /* fatalError */
};

BeagleParserContext *
_beagle_parser_context_new (void)
{
	BeagleParserContext *ctx = g_new0 (BeagleParserContext, 1);
	ctx->message_type = NULL;
	ctx->text_buffer = NULL;
	ctx->xml_context = NULL;

	xmlSubstituteEntitiesDefault (1);

	return ctx;
}

BeagleResponse *
_beagle_parser_context_get_response (BeagleParserContext *ctx)
{
	return ctx->response;
}

char *
_beagle_parser_context_get_text_buffer (BeagleParserContext *ctx)
{
	return g_strndup (ctx->text_buffer, ctx->buffer_len);
}


void
_beagle_parser_context_parse_chunk (BeagleParserContext *ctx, const char *buf, gsize bytes)
{
	if (ctx->xml_context == NULL) {
		ctx->xml_context = xmlCreatePushParserCtxt (&sax_handler, ctx,
							    NULL, 0, NULL);
#ifdef PARSER_DEBUG
		ctx->debug_str = g_string_new (NULL);
#endif
	}

#ifdef PARSER_DEBUG
	g_string_append_len (ctx->debug_str, buf, bytes);
#endif
	xmlParseChunk (ctx->xml_context, buf, bytes, 0);
}

BeagleResponse *
_beagle_parser_context_finished (BeagleParserContext *ctx)
{
	BeagleResponse *resp;

	if (ctx->xml_context != NULL) {
		xmlParseChunk (ctx->xml_context, NULL, 0, 1);
		xmlFreeParserCtxt (ctx->xml_context);
		ctx->xml_context = NULL;

#ifdef PARSER_DEBUG
	g_print ("Message: %s\n", ctx->debug_str->str);
	g_string_free (ctx->debug_str, TRUE);
#endif
	}

	resp = ctx->response;

	g_free (ctx->message_type);
	g_free (ctx->text_buffer);
	g_free (ctx);

	return resp;
}
