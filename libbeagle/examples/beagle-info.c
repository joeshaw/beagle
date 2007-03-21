#include <beagle/beagle.h>

static void
test_daemon_information (BeagleClient *client)
{
	BeagleDaemonInformationRequest *request;
	BeagleResponse *response;
	gboolean get_version = TRUE, get_info = TRUE, get_status = TRUE, is_indexing = TRUE;

	request = beagle_daemon_information_request_new_specific (get_version,
								  get_info,
								  get_status,
								  is_indexing
								);
	response = beagle_client_send_request (client, BEAGLE_REQUEST (request), NULL);

	g_object_unref (request);

	if (get_version)
		g_print ("Beagle version: %s\n", beagle_daemon_information_response_get_version (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)));
	if (get_info)
		g_print ("%s\n", beagle_daemon_information_response_get_human_readable_status (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)));
	if (get_status)
		g_print ("%s\n", beagle_daemon_information_response_get_index_information (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)));
	if (is_indexing)
		g_print ("%s\n", (beagle_daemon_information_response_is_indexing (BEAGLE_DAEMON_INFORMATION_RESPONSE (response)) ? "Indexing" : "Not Indexing"));

	g_object_unref (response);
}

int
main ()
{
	BeagleClient *client;

	g_type_init ();

	client = beagle_client_new (NULL);
	test_daemon_information (client);

	return 0;
}
	
