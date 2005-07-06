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
