import beagle
import gobject
import sys

total_hits = 0

def hits_added_cb (query, response):
	global total_hits
	hits = response.get_hits()
	
	total_hits = total_hits + len(hits)
	print "Found hits (%d):" % len(hits)
	print "-------------------------------------------"
	
	for hit in hits:
		if hit.get_type() == "FeedItem":
			text = hit.get_property("dc:title")
			print "Blog: %s" % text
		elif hit.get_type() == "File":
			print "File: %s" % hit.get_uri()
		else:
			print "%s (%s)" % (hit.get_uri(), hit.get_source_object_name())

	print "-------------------------------------------"

def finished_cb (query, response, loop):
	main_loop.quit()

client = beagle.Client()
main_loop = gobject.MainLoop()
query = beagle.Query()

for i in sys.argv[1:]:
	query.add_text(i)

query.connect("hits-added", hits_added_cb)
query.connect("finished", finished_cb, main_loop)
client.send_request_async(query)

main_loop.run()

print "Found a total of %d hits" % total_hits
