/*
 * beaglefs/hit.c - Query hit processing engine
 *
 * Robert Love <rml@novell.com>
 *
 * Copyright (C) 2006 Robert Love
 *
 * Licensed under the terms of the GNU GPL v2
 */

#include <string.h>

#include <beagle/beagle.h>
#include <glib.h>

#include "hit.h"
#include "inode.h"
#include "dir.h"

static char *query_text;
static GMainLoop *hit_main_loop;

/*
 * beagle_hit_set_query - Set the query used by the filesystem to 'query'.
 * A copy of 'query' is made and must be freed via g_free().
 */
void
beagle_hit_set_query (const char *query)
{
	g_return_if_fail (query);
	query_text = g_strdup (query);
}

/*
 * hit_to_new_inode - create a new beaglefs inode object via beagle_inode_new()
 * and initialize its default values via a BeagleHit object.
 *
 * Returns the newly allocated inode object, which must be freed via a call to
 * beagle_inode_free().
 */
static beagle_inode_t *
hit_to_new_inode (BeagleHit *hit)
{
	BeagleTimestamp *hit_time;
	beagle_inode_t *inode;
	time_t timestamp;

	g_return_val_if_fail (hit, NULL);

	hit_time = beagle_hit_get_timestamp (hit);
	if (!beagle_timestamp_to_unix_time (hit_time, &timestamp))
		time (&timestamp); /* current time is better than nothing */

	inode = beagle_inode_new (beagle_hit_get_uri (hit),
				  timestamp,
				  beagle_hit_get_mime_type (hit),
				  beagle_hit_get_type (hit),
				  beagle_hit_get_source (hit),
				  beagle_hit_get_score (hit));

	return inode;
}

/*
 * hits_added_cb - Our callback for the libbeagle "hits-added" signal.  For
 * each hit, create a new inode object and add it to the filesystem.
 */
static void
hits_added_cb (G_GNUC_UNUSED BeagleQuery *query,
	       BeagleHitsAddedResponse *response) 
{
	GSList *hits, *elt;

	hits = beagle_hits_added_response_get_hits (response);
	for (elt = hits; elt; elt = g_slist_next (elt)) {
		beagle_inode_t *inode;

		inode = hit_to_new_inode (BEAGLE_HIT (elt->data));

		beagle_dir_write_lock ();
		beagle_dir_add_inode (inode);
		beagle_dir_write_unlock();
	}
}

/*
 * hits_subtracted_cb - Our callback for the libbeagle "hits-subtracted"
 * signal.  For each hit, remove the corresponding inode from the filesystem.
 */
static void
hits_subtracted_cb (G_GNUC_UNUSED BeagleQuery *query,
		    BeagleHitsSubtractedResponse *response) 
{
	GSList *hits, *elt;

	hits = beagle_hits_subtracted_response_get_uris (response);
	for (elt = hits; elt; elt = g_slist_next (elt)) {
		const char *name;

		name = strrchr (elt->data, G_DIR_SEPARATOR);

		beagle_dir_write_lock ();
		beagle_dir_remove_inode_by_name (name);
		beagle_dir_write_unlock ();
	}
}

static void *
hit_thread_start (G_GNUC_UNUSED void *ignored)
{
	BeagleClient *client;
	BeagleQuery *query;
	BeagleRequest *request;

	client = beagle_client_new (NULL);
	if (!client)
		g_critical ("Failed to instantiate a BeagleClient.");

	hit_main_loop = g_main_loop_new (NULL, FALSE);
	query = beagle_query_new ();
	request = BEAGLE_REQUEST (query);

	beagle_query_add_text (query, query_text);
	beagle_query_add_hit_type (query, "File");
	beagle_query_add_hit_type (query, "IMLog");

	g_signal_connect (query,
			  "hits-added",
			  G_CALLBACK (hits_added_cb),
			  client);

	g_signal_connect (query,
			  "hits-subtracted",
			  G_CALLBACK (hits_subtracted_cb),
			  client);

	if (!beagle_client_send_request_async (client, request, NULL))
		g_critical ("Failed to send BeagleQuery to Beagle.");

	g_main_loop_run (hit_main_loop);

	g_object_unref (query);
	g_object_unref (client);
	g_main_loop_unref (hit_main_loop);
	g_free (query_text);

	return NULL;
}

/*
 * beagle_hit_init - Initialize the hit engine.
 */
void
beagle_hit_init (void)
{
	g_thread_init (NULL);
	g_type_init ();

	if (!g_thread_create (hit_thread_start, NULL, FALSE, NULL))
		g_critical ("Failed to launch hit engine thread.");
}

/*
 * beagle_hit_destroy - Force a return from the main loop, destroying the hit
 * engine thread.
 */
void
beagle_hit_destroy (void)
{
	g_main_loop_quit (hit_main_loop);
}
