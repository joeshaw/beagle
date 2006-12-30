#include <beagle/beagle.h>

int
main (int argc, char **argv)
{
	BeagleClient  *client;
	BeagleRequest *request;

	g_type_init ();

	client = beagle_client_new (NULL);
	request = BEAGLE_REQUEST (beagle_shutdown_request_new ());

	beagle_client_send_request (client, request, NULL);

	g_object_unref (request);
	g_object_unref (client);

	return 0;
}
