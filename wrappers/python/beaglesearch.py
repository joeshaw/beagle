#!/usr/bin/env python

import beagle
import gobject
import sys

total_hits = 0

def hits_added_cb (query, response):
	global total_hits
	hits = response.get_hits()
	num_matches = response.get_num_matches()
	
	total_hits = total_hits + len(hits)
	print "Returned hits (%d) out of total %d matches:" % (len(hits), num_matches)
	print "-------------------------------------------"
	
	for hit in hits:
		if hit.get_type() == "FeedItem":
			text = hit ['dc:title']
			print "Blog: %s" % text
		elif hit.get_type() == "File":
			print "File: %s" % hit.get_uri()
		elif hit.get_type() == "MailMessage":
			print "Email: %s" % hit ['dc:title']
			for sender in hit.get_properties ('fixme:from'):
				print "\tSent by: %s" % sender
		else:
			print "%s (%s)" % (hit.get_uri(), hit.get_source())

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
