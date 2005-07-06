#include <beagle/beagle.h>

#if 0
static void
test_daemon_information (BeagleClient *client)
{
	BeagleDaemonInformationRequest *request;
	BeagleResponse *response;

	request = beagle_daemon_information_request_new ();
	response = beagle_client_send_request (client, BEAGLE_REQUEST (request), NULL);

	g_object_unref (request);
	g_object_unref (response);

	g_print ("Beagle version: %s\n", beagle_daemon_information_response_get_version (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)));

	g_print ("%s\n", beagle_daemon_information_response_get_human_readable_status (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)));

}

static void
test_shutdown (BeagleClient *client)
{
	BeagleShutdownRequest *request;
	BeagleResponse *response;

	request = beagle_shutdown_request_new ();
	response = beagle_client_send_request (client, BEAGLE_REQUEST (request), NULL);

}
#endif

static void
hits_added_cb (BeagleQuery *query, BeagleHitsAddedResponse *response, BeagleClient *client)
{
	GSList *hits;
	BeagleSnippetRequest *snippet;
	
	hits = beagle_hits_added_response_get_hits (response);
	snippet = beagle_snippet_request_new ();
	beagle_snippet_request_add_query_term (snippet, "gnome");

	while (hits != NULL) {
		BeagleHit *hit = hits->data;
		BeagleResponse *resp;
		GError *err = NULL;

		g_print ("added %s\n", beagle_hit_get_uri (hit));

		beagle_snippet_request_set_hit (snippet, hit);

		resp = beagle_client_send_request (client, BEAGLE_REQUEST (snippet), &err);
		if (err) {
			g_print ("got error: %s\n",
				 err->message);
			g_error_free (err);
		}

		if (resp) {
			g_print ("getting a snippet: %s\n",
				 beagle_snippet_response_get_snippet (BEAGLE_SNIPPET_RESPONSE (resp)));
			g_object_unref (resp);
		}

		hits = hits->next;
	}

	g_object_unref (snippet);
}


static void
hits_subtracted_cb (BeagleQuery *query, BeagleHitsSubtractedResponse *response)
{
	GSList *uris;
	
	uris = beagle_hits_subtracted_response_get_uris (response);
	while (uris != NULL) {
		char *uri = uris->data;

		g_print ("removed: %s\n", uri);

		uris = uris->next;
	}
}

static void
finished_cb (BeagleQuery *query, BeagleFinishedResponse *response, GMainLoop *loop)
{
	g_print ("finished!\n");
	g_main_loop_quit (loop);
}

static void
test_live_query (BeagleClient *client)
{
	BeagleQuery *query;
	GMainLoop *loop;

	loop = g_main_loop_new (NULL, FALSE);

	query = beagle_query_new ();
	g_signal_connect (query, "hits-added",
			  G_CALLBACK (hits_added_cb), client);
	g_signal_connect (query, "hits-subtracted",
			  G_CALLBACK (hits_subtracted_cb), NULL);
	g_signal_connect (query, "finished",
			  G_CALLBACK (finished_cb), loop);

	beagle_query_add_text (query, "gnome");

	beagle_client_send_request_async (client, BEAGLE_REQUEST (query), NULL);

	g_main_loop_run (loop);
	g_print ("back from main loop!\n");
	g_object_unref (query);
}

#if 0
static void
test_indexer (BeagleClient *client)
{
	BeagleIndexable *indexable;
	BeagleProperty *prop;
	BeagleIndexingServiceRequest *request;

	indexable = beagle_indexable_new ("uid:richard");

	prop = beagle_property_new ("sliff", "sloff");
	beagle_indexable_add_property (indexable, prop);

	request = beagle_indexing_service_request_new ();
	beagle_indexing_service_request_add (request, indexable);
	beagle_client_send_request (client, BEAGLE_REQUEST (request), NULL);
}
#endif


int
main ()
{
	BeagleClient *client;

	g_type_init ();

	client = beagle_client_new (NULL);
#if 0
	g_print ("testing indexer\n");
	test_indexer (client);

	g_print ("testing daemon information\n");
	test_daemon_information (client);
#endif
	g_print ("testing live query\n");
	test_live_query (client);
	
	g_print ("good bye!\n");
	g_object_unref (client);

	/*	test_shutdown (client);*/

	return 0;
}
	

	
