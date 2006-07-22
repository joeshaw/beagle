#!/usr/bin/env python

import beagle
import gobject
import sys

def process_inputs (choice, val):
    if choice [val] == "y":
	choice [val] = True
    else:
	choice [val] = False

request = beagle.DaemonInformationRequest()

get_version = raw_input ("Get version (y/n)? ")
get_scheduler_status = raw_input ("Get scheduler status (y/n)? ")
get_index_info = raw_input ("Get index information (y/n)? ")
get_is_indexing = raw_input ("Find out if currently indexing (y/n)? ")

choice = {
    'get_version': get_version,
    'get_scheduler_status': get_scheduler_status,
    'get_index_info': get_index_info,
    'get_is_indexing': get_is_indexing}

process_inputs (choice, 'get_version')
process_inputs (choice, 'get_scheduler_status')
process_inputs (choice, 'get_index_info')
process_inputs (choice, 'get_is_indexing')

client = beagle.Client ()

request = beagle.DaemonInformationRequest (choice ['get_version'],
					   choice ['get_scheduler_status'],
					   choice ['get_index_info'],
					   choice ['get_is_indexing'])
response = client.send_request (request)
del request
request = None

if choice ['get_version']:
    print "Version = %s" % response.get_version ()
if choice ['get_scheduler_status']:
    print "Scheduler status:\n %s" % response.get_human_readable_status ()
if choice ['get_index_info']:
    print "Index information:\n %s" % response.get_index_information ()
if choice ['get_is_indexing']:
    print "Is indexing ? %s" % response.is_indexing ()

