#include <stdlib.h>
#include <glib.h>
#include <string.h>
#include <beagle/beagle.h>

static int total_hits;

static void
print_feed_item_hit (BeagleHit *hit)
{
	const char *text;

	if (beagle_hit_get_one_property (hit, "dc:title", &text))
		g_print ("Blog: %s\n", text);
}

static void
print_file_hit (BeagleHit *hit)
{
	g_print ("File: %s\n", beagle_hit_get_uri (hit));
}

static void
print_other_hit (BeagleHit *hit)
{
	g_print ("%s (%s)", beagle_hit_get_uri (hit),
		 beagle_hit_get_source (hit));
}

static void
print_hit (BeagleHit *hit) 
{
	if (strcmp (beagle_hit_get_type (hit), "FeedItem") == 0) {
		print_feed_item_hit (hit);
	} 
	else if (strcmp (beagle_hit_get_type (hit), "File") == 0) {
		print_file_hit (hit);
	} else {
		print_other_hit (hit);
	}
}

static void
hits_added_cb (BeagleQuery *query, BeagleHitsAddedResponse *response, BeagleClient *client) 
{
	GSList *hits, *l;
	gint    i;
	gint    nr_hits;
	gint    total_matches;
	GError  *err;
	BeagleSnippetRequest *snippetrequest;
	BeagleResponse *snippetresponse;

	hits = beagle_hits_added_response_get_hits (response);
	total_matches = beagle_hits_added_response_get_num_matches (response);

	nr_hits = g_slist_length (hits);
	total_hits += nr_hits;
	g_print ("Found hits (%d) out of total %d matches:\n", nr_hits, total_matches);

	// This is necessary only once, not for each item
	snippetrequest = beagle_snippet_request_new();
	beagle_snippet_request_set_query(snippetrequest, query);

	g_print ("-------------------------------------------\n");
	for (l = hits, i = 1; l; l = l->next, ++i) {
		g_print ("[%d] ", i);

		print_hit (BEAGLE_HIT (l->data));

		g_print ("\n");

		beagle_snippet_request_set_hit(snippetrequest, BEAGLE_HIT (l->data));
		err = NULL;
		snippetresponse = beagle_client_send_request (client, BEAGLE_REQUEST (snippetrequest), &err);
		if (err) {
			g_error_free (err);
			g_print (" no snippet");
		}
		else if (snippetresponse) {
			g_print ("snippet: %s", beagle_snippet_response_get_snippet( BEAGLE_SNIPPET_RESPONSE(snippetresponse)) );
                        g_object_unref(snippetresponse);
		}

	}
	g_print ("-------------------------------------------\n\n\n");
}

static void
finished_cb (BeagleQuery            *query,
	     BeagleFinishedResponse *response, 
	     GMainLoop              *main_loop)
{
	g_main_loop_quit (main_loop);
}

static void
indexing_status_cb (BeagleInformationalMessagesRequest *request,
		    BeagleIndexingStatusResponse       *response,
		    gpointer                            user_data)
{
	g_print ("Daemon is indexing: %s\n", beagle_indexing_status_response_is_indexing (response) ? "YES" : "NO");
}

int
main (int argc, char **argv)
{
	BeagleClient *client;
	BeagleInformationalMessagesRequest *info_req;
	BeagleQuery *query;
	GMainLoop *main_loop;
	gint i;
	
	if (argc < 2) {
		g_print ("Usage %s \"query string\"\n", argv[0]);
		exit (1);
	}
	
	g_type_init ();

	total_hits = 0;

	client = beagle_client_new (NULL);

	if (client == NULL) {
		g_warning ("Unable to establish a connection to the beagle daemon");
		return 1;
	}

	main_loop = g_main_loop_new (NULL, FALSE);

	info_req = beagle_informational_messages_request_new ();
	g_signal_connect (info_req, "indexing-status",
			  G_CALLBACK (indexing_status_cb),
			  NULL);
	beagle_client_send_request_async (client, BEAGLE_REQUEST (info_req),
					  NULL);

	query = beagle_query_new ();

	for (i = 1; i < argc; ++i) {
		beagle_query_add_text (query, argv[i]);
	}

	g_signal_connect (query, "hits-added",
			  G_CALLBACK (hits_added_cb),
			  client);

	g_signal_connect (query, "finished",
			  G_CALLBACK (finished_cb),
			  main_loop);
	
	beagle_client_send_request_async (client, BEAGLE_REQUEST (query),
					  NULL);

	g_main_loop_run (main_loop);

	g_object_unref (info_req);
	g_object_unref (query);
	g_object_unref (client);
	g_main_loop_unref (main_loop);

	g_print ("Found a total of %d hits\n", total_hits);
	
	return 0;
}
