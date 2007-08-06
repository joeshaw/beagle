//
// beagleQueue.js: Queue component implementation
//
// Copyright (C) 2007 Pierre Östlund
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

var queueAdd = Components.classes ['@mozilla.org/supports-array;1']
	.createInstance (Components.interfaces.nsISupportsArray);
var queueRemove = Components.classes ['@mozilla.org/supports-array;1']
	.createInstance (Components.interfaces.nsISupportsArray);
var observerService = Components.classes ['@mozilla.org/observer-service;1']
	.getService(Components.interfaces.nsIObserverService);

var totalAdded = 0;
var totalRemoved = 0;

function notify (data)
{
	var self = Components.classes ['@beagle-project.org/services/queue;1']
		.getService (Components.interfaces.nsIBeagleQueue);
	observerService.notifyObservers (self, "beagle-queue", data);
}

function init ()
{
	observerService.addObserver (gBeagleQueueObserver, 'quit-application', false);
}

// obj is either nsIMsgDBHdr or nsIMsgFolder
function add (obj)
{
	if (queueAdd.GetIndexOf (obj) != -1)
		return;
	
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	if (obj instanceof Components.interfaces.nsIMsgDBHdr)
		indexer.markHdrAsIndexed (obj);
	else if (obj instanceof Components.interfaces.nsIMsgFolder)
		indexer.markFolderAsIndexed (obj);
	else 
		return;
	
	queueAdd.AppendElement (obj);
	totalAdded++;
	notify ('add');
}

function remove (obj)
{
	if (queueRemove.GetIndexOf (obj) != -1)
		return;
	
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	if (obj instanceof Components.interfaces.nsIMsgDBHdr)
		indexer.resetHdr (obj, false);
	else if (obj instanceof Components.interfaces.nsIMsgFolder)
		indexer.resetFolder (obj, false, false, false);
	else 
		return;
	
	queueRemove.AppendElement (obj);
	totalRemoved++;
	notify ('remove');
}

// add, remove* and move* all return true if the object was added to the queue. If the object was
// rejected by a filter, then they will return false. A filter in this sense is if a mail is marked
// to be indexed or not (by the user).

// Add a new header for inclusion in the beagle index
function addHdr (hdr)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	// Check if we should index this
	if (!indexer.shouldIndexHdr (hdr) || !indexer.shouldIndexFolder (hdr.folder)) 
		return false;

	add (hdr);
	
	process ();
	
	return true;
}

function removeHdr (hdr)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	if (hdr instanceof Components.interfaces.nsIMsgDBHdr) {
		remove (hdr);

		process ();

		return true;
	}
	
	return false;
}

// Basic purpose of this function is to make the main loop run which eventually will pick it up
function addFolder (folder)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	if (indexer.isFolderIndexed (folder))
		return;
	
	notify ('add-folder');
}

function removeFolder (folder)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	if (folder instanceof Components.interfaces.nsIMsgFolder) {
		remove (folder);
		process ();

		return true;
	}
	
	return false;
}

function moveHdr (oldHdr, newHdr)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Component.interfaces.nsIBeagleIndexer);
	
	if (!indexer.shouldIndexHdr (oldHdr) || !indexer.shouldIndexHdr (newHdr))
		return false;
	
	remove (oldHdr);
	add (newHdr);
	processs ();
	
	return true;
}

function moveFolder (oldFolder, newFolder)
{
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Component.interfaces.nsIBeagleIndexer);
	
	if (!indexer.shouldIndexFolder (oldFolder) || !indexer.shouldIndexFolder (newFolder))
		return false;
		
	remove (oldFolder);
	add (newFolder);
	process ();
		
	return true;
}

// This process function will make sure that we have enough objects in the queue before processing
function process ()
{
	var settings = Components.classes ['@beagle-project.org/services/settings;1']
		.getService (Components.interfaces.nsIBeagleSettings);
	
	if (getQueueCount () < settings.getIntPref ('IndexQueueCount'))
		return;
	
	forceProcess ();
}

// No object count is done here, mainly so that the queue can be processed at any given time
function forceProcess ()
{
	var count = getQueueCount ();
	if (count == 0)
		return;
		
	var indexer = Components.classes ['@beagle-project.org/services/indexer;1']
		.getService (Components.interfaces.nsIBeagleIndexer);
	
	// Add new items to the beagle database
	for (var i = 0; i < queueAdd.Count (); i++) {
		var msg = queueAdd.GetElementAt (i).QueryInterface (Components.interfaces.nsIMsgDBHdr);
		if (!msg)
			continue;
		indexer.addToIndex (msg);
	}
	
	// Remove old items from the beagle database
	for (var i = 0; i < queueRemove.Count (); i++) {
		var obj = queueRemove.GetElementAt (i);
		
		if (obj instanceof Components.interfaces.nsIMsgDBHdr) {
			obj.QueryInterface (Components.interfaces.nsIMsgDBHdr);
			indexer.dropHdrFromIndex (obj);
		} else if (obj instanceof Components.interfaces.nsIMsgFolder) {
			obj.QueryInterface (Components.interfaces.nsIMsgFolder);
			indexer.dropFolderFromIndex (obj);
		}
	}
	
	queueAdd.Clear ();
	queueRemove.Clear ();
	
	dump ("Done processing " + count + " items\n");
}

function getQueueCount ()
{
	return queueAdd.Count () + queueRemove.Count ();
}

// This observer will check if the application is about to quit and process any remaining
// items in the queue when it does
var gBeagleQueueObserver = {

	observe: function (subject, topic, data)
	{
		// Just process whatever is left in the queue
		try {
			forceProcess ();
			observerService.removeObserver (this, 'quit-application');
		} catch (ex) {
		}
	}
};

