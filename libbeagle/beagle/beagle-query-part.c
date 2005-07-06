#include <string.h>

#include "beagle-private.h"
#include "beagle-query-part.h"

struct _BeagleQueryPart {
	char *target;
	char *text;
	gboolean is_keyword : 1;
	gboolean is_required : 1;
	gboolean is_prohibited : 1;
};

/**
 * beagle_query_part_new:
 *
 * Creates a new #BeagleQueryPart.
 * 
 * Return value: the newly created #BeagleQueryPart.
 **/
BeagleQueryPart *
beagle_query_part_new (void)
{
	BeagleQueryPart *part;

	part = g_new0 (BeagleQueryPart, 1);

	return part;
}

/**
 * beagle_query_part_free:
 * @part: a #BeagleQueryPart
 *
 * Frees the memory allocated by the given #BeagleQueryPart.
 **/
void
beagle_query_part_free (BeagleQueryPart *part)
{
	g_return_if_fail (part != NULL);

	g_free (part->target);
	g_free (part->text);
	g_free (part);
}

/**
 * beagle_query_part_set_target:
 * @part: a #BeagleQueryPart
 * @target: a string
 *
 * Sets the target of the given #BeagleQueryPart to @target.
 **/
void
beagle_query_part_set_target (BeagleQueryPart *part,
			      const char *target)
{
	g_return_if_fail (part != NULL);

	part->target = g_strdup (target);
}

/**
 * beagle_query_part_set_text:
 * @part: a #BeagleQueryPart
 * @text: a string
 *
 * Sets the text of the given #BeagleQueryPart to @text.
 **/
void
beagle_query_part_set_text (BeagleQueryPart *part,
			    const char *text)
{
	g_return_if_fail (part != NULL);

	part->text = g_strdup (text);
}

/**
 * beagle_query_part_set_keyword:
 * @part: a #BeagleQueryPart
 * @is_keyword: a boolean
 *
 * Sets whether the given #BeagleQueryPart is a keyword.
 **/
void
beagle_query_part_set_keyword (BeagleQueryPart *part,
			       gboolean is_keyword)
{
	g_return_if_fail (part != NULL);

	part->is_keyword = is_keyword;
}

/**
 * beagle_query_part_set_required:
 * @part: a #BeagleQueryPart
 * @is_required: a boolean
 *
 * Sets whether the given #BeagleQueryPart is required.
 **/
void
beagle_query_part_set_required (BeagleQueryPart *part,
				gboolean is_required)
{
	g_return_if_fail (part != NULL);

	part->is_required = is_required;
}

/**
 * beagle_query_part_set_prohibited:
 * @part: a #BeagleQueryPart
 * @is_prohibited: a boolean
 *
 * Sets whether the given #BeagleQueryPart is prohibited.
 **/
void
beagle_query_part_set_prohibited (BeagleQueryPart *part,
				  gboolean is_prohibited)
{
	g_return_if_fail (part != NULL);

	part->is_prohibited = is_prohibited;
}

void
_beagle_query_part_to_xml (BeagleQueryPart *part,
			   GString *data)
{
	g_return_if_fail (part != NULL);
	g_return_if_fail (data != NULL);

	g_string_append_len (data, "<Part>", 6);

	g_string_append_len (data, "<Target>", 8);
	g_string_append_len (data, part->target, strlen (part->target));
	g_string_append_len (data, "</Target>", 9);

	g_string_append_len (data, "<Text>", 6);
	g_string_append_len (data, part->text, strlen (part->text));
	g_string_append_len (data, "</Text>", 7);

	if (part->is_keyword)
		g_string_append_len (data, "<IsKeyword>true</IsKeyword>", 27);
	else
		g_string_append_len (data, "<IsKeyword>false</IsKeyword>", 28);
	
	if (part->is_required)
		g_string_append_len (data, "<IsRequired>true</IsRequired>", 29);
	else
		g_string_append_len (data, "<IsRequired>false</IsRequired>", 30);
	
	if (part->is_prohibited)
		g_string_append_len (data, "<IsProhibited>true</IsProhibited>", 33);
	else
		g_string_append_len (data, "<IsProhibited>false</IsProhibited>", 34);
	
	g_string_append_len (data, "</Part>", 7);
}
