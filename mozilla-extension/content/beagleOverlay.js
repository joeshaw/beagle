/*
 * Beagle Extension: Index webpages you visit using the Beagle Indexing Engine.
 * An Extension for the Firefox (and Mozilla?) Browser.
 */

// Load jslib parts used in file execution
var gFile = new FileUtils();

// Initiate a new preference instance.
var gPref = Components.classes['@mozilla.org/preferences-service;1'].getService(Components.interfaces.nsIPrefBranch);

// Create the global variables
var gBeagleRunStatus = 0;
var gBeagleIndexerPath;
var gBeagleBestPath;

function beagleFindFileInPath(filename)
{
  var env = Components.classes["@mozilla.org/process/environment;1"].getService(Components.interfaces.nsIEnvironment);
  var path = env.get("PATH");
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
	dump ("beagleInit started!");

  gBeagleIndexerPath = beagleFindFileInPath("beagle-index-url");
  gBeagleBestPath = beagleFindFileInPath("best");

	dump ("beagleInit: Found beagle-index-url: " + gBeagleIndexerPath);
	dump ("beagleInit: Found best: " + gBeagleBestPath);

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

  // Add listener for page loads
  if (gBeagleIndexerPath) {
    if (document.getElementById("appcontent"))
      document.getElementById("appcontent").addEventListener("load", 
							     beaglePageLoad, 
							     true);
	    	dump ("beagleInit : Listening to document['appcontent'].load\n");
  } else {
    gBeagleRunStatus = "beagle-index-url not found in $PATH";
    beagleUpdateStatus ();
  }
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

function beagleShouldIndex(page)
{
  // user disabled, or can't find beagle-index-url.
  if (gBeagleRunStatus == -1 || !gBeagleIndexerPath)
    return false;

  if (!page || 
      !page.location || 
      page.location == 'about:blank' || 
      !page.location.href) {
    dump("beagleShouldIndex: strange page: " + page);
    return false;
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

	dump("beaglePageLoad : storing page: " + page.location.href);

  // FIXME: this should be in a safer directory and hash the url as well
  var hash = new Date().getMilliseconds ();
  var tmpfilepath = "/tmp/firefox-beagle-" + hash + ".html";

  try {
    beagleWriteContent(page, tmpfilepath);
    	dump ("beaglePageLoad: beagleWriteContent sucessful!");
    beagleRunIndexer(page.location.href, tmpfilepath);
    	dump ("beaglePageLoad: beagleRunIndexer sucessful!");
  } catch (ex) {
    alert ("beaglePageLoad: beagleWriteContent failed: " + ex);
  }
}

function beagleRunBest(url)
{
  try {
    var retval = gFile.spawn(gBeagleBestPath, ['--url', url]);
    if (retval) 
      alert("Error running best: " + retval);
  } catch(e) {
    alert("Caught error from best: " + e);
  }
}

function beagleRunIndexer(url, filepath)
{
  try {
    var retval = gFile.spawn(gBeagleIndexerPath, 
			    ["--url", url, 
			     "--sourcefile", filepath, 
			     "--deletesourcefile"]);
    if (retval) {
      alert("Error running beagle-index-url: " + retval);
      gBeagleRunStatus = retval;
    }
  } catch(e) {
    alert("Caught error from beagle-index-url: " + e);
    gBeagleRunStatus = e;
  }

  beagleUpdateStatus();
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
	break;
      case -1:
      case -2:
	// currently disabled (by user or by secure content). enable.
	gBeagleRunStatus = 0;
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

