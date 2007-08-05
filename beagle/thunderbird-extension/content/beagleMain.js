//
// beagleMain.js: This file will make sure data is indexed by running a main loop and catching
//                various changes (new messages, new folders, etc.)
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

var gAccountManager = Components.classes ['@mozilla.org/messenger/account-manager;1']
	.getService (Components.interfaces.nsIMsgAccountManager);
var gBeagleSettings = Components.classes ['@beagle-project.org/services/settings;1']
	.getService (Components.interfaces.nsIBeagleSettings);
var gBeagleIndexer = Components.classes ['@beagle-project.org/services/indexer;1']
	.getService (Components.interfaces.nsIBeagleIndexer);
var gBeagleQueue = Components.classes ['@beagle-project.org/services/queue;1']
	.getService (Components.interfaces.nsIBeagleQueue);

var gBeagleDataTracker = {

	Notifications: Components.classes ['@mozilla.org/messenger/msgnotificationservice;1']
		.getService(Components.interfaces.nsIMsgFolderNotificationService),

	RegisterSelf: function ()
	{
		dump ('Registering beagle data tracker...');
		this.Notifications.removeListener (this);
		this.Notifications.addListener (this);
		dump ("Done.\n");
	},
	
	UnregisterSelf: function ()
	{
		dump ('Unregistering beagle data tracker...');
		this.Notifications.removeListener (this);
		dump ("Done.\n");
	},
	
	//
	//	Below are the functios that will get all updates
	//
	
	// Message added
	itemAdded: function (item)
	{
		if (!item)
			return;
		
		dump ("Adding new messages and restarting loop\n");
		gBeagleMainloop.Restart (20);
		try {
			var hdr = item.QueryInterface (Components.interfaces.nsIMsgDBHdr);
			if (!hdr)
				return;
			
			gBeagleQueue.addHdr (hdr);
		} catch (ex) {
		}
	},
	
	// Message _or_ folder removed
	itemDeleted: function (item)
	{
		dump ("Removing message(s) or folder and restarting loop\n");
		gBeagleMainloop.Restart (20);
		if (item instanceof Components.interfaces.nsIMsgDBHdr) {
			gBeagleQueue.removeHdr (item)
		} else if (item instanceof Components.interfaces.nsIMsgFolder) {
			gBeagleQueue.removeFolder (item)
		}
	},
	
	itemMoveCopyCompleted: function (move, items, dest)
	{
		dump ("Moving/copying message(s)/folder and restarting loop\n");
		gBeagleMainloop.Restart (20);
		
		// There can be at most one folder in "items", so we check for that
		var folder = items.GetElementAt (0);
		if (folder instanceof Components.interfaces.nsIMsgFolder) {
			
			// We have a folder. This is an ugly solution...
			var finished = false;
			var enumerator = dest.GetSubFolders ();
			while (!finished) {
				var f = enumerator.currentItem ().QueryInterface (Components.interfaces.nsIMsgFolder);
				if (f.name == folder.name) {
					// We do this recursively to ensure all sub-content are re-indexed too
					gBeagleIndexer.resetFolder (f, false, true, true);
					break;
				}
			
				try { enumerator.next (); }
				catch (ex) { finished = true; }
			}
			
			// If we moved the folder, then we also have to remove the source folder. Otherwise
			// we'll end up with messages and folders that doesn't exist
			if (move)
				gBeagleQueue.removeFolder (folder);
		} else {
			// We have a bunch of messages. Reset all messages.
			for (var i = 0; i < items.Count (); i++) {
				var message = items.QueryElementAt (i, Components.interfaces.nsIMsgDBHdr);
				gBeagleIndexer.resetHdr (message, false);
			}
			
			// Also make sure we reset the destination folder, otherwise the new messages
			// won't be caught by the main loop
			gBeagleIndexer.resetFolder (dest, false, false, true);
		}
	},
	
	folderRenamed: function (oldFolder, newFolder)
	{
		dump ("Renaming folder and restarting loop\n");
		gBeagleMainloop.Restart (20);
		gBeagleQueue.moveFolder (oldFolder, newFolder);
	},
	
	itemEvent: function (item, event, data)
	{
	}
};

var gBeagleDataCollector = {

	GetNextFolder: function ()
	{
		var accounts = gAccountManager.accounts;
		
		for (var i = 0; i < accounts.Count (); i++) {
			var account = accounts.QueryElementAt (i, Components.interfaces.nsIMsgAccount);
			
			// This check the overall type
			if (!gBeagleIndexer.shouldIndexAccount (account)) 
				continue;
			
			var allFolders = Components.classes ['@mozilla.org/supports-array;1']
				.createInstance (Components.interfaces.nsISupportsArray);
			account.incomingServer.rootFolder.ListDescendents (allFolders);
			
			for (var j = 0; j < allFolders.Count (); j++) {
				var folder = allFolders.QueryElementAt (j, Components.interfaces.nsIMsgFolder);
				
				// We don't bother if there's nothing to index
				if (folder.getTotalMessages (false) == 0)
					continue;

				// We only need to index a folder if it isn't already indexed and if the user
				// hasn't explicitly excluded it
				if (!gBeagleIndexer.isFolderIndexed (folder) && gBeagleIndexer.shouldIndexFolder (folder))
					return folder;
			}
		}
		
		return null;
	},

	// Add new mails to the indexing queue
	Process: function ()
	{
		// If we don't have a folder available at this time, get next available
		if (!this.CurrentFolder)
			this.CurrentFolder = this.GetNextFolder ();
			
		// Note that we don't have any folders left to index if GetNextFolder returned null
		if (!this.CurrentFolder) {
			gBeagleQueue.forceProcess ();
			return;
		}
		
		dump ('Processing messages in ' + this.CurrentFolder.prettyName + "\n");
		
		// We have a valid folder to enumerate over, make sure we have a valid enumerator as well
		if (this.CurrentEnumerator == null)
			this.CurrentEnumerator = this.CurrentFolder.getMessages (null);
		
		// Process items. We skip already indexed items.
		var batchCounter = gBeagleSettings.getIntPref ('IndexBatchCount');
		while (batchCounter > 0 && this.CurrentEnumerator.hasMoreElements ()) {
			var hdr = this.CurrentEnumerator.getNext ().QueryInterface (Components.interfaces.nsIMsgDBHdr);
			if (!hdr || gBeagleIndexer.isHdrIndexed (hdr))
				continue;
			
			// We only count down the counter in case we actually did add the message (since a
			// filter could have picked this up)		
			if (gBeagleQueue.addHdr (hdr)) 
				batchCounter--;
		}
		
		// We might have missed items in case the database content changed, so we set the enumerator
		// to null to make sure we enumerate the same database again. We keep doing this until
		// batchCounter does not change, this way we'll know that everything in this mailbox has
		// been indexed and that we can move on.
		if (!this.CurrentEnumerator.hasMoreElements ()) {
			// We are done and mark this folder to reflect this. Doing so will make it a lot faster
			// finding not already indexed folders and we don't have to keep track of this somewhere
			// else. It's also stored across sessions.
			gBeagleIndexer.markFolderAsIndexed (this.CurrentFolder);
			this.CurrentFolder.getMsgDatabase (null).Commit (1);
			dump ('Finished indexing ' + this.CurrentFolder.prettyName + "\n");
			this.CurrentFolder = null;
		}
		this.CurrentEnumerator = null;
	},
	
	CurrentFolder: null,
	CurrentEnumerator: null
};

var gBeagleMainloop = {

	Start: function ()
	{
		this.Timer.cancel ();
		this.Timer.initWithCallback (this,
									gBeagleSettings.getIntPref ('IndexDelay') * 1000,
									Components.interfaces.nsITimer.TYPE_REPEATING_SLACK);
		this.IsRunning = true;
	},
	
	Stop: function ()
	{
		this.Timer.cancel ();
		this.IsRunning = false;
		
		try {
			gBeagleSettings.setBoolPref ('Enabled', false);
		} catch (ex) {
			dump ("Failed to disable beagle extension!\n");
		}
	},
	
	Restart: function (seconds)
	{
		this.Timer.cancel ();
		this.Timer.initWithCallback (this, 
			seconds * 1000,
			Components.interfaces.nsITimer.TYPE_REPEATING_SLACK);
		this.IsRunning = true;
	},
	
	notify: function (timer)
	{
		// Make sure we have a destination directory. If it doesn't exists, create it and mark 
		// everything as not indexed.
		try {
			// Check if destination directory exists
			var dir = Components.classes ["@mozilla.org/file/local;1"]
				.createInstance (Components.interfaces.nsILocalFile);
			dir.initWithPath (gBeagleSettings.getCharPref ('DestinationDirectory'));
			if (dir.exists ()) {
				if (!dir.isDirectory ()) {
					dump ("Destination directory exists but is not a directory! Bailing out!\n");
					gBeagleMainloop.Stop ();
					return;
				}
				
				// We need to create the ToIndex directory in case it doesn't exist
				dir.initWithPath (gBeagleSettings.getCharPref ('DestinationDirectory') + '/ToIndex');
				if (!dir.exists ()) {
					// We create this directory and mark all content as not indexed
					dir.create (Components.interfaces.nsIFile.DIRECTORY_TYPE, 0755);
					gBeagleUnindex.UnindexEverything (false);
				} else if (dir.isFile ()) {
					dump ("The ToIndex directory exists but is not a directory!\n");
					this.Stop ();
					return;
				} else if (!dir.isWritable ()) {
					dump ("The ToIndex directory exists but is not writable!\n");
					this.Stop ();
					return;
				}
			} else {
				dump ("Destination directory does not exist!\n");
				this.Stop ();
				return;
			}
		} catch (ex) {
			dump ("Failed to create destination directory! Bailing out! (" + ex + ")\n");
			this.Stop ();
			return;
		}

		// Index next set
		//try {
			gBeagleDataCollector.Process ();
		/*} catch (ex) {
			dump ("Error while indexing: " + ex + "\n");
		}*/
		//gBeagleMainloop.Start ();
		this.Timer.delay = gBeagleSettings.getIntPref ('IndexDelay') * 1000;
	},

	IsRunning: false,
	Timer: Components.classes ["@mozilla.org/timer;1"].createInstance (Components.interfaces.nsITimer)
};

