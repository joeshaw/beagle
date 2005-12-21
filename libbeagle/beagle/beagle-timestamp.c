/*
 * beagle-timestamp.c
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

#include "beagle-timestamp.h"
#include "beagle-private.h"
#include <stdio.h>
#include <time.h>
#include <stdlib.h>

struct _BeagleTimestamp {
	int year, month, day;

	int hour, minute, second;

	int ticks;

	int tz_hour, tz_minute;
};

static BeagleTimestamp *
beagle_timestamp_new (void)
{
	BeagleTimestamp *timestamp;
	
	timestamp = g_new0 (BeagleTimestamp, 1);

	return timestamp;
}

/**
 * beagle_timestamp_new_from_string:
 * @str: a string
 *
 * Creates a newly allocated #BeagleTimestamp from the given string. The string should be of the following format, "2005-06-23T10:05:11.0000000+01:00".
 *
 * Return value: the newly allocated #BeagleTimestamp.
 **/
BeagleTimestamp *
beagle_timestamp_new_from_string (const char *str)
{
	BeagleTimestamp *timestamp;
	int consumed;

	timestamp = beagle_timestamp_new ();

	consumed = sscanf (str, "%04d-%02d-%02dT%02d:%02d:%02d.%07d%03d:%02d", 
			   &timestamp->year, &timestamp->month, &timestamp->day,
			   &timestamp->hour, &timestamp->minute, &timestamp->second,
			   &timestamp->ticks, &timestamp->tz_hour, &timestamp->tz_minute);
	
	if (consumed != 9) {
		beagle_timestamp_free (timestamp);
		return NULL;
	}

	
	return timestamp;
}

/**
 * beagle_timestamp_new_from_unix_time:
 * @time: a #time_t
 *
 * Creates a newly allocated #BeagleTimestamp from @time.
 *
 * Return value: the newly created #BeagleTimestamp.
 **/
BeagleTimestamp *
beagle_timestamp_new_from_unix_time (time_t time)
{
	BeagleTimestamp *timestamp;
	struct tm *result;

	result = gmtime (&time);

	timestamp = beagle_timestamp_new ();

	timestamp->year = result->tm_year + 1900;
	timestamp->month = result->tm_mon + 1;
	timestamp->day = result->tm_mday;

	timestamp->hour = result->tm_hour;
	timestamp->minute = result->tm_min;
	timestamp->second = result->tm_sec;

	return timestamp;
}

/**
 * beagle_timestamp_free:
 * @timestamp: a #BeagleTimestamp
 *
 * Frees the memory allocated by the given #BeagleTimestamp.
 **/
void 
beagle_timestamp_free (BeagleTimestamp *timestamp)
{
	g_free (timestamp);
}

/* I love you, UNIX */
static time_t
give_me_a_time_t_that_is_utc (struct tm *tm) {
	time_t ret;
	char *tz;
	
	tz = getenv("TZ");
	setenv("TZ", "", 1);
	tzset();
	ret = mktime(tm);
	if (tz)
		setenv("TZ", tz, 1);
	else
		unsetenv("TZ");
	tzset();
	return ret;
}

/**
 * beagle_timestamp_to_unix_time:
 * @timestamp: a #BeagleTimestamp
 * @time: a #time_t
 *
 * Converts the given #BeagleTimestamp to a unix #time_t.
 *
 * Return value: %TRUE on success and otherwise %FALSE.
 **/
gboolean
beagle_timestamp_to_unix_time (BeagleTimestamp *timestamp, time_t *time)
{
	time_t result, tz;
	struct tm tm_time;
	
	/* We special-case the timestamp "epoch" and use the unix epoch */
	if (timestamp->year == 0 && timestamp->month == 0 && timestamp->day == 0 &&
	    timestamp->hour == 0 && timestamp->minute == 0 && timestamp->second == 0 &&
	    timestamp->ticks == 0 && timestamp->tz_hour == 0 && timestamp->tz_minute == 0) {
		*time = 0;
		return TRUE;
	}

	if (timestamp->year < 1970 || timestamp->year > 2038) {
		return FALSE;
	}

	tm_time.tm_year = timestamp->year - 1900;
	tm_time.tm_mon = timestamp->month - 1;
	tm_time.tm_mday = timestamp->day;
	tm_time.tm_hour = timestamp->hour;
	tm_time.tm_min = timestamp->minute;
	tm_time.tm_sec = timestamp->second;
	tm_time.tm_isdst = -1;

	result = give_me_a_time_t_that_is_utc (&tm_time);

	if (result == -1)
		return FALSE;

	/* Add timezone */
	if (timestamp->tz_hour > 0) 
		tz = timestamp->tz_hour * 60 + timestamp->tz_minute;
	else
		tz = timestamp->tz_hour *60 - timestamp->tz_minute;

	tz *= 60;

        result += tz;
	
	/* Check for overflow */
	if (result < 0)
		return FALSE;

	*time = result;

	return TRUE;
}

char *
_beagle_timestamp_to_string (BeagleTimestamp *timestamp)
{
	return g_strdup_printf ("%04d-%02d-%02dT%02d:%02d:%02d.%07d%+03d:%02d",
				timestamp->year, timestamp->month, timestamp->day,
				timestamp->hour, timestamp->minute, timestamp->second,
				timestamp->ticks, timestamp->tz_hour, timestamp->tz_minute);
	
}

char *
_beagle_timestamp_get_start (void)
{
	return NULL;
}
