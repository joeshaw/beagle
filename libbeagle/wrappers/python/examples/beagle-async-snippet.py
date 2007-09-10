#!/usr/bin/env python

import beagle
import gobject
import sys

total_hits = 0

class TestCase:

	def __init__(self):
		self.beagle = beagle.Client()

	def beagle_query(self, qstring):
		print "Send query (%s)" % qstring
		self.hits = {}
		self.finished = False

		self.beagle_query = beagle.Query()
		query_part_human = beagle.QueryPartHuman()
		query_part_human.set_string(qstring)
		self.beagle_query.add_part(query_part_human)

		self.beagle_query.connect("hits-added", self.hits_added_cb, qstring)
		self.beagle_query.connect("finished", self.finished_cb)
		self.beagle.send_request_async(self.beagle_query)

	def hits_added_cb (self, query, response, qstring):
		hits = response.get_hits()
		num_matches = response.get_num_matches()
	
		print "Returned hits (%d) out of total %d matches:" % (len(hits), num_matches)
		print "-------------------------------------------"
	
		for hit in hits:
			print hit.get_uri()

			snippet_request = beagle.SnippetRequest()
			snippet_request.set_query(query)
			snippet_request.set_hit(hit)
			hit.ref()
			snippet_request.connect('response', self._on_snippet_received, hit)
			snippet_request.connect('closed', self._on_snippet_closed, hit)
			self.beagle.send_request_async(snippet_request)
			self.hits [hit] = snippet_request

		print "-------------------------------------------"
   
	def _on_snippet_received(self, request, response, hit):
		print "Snippet received for %s" % hit.get_uri()
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
