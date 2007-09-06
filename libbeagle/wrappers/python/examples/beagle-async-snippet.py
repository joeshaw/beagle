#!/usr/bin/env python

import beagle
import gobject
import sys

total_hits = 0

class TestCase:

	def __init__(self):
		self.beagle = beagle.Client()

	def beagle_query(self, qstring):
		print "Send query"
		self.beagle_query = beagle.Query()
		self.beagle_query.add_text(qstring)
		self.beagle_query.connect("hits-added", self.hits_added_cb, qstring)
		self.beagle_query.connect("finished", self.finished_cb)
		self.beagle.send_request_async(self.beagle_query)

	def hits_added_cb (self, query, response, qstring):
		hits = response.get_hits()
		num_matches = response.get_num_matches()
		self.hits = {}
		self.finished = False
	
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

			snippet_request = beagle.SnippetRequest()
			snippet_request.set_query(query)
			snippet_request.set_hit(hit)
			hit.ref()
			snippet_request.connect('response', self._on_snippet_received)
			snippet_request.connect('closed', self._on_snippet_closed, hit)
			self.beagle.send_request_async(snippet_request)
			self.hits [hit] = snippet_request

		print "-------------------------------------------"
   
	def _on_snippet_received(self, request, response):
		print "Snippet received"
		snippet = response.get_snippet()
		print snippet
    
	def _on_snippet_closed(self, request, hit):
		del self.hits [hit]
		hit.unref()
		if (self.finished and len (self.hits) == 0):
			main_loop.quit()
            
	def finished_cb (self, query, response):
		# Checking for self.finished should be done within a mutex
		self.finished = True
		if (len (self.hits) == 0):
			main_loop.quit()

if __name__ == '__main__':
	global main_loop
	main_loop = gobject.MainLoop()
	case = TestCase()
	case.beagle_query(sys.argv[1])
	main_loop.run()
