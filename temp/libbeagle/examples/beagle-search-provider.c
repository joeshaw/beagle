#include <beagle/beagle.h>
#include <stdlib.h>
#include <time.h>

static void
test_indexer (BeagleClient *client)
{
	BeagleIndexable *indexable;
	BeagleProperty *prop1, *prop2, *prop3;
	BeagleIndexingServiceRequest *request;
	BeagleTimestamp *timestamp;

	/* Use a unique URI for this indexable.
	 */
	indexable = beagle_indexable_new ("mybackend:///foobar1");

	/* We are adding an indexable.
	 * The other possibilities are
	 * Removing indexable - BEAGLE_INDEXABLE_TYPE_REMOVE
	 * Changing property of an indexable - BEAGLE_INDEXABLE_TYPE_PROPERTY_CHANGE
	 */
	beagle_indexable_set_type (indexable, BEAGLE_INDEXABLE_TYPE_ADD);

	/* No content means only properties will be indexed.
	 * No data is present for this indexable.
	 */
	beagle_indexable_set_no_content (indexable, TRUE);

	/* Set the source of this indexable.
	 * Use this to define the "backend" for this data.
	 */
	beagle_indexable_set_source (indexable, "MyBackend");

	/* Type of this indexable.
	 * One backend can produce indexables of various types.
	 * Different backends can generate indexables of same type
	 * e.g., different IM backends create indexables of type "Conversation"
	 */
	beagle_indexable_set_hit_type (indexable, "MyType");

	/* This indexable does not have any content, so do not try to filter its data.
	 */
	beagle_indexable_set_filtering (indexable, BEAGLE_INDEXABLE_FILTERING_NEVER);

	/* Create a custom property.
	 */
	prop1 = beagle_property_new (BEAGLE_PROPERTY_TYPE_TEXT, "dc:title", "foo bar");
	beagle_indexable_add_property (indexable, prop1);

	prop2 = beagle_property_new (BEAGLE_PROPERTY_TYPE_TEXT, "dc:rights", "GPL");
	beagle_indexable_add_property (indexable, prop2);

	prop3 = beagle_property_new (BEAGLE_PROPERTY_TYPE_KEYWORD, "fixme:foo", "1234-4567-8900");
	beagle_indexable_add_property (indexable, prop3);

	/* Set timestamp.
	 */
	timestamp = beagle_timestamp_new_from_unix_time (time (NULL));
	beagle_indexable_set_timestamp (indexable, timestamp);

	request = beagle_indexing_service_request_new_for_service ("IndexingServiceRequest");
	beagle_indexing_service_request_add (request, indexable);

	beagle_client_send_request (client, BEAGLE_REQUEST (request), NULL);
	g_print ("Data sent to beagle search service.\n");

	beagle_timestamp_free (timestamp);
	beagle_property_free (prop1);
	beagle_property_free (prop2);
	beagle_property_free (prop3);
}


int
main ()
{
	BeagleClient *client;

	g_type_init ();

	if (! beagle_util_daemon_is_running ()) {
		g_print ("beagle search service is not running.\n");
		return 1;
	}

	client = beagle_client_new (NULL);

	g_print ("Sending data to beagle search service\n");
	test_indexer (client);

	return 0;
}
	
	
