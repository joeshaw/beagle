/* -*- Mode: javascript; indent-tabs-mode: nil; c-basic-offset: 2 -*- */
/*
 * Beagle Extension: Index webpages you visit using the Beagle Indexing Engine.
 * An Extension for the Firefox (and Mozilla?) Browser.
 */

// Initiate a new preference instance.
var gPref = Components.classes['@mozilla.org/preferences-service;1'].getService(Components.interfaces.nsIPrefBranch);
var gEnv = Components.classes["@mozilla.org/process/environment;1"].getService(Components.interfaces.nsIEnvironment);

// Load jslib parts used in file execution
var gFile = new FileUtils();

// Create the global variables
var gBeagleRunStatus = 0;
var gBeagleDataPath = gEnv.get("HOME") + "/.beagle/ToIndex";
var gBeagleDataDir = new Dir(gBeagleDataPath);
var gBeagleBestPath;

function beagleFindFileInPath(filename)
{
  var path = gEnv.get("PATH");
  if (path) {
    var split = path.split(':');
    var idx = 0;
    while (idx < split.length) {
      var trypath = split[idx++] + '/' + filename;
      if (gFile.exists(trypath))
        return trypath;
    }
  }
  return undefined;
}

function beagleInit()
{
  dump ("beagleInit started!\n");

  gBeagleBestPath = beagleFindFileInPath("beagle-search");

  dump ("beagleInit: Found beagle-search: " + gBeagleBestPath + "\n");

  // Create eventlistener to trigger when context menu is invoked.
  if (gBeagleBestPath) {
    try {
      document.getElementById('contentAreaContextMenu').addEventListener('popupshowing',
								         beagleContext,
								         false);
    } catch(e) {
      alert(e);
    }
  }

  // Get the global enable/disable pref
  try { bPref = gPref.getBoolPref('beagle.enabled'); }
  catch(e) { bPref = true }

  if (bPref)
    gBeagleRunStatus = 0;
  else
    gBeagleRunStatus = -1;

  // Add listener for page loads
  if (document.getElementById("appcontent"))
    document.getElementById("appcontent").addEventListener("load", 
                                                           beaglePageLoad, 
                                                           true);
  dump ("beagleInit : Listening to document['appcontent'].load\n");

  beagleUpdateStatus ();
}

//
// Copied from nsIWebBrowserPersist.idl
//

// Only use cached data (could fail)
var PERSIST_FLAGS_FROM_CACHE = 1;
// Replace existing files on the disk
var PERSIST_FLAGS_REPLACE_EXISTING_FILES = 32;
// Don't modify or add base tags
var PERSIST_FLAGS_NO_BASE_TAG_MODIFICATIONS = 64;
// Don't make any adjustments to links
var PERSIST_FLAGS_DONT_FIXUP_LINKS = 512;
// Don't make any adjustments to filenames
var PERSIST_FLAGS_DONT_CHANGE_FILENAMES = 2048;
// Cleanup on failure
var PERSIST_FLAGS_CLEANUP_ON_FAILURE = 8192;

var PERSIST_MASK = (PERSIST_FLAGS_FROM_CACHE | 
		    PERSIST_FLAGS_REPLACE_EXISTING_FILES |
		    PERSIST_FLAGS_NO_BASE_TAG_MODIFICATIONS |
		    PERSIST_FLAGS_DONT_FIXUP_LINKS |
		    PERSIST_FLAGS_DONT_CHANGE_FILENAMES |
		    PERSIST_FLAGS_CLEANUP_ON_FAILURE);

// Write raw source
var ENCODE_FLAGS_RAW = 4;
// Convert links to absolute links where possible.
var ENCODE_FLAGS_ABSOLUTE_LINKS = 128;

var ENCODE_MASK = (ENCODE_FLAGS_RAW | ENCODE_FLAGS_ABSOLUTE_LINKS);

function beagleWriteContent(page, tmpfilepath)
{
  var tmpfile = Components.classes["@mozilla.org/file/local;1"].createInstance(Components.interfaces.nsILocalFile);
  tmpfile.initWithPath(tmpfilepath);

  var persist = Components.classes["@mozilla.org/embedding/browser/nsWebBrowserPersist;1"].createInstance(Components.interfaces.nsIWebBrowserPersist);
  persist.persistFlags = PERSIST_MASK;

  persist.saveDocument(page, tmpfile, null, null, ENCODE_MASK, 0);
}

function beagleWriteMetadata(page, tmpfilepath)
{
  var tmpfile = Components.classes["@mozilla.org/file/local;1"].createInstance(Components.interfaces.nsILocalFile);
  tmpfile.initWithPath(tmpfilepath);

  var stream = Components.classes["@mozilla.org/network/file-output-stream;1"].createInstance(Components.interfaces.nsIFileOutputStream);
  stream.QueryInterface(Components.interfaces.nsIOutputStream);
  stream.init(tmpfile, 0x04 | 0x08 | 0x20, 0600, 0);

  var line;

  // First line: URI
  line = page.location.href + "\n";
  stream.write(line, line.length);
  
  // Second line: Hit Type
  line = "WebHistory\n";
  stream.write(line, line.length);

  // Third line: Mime type
  line = "text/html\n";
  stream.write(line, line.length);

  // Additional lines: Properties
  line = "k:_unindexed:encoding=" + page.characterSet + "\n";
  stream.write(line, line.length);

  stream.flush();
  stream.close();
}

function beagleShouldIndex(page)
{
  // user disabled, or can't find beagle-index-url.
  if (gBeagleRunStatus == -1)
    return false;

  if (!page || 
      !page.location || 
      page.location == 'about:blank' || 
      !page.location.href) {
    dump("beagleShouldIndex: strange page: " + page + "\n");
    return false;
  }

  try {
    fPref = gPref.getCharPref('beagle.security.filters');
    var filtered = fPref.split(';');
    for (j = 0; j < filtered.length; j++){
      if (page.location.host.search("/"+filtered[j]+"/i") != -1){
        dump("beagleShouldIndex: filtered host: " + page.location.host + '\n');
        gBeagleRunStatus = -2;
        beagleUpdateStatus ();
        return false;
      }
    }
  } catch (e) {
    // Do nothing..
  }

  if (page.location.protocol == "https:") {
	var bPref;

	// secure content, check if user wants it indexed
	try { bPref = gPref.getBoolPref('beagle.security.active'); }
	catch(e) { bPref = false }

	if (!bPref) {
	  // don't index. disable and return.
	  gBeagleRunStatus = -2;
	  beagleUpdateStatus ();
	  return false;
	}
  } else if (gBeagleRunStatus == -2) {
	// no longer secure content, re-enable
	gBeagleRunStatus = 0;
	beagleUpdateStatus ();
  }
  
  return true;
}

function beaglePageLoad(event)
{
  var page = event.originalTarget;

  if (!beagleShouldIndex (page))
    return;

  if (!gFile.exists (gEnv.get("HOME") + "/.beagle")) {
    dump("beaglePageLoad: ~/.beagle doesn't exist, not indexing");
    return;
  }

  dump("beaglePageLoad: storing page: " + page.location.href + "\n");

  if (!gFile.exists(gBeagleDataPath)) {
    try {
      gBeagleDataDir.create ();
      dump ("beaglePageLoad: Created .beagle/firefox\n");
    } catch(e) {
      dump ("beaglePageLoad: Unable to create .beagle/firefox: " + e + "\n");
    }
  }

  var hash = hex_md5(page.location.href);
  var tmpdatapath = gBeagleDataPath + "/firefox-beagle-" + hash + ".html";
  var tmpmetapath = gBeagleDataPath + "/.firefox-beagle-" + hash + ".html";

  try {
    beagleWriteContent(page, tmpdatapath);
    dump ("beaglePageLoad: beagleWriteContent sucessful!\n");
    beagleWriteMetadata(page, tmpmetapath);
    dump ("beaglePageLoad: beagleWriteMetadata sucessful!\n");
  } catch (ex) {
    alert ("beaglePageLoad: beagleWriteContent/Metadata failed: " + ex);
  }
}

function beagleRunBest(query)
{
  try {
    dump("Running best with query: "+ query + "\n");
    var retval = gFile.spawn(gBeagleBestPath, ["", query]);
    if (retval) 
      alert("Error running best: " + retval);
  } catch(e) {
    alert("Caught error from best: " + e);
  }
}

function beagleShowPrefs()
{
  window.openDialog('chrome://beagle/content/beaglePrefs.xul',
		    'PrefWindow',
		    'chrome,modal=yes,resizable=no',
		    'browser');
}

function beagleProcessClick(event)
{
  // Right-click event.
  if (event.button == 2) {
    beagleShowPrefs();
    return;
  }

  // Left-click event (also single click, like Mac).
  if (event.button == 0) {
    if (event.ctrlKey) {
      // Ctrl-click for Mac properties.  Works on PC too.
      beagleShowPrefs();
    } else {
      switch(gBeagleRunStatus) {
      case 0:
	// currently enabled. disable by user.
	gBeagleRunStatus = -1;
        gPref.setBoolPref('beagle.enabled', false);
	break;
      case -1:
      case -2:
	// currently disabled (by user or by secure content). enable.
	gBeagleRunStatus = 0;
        gPref.setBoolPref('beagle.enabled', true);
	break;
      default:
	// last run was an error, show the error
	alert("Error running Beagle Indexer: " + gBeagleRunStatus);
        return;
      }

      beagleUpdateStatus();
    }
  }
}

function beagleUpdateStatus()
{
  var icon = document.getElementById('beagle-notifier-status');

  switch(gBeagleRunStatus) {
    case 0: // active
      icon.setAttribute("status","000");
      icon.setAttribute("tooltiptext","Beagle indexing active. Click to disable.");
      break;
    case -1: // disabled by user
    case -2: // disabled for secure protocol
      icon.setAttribute("status","00f");
      icon.setAttribute("tooltiptext","Beagle indexing disabled.  Click to enable.");
      break;
    default: // anything else is an error
      icon.setAttribute("status","f00");
      icon.setAttribute("tooltiptext",
			"Error while indexing: " + gBeagleRunStatus);
      break;
  }
}

// Create event listener.
window.addEventListener('load', beagleInit, false); 

// Right-click context menu
function beagleContext()
{
  var bPref;

  // Find context menu display preference.
  try      { bPref = gPref.getBoolPref('beagle.context.active'); }
  catch(e) { }

  // Set hidden property of context menu and separators.
  document.getElementById('beagle-context-menu').hidden = !(bPref);
  document.getElementById('beagle-context-sep-a').hidden = !(bPref);
  document.getElementById('beagle-context-sep-b').hidden = !(bPref);

  // If not displaying context menu, return.
  if (!bPref) return;

  // Separator A (top) display preference.
  try      { bPref = gPref.getBoolPref('beagle.context.sep.a'); }
  catch(e) { bPref = false }
  document.getElementById('beagle-context-sep-a').hidden = !(bPref);

  // Separator B (bottom) display preference.
  try      { bPref = gPref.getBoolPref('beagle.context.sep.b'); }
  catch(e) { bPref = false }
  document.getElementById('beagle-context-sep-b').hidden = !(bPref);

  // Should search link item be hidden or shown?
  document.getElementById('beagle-context-search-link').hidden = !(gContextMenu.onLink);

  // Should text search item be hidden or shown?
  document.getElementById('beagle-context-search-text').hidden = !(gContextMenu.isTextSelected);
  document.getElementById('beagle-context-search-text').setAttribute("label","Search for \"" + gContextMenu.searchSelected() + "\"");
}

