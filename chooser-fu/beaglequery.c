/* -*- Mode: C; tab-width: 4; indent-tabs-mode: nil; c-basic-offset: 4 -*- */


/*
 * beaglequery.c
 *
 * Copyright (C) 2004 Novell, Inc.
 *
 */

/*
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307
 * USA.
 */

#ifdef CONFIG_H
#include <config.h>
#endif
#include "beaglequery.h"

#define _XOPEN_SOURCE /* glibc2 needs this */
#include <time.h>

void
beagle_hit_free (BeagleHit *hit)
{
    if (hit != NULL) {
        g_free (hit->uri);
        g_free (hit->type);
        g_free (hit->mime_type);
        g_free (hit->source);
        g_free (hit);
    }
}

/* ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** ** */

static BeagleQueryResult *
beagle_query_result_new ()
{
    BeagleQueryResult *bqr;

    bqr = g_new0 (BeagleQueryResult, 1);
    bqr->count = 0;
    bqr->all   = NULL;
    bqr->by_uri = g_hash_table_new (g_str_hash, g_str_equal);

    return bqr;
}

void
beagle_query_result_free (BeagleQueryResult *bqr)
{
    if (bqr != NULL) {
        g_hash_table_destroy (bqr->by_uri);
        g_slist_foreach (bqr->all, (GFunc) beagle_hit_free, NULL);
        g_slist_free (bqr->all);
    }
}

static void
beagle_query_result_add (BeagleQueryResult *bqr, BeagleHit *hit)
{
    g_return_if_fail (bqr != NULL);
    g_return_if_fail (hit != NULL);

    /* Throw out hits w/o URIs */
    if (hit->uri == NULL) {
        beagle_hit_free (hit);
        return;
    }

    ++bqr->count;
    bqr->all = g_slist_prepend (bqr->all, hit);
    g_hash_table_insert (bqr->by_uri, beagle_hit_get_uri (hit), hit);
}

BeagleHit *
beagle_query_result_get_by_uri (BeagleQueryResult *bqr, const char *uri)
{
    BeagleHit *hit;

    g_return_val_if_fail (bqr != NULL, NULL);
    g_return_val_if_fail (uri != NULL, NULL);

    hit = g_hash_table_lookup (bqr->by_uri, uri);

    return hit;
}

BeagleQueryResult *
beagle_query (const char *query_string)
{
    BeagleQueryResult *bqr = NULL;
    BeagleHit *current_hit = NULL;
    char *cmdline = NULL;
    char *query_output = NULL;
    char *query_ptr;

    g_return_val_if_fail (query_string != NULL, NULL);

    /* An ugly hack: we run beagle-query and parse the results into BeagleHit
       objects. */

    cmdline = g_strdup_printf ("/opt/beagle/bin/beagle-query --verbose %s", query_string);

    /* FIXME: We should fail gracefully and w/ a meaningful error message if
       we can't find beagle-query, etc. */
    if (! g_spawn_command_line_sync (cmdline, &query_output, NULL, NULL, NULL))
        goto finished;

    bqr = beagle_query_result_new ();

    query_ptr = query_output;
    while (query_ptr != NULL && *query_ptr) {
        char *next = strchr (query_ptr, '\n');
        if (next != NULL) {
            *next = '\0';
            ++next;
        }

        while (*query_ptr && isspace (*query_ptr))
            ++query_ptr;

        if (! strncmp (query_ptr, "Uri: ", 5)) {
            if (current_hit != NULL)
                beagle_query_result_add (bqr, current_hit);
            current_hit = g_new0 (BeagleHit, 1);
            current_hit->uri = g_strdup (query_ptr + 5);
        } else if (! strncmp (query_ptr, "Type: ", 6)) {
            current_hit->type = g_strdup (query_ptr + 6);
        } else if (! strncmp (query_ptr, "MimeT: ", 7)) {
            current_hit->mime_type = g_strdup (query_ptr + 7);
        } else if (! strncmp (query_ptr, "Src: ", 5)) {
            current_hit->source = g_strdup (query_ptr + 5);
        } else if (! strncmp (query_ptr, "Score: ", 7)) {
            current_hit->score = atof (query_ptr + 7);
        } else if (! strncmp (query_ptr, "Time: ", 6)) {
            struct tm tm;

	    /* strptime() does not init fields it does not touch ... */
	    memset (&tm, '0', sizeof (struct tm));
            if (strptime (query_ptr + 6, "%m/%d/%Y %I:%M:%S %p", &tm))
                current_hit->timestamp = mktime (&tm);
        }

        /* FIXME: We should also read in the properties */
        
        query_ptr = next;
    }

    if (current_hit != NULL)
        beagle_query_result_add (bqr, current_hit);

    g_print ("Query '%s' yieled %d hits\n", query_string, beagle_query_result_get_count (bqr));


 finished:
    g_free (cmdline);
    g_free (query_output);

    return bqr;
}
