/*** -*- Mode: Javascript; tab-width: 2;

The contents of this file are subject to the Mozilla Public
License Version 1.1 (the "License"); you may not use this file
except in compliance with the License. You may obtain a copy of
the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS
IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
implied. See the License for the specific language governing
rights and limitations under the License.

The Original Code is jslib team code.
The Initial Developer of the Original Code is jslib team.

Portions created by jslib team are
Copyright (C) 2003 jslib team.  All
Rights Reserved.

Original Author: Pete Collins <pete@mozdev.org>
Contributor(s):  Neil Deakin <neil@mozdev.org>

***/

// STATUS: WORK IN PROGRESS

// Make sure jslib.js is loaded
if (typeof(JS_LIB_LOADED) == 'boolean')
{

  // Ensure we are in a DOM window environment
  if (typeof(window) == 'undefined') {
    jslibDebug("Error: This library can only be used in a dom window environment");
  } else {
    // Load dependency lib files
    include(jslib_file);
    include(jslib_dirutils);
    include(jslib_dir);
    include(jslib_fileutils);
    include(jslib_window);

  // GLOBALS
  const JS_LIB_UNINSTALL_LOADED   = true;
  const JS_LIB_UNINSTALL_FILE     = 'uninstall.js';

  // CONSTRUCTOR
  function Uninstall(aPackageName, installCallback) 
  {
    if (!aPackageName) {
      throw - jslibRes.NS_ERROR_XPC_NOT_ENOUGH_ARGS;
    } else {
      if (typeof(aPackageName) == "string"){
        this.mNames = [aPackageName];
      }
      else {
        this.mNames = aPackageName;
      }
    }

    this.mInstallCallback = installCallback;

    this.gRDF = jslibGetService("@mozilla.org/rdf/rdf-service;1", "nsIRDFService");
    this.gDirService = jslibGetService("@mozilla.org/file/directory_service;1", "nsIProperties");
  }

  /*********** UNINSTALL ***************/
  Uninstall.prototype =
  {
    // set ACTIVE_MODE to false for debugging, and changes will only be simulated.
    ACTIVE_MODE             : true,

    gRDF                    : null,
    gDirService             : null,

    CHRM_REG_CID            : "@mozilla.org/chrome/chrome-registry;1",
    CHROME_NS               : "http://www.mozilla.org/rdf/chrome#",

    mNames                  : null,
    mLastURL                : "about:blank",
    mLastChromeURL          : null,
    mUninstallInfoGenerated : false,
    mInstallCallback        : null,

    filesToDelete           : [],
    filesToDeleteHash       : {},
    overlaysToDelete        : [],
    baseURIs                : {},
    packageDisplayName      : "",

  closeOpenWindows : function () 
  {
    if (!this.ACTIVE_MODE) return;

    jslibDebug(opener.location);
    // jslibDebug(opener._content.location);
    this.mLastChomeURL = opener.location.toString();
    try {
      this.mLastURL = opener._content.location.toString();
    } catch (e) {}
    var win = new CommonWindow();
    var openWindows = win.openWindows;
    for (var i=0; i<openWindows.length; i++)  {
      if (/chrome:\/\/launchy\/content\/jslib\/install\/uninstall.xul/.test(openWindows[i].location)) {
        jslibPrint("Skipping: "+openWindows[i].location);
        continue;
      }
      openWindows[i].close();
    }

    return;
  },

  launchLastWindow : function () 
  {
    if (!this.mLastChomeURL) return;
    window.openDialog("chrome://communicator/content/pref/pref.xul", "PrefWindow", "chrome,titlebar,resizeable=yes", "chrome://communicator/content/pref/pref-navigator.xul", "navigator", "navigator");
    //window.openDialog(this.mLastChomeURL, "_blank", "chrome,all,dialog=no", this.mLastURL);
    
  },

  finish : function () 
  {
    this.launchLastWindow();
  },

  generateUninstallInfo : function () 
  {
    if (!this.mUninstallInfoGenerated){
      this.mUninstallInfoGenerated = true;

      this.filesToDelete = [];
      this.filesToDeleteHash = {};
      this.overlaysToDelete = [];
      this.baseURIs = {};

      this.doUninstall(false);
    }
  },

  uninstallPackage : function () 
  {
    this.generateUninstallInfo();
    this.doUninstall(true);
  },

/**
 * Iterates over the items in an RDF Container
 */
iterateContainer : function(ds, resource, callback)
{
  try {
    var container = jslibCreateInstance("@mozilla.org/rdf/container;1", "nsIRDFContainer");
    container.Init(ds, resource);
  }
  catch (ex){ return; }

  var elements = container.GetElements();
  while (elements.hasMoreElements()){
    var element = elements.getNext();
    callback(resource, element, this);
  }
},

/**
 * Get all of the currently installed packages. This function is not currently used.
 */
getAllPackagesInfo : function(chromeDS)
{
  var allPackages = {};

  var handlePackages = function(container, packres, uninstallObj)
  {
    var childPred = uninstallObj.gRDF.GetResource(uninstallObj.CHROME_NS + "name")
    var childName = chromeDS.GetTarget(packres, childPred, true);

    var displayPred = uninstallObj.gRDF.GetResource(uninstallObj.CHROME_NS + "displayName")
    var displayName = chromeDS.GetTarget(packres, displayPred, true);

    if (childName instanceof jslibI.nsIRDFLiteral){
      if (displayName instanceof jslibI.nsIRDFLiteral){
        displayName = displayName.Value;
      }
      else {
        displayName = childName.Value;
      }
      allPackages[childName.Value] = displayName;
    }
  }

  var rootseq = this.gRDF.GetResource("urn:mozilla:package:root");
  this.iterateContainer(chromeDS, rootseq, handlePackages);
},

/**
 * Do the uninstallation. This function will be called twice. Once to generate
 * the list of files and overlays to delete, and the second to do the deletions.
 */
doUninstall : function(makeChanges)
{
  var ioService = jslibGetService("@mozilla.org/network/io-service;1", "nsIIOService");

  // scan through chrome.rdf and find all references to the package and remove them.
  var appChromeDir = this.gDirService.get("AChrom", jslibI.nsIFile);
  var chromeRdfFile = appChromeDir.clone();
  chromeRdfFile.append("chrome.rdf");
  var chromeUrl = ioService.newFileURI(chromeRdfFile).spec;
  var appChromeDS = this.gRDF.GetDataSourceBlocking(chromeUrl);

  for (var pt = 0; pt < this.mNames.length; pt++){
    this.handleChromeRDF(this.mNames[pt], appChromeDir, appChromeDS, makeChanges);
  }

  // scan through chrome.rdf and find all references to the package and remove them.
  var userChromeDir = this.gDirService.get("UChrm", jslibI.nsIFile);
  chromeRdfFile = userChromeDir.clone();
  chromeRdfFile.append("chrome.rdf");
  chromeUrl = ioService.newFileURI(chromeRdfFile).spec;
  var userChromeDS = this.gRDF.GetDataSourceBlocking(chromeUrl);

  for (var pt = 0; pt < this.mNames.length; pt++){
    this.handleChromeRDF(this.mNames[pt], userChromeDir, userChromeDS, makeChanges);
  }

  if (makeChanges){
    if (this.ACTIVE_MODE){
      if (appChromeDS instanceof jslibI.nsIRDFRemoteDataSource)
          appChromeDS.Flush();
      if (userChromeDS instanceof jslibI.nsIRDFRemoteDataSource)
          userChromeDS.Flush();
    }

    if (this.mInstallCallback) this.mInstallCallback(this.filesToDelete,true);

    for (t=0; t<this.overlaysToDelete.length; t++){
      this.removeOverlay(this.overlaysToDelete[t]);
    }

    this.removeFromInstalledChrome(appChromeDir);

    var uninstallObj = this;
    var callback = function() {  uninstallObj.doNextUninstallStep(uninstallObj,0); }
    setTimeout(callback,50);
  }
  else {
    if (this.mInstallCallback) this.mInstallCallback(this.filesToDelete,false);
  }
},

doNextUninstallStep : function(uninstallObj,step)
{
  jslibPrint("doNextUninstallStep");
  var pmeter = document.getElementById("uninstallProgress");

  if (step >= uninstallObj.filesToDelete.length){
    pmeter.value = 100;
    document.getElementById("progressText").value = "Uninstall Complete";

    var wizard = document.getElementById("uninstallWizard");
    wizard.canAdvance = true;

    return;
  }

  var adj = Math.round(100 / uninstallObj.filesToDelete.length);
  if (adj < 5){
    if (step % Math.round(uninstallObj.filesToDelete.length / 20)) adj = 5;
    else adj = 0;
  }
  pmeter.value = parseInt(pmeter.value) + adj;

  // ignore errors since it doesn't matter if a file could not be found, and
  // non-empty directories should not be deleted.
  try {
    var file = uninstallObj.filesToDelete[step];
    var path = file.path;

    document.getElementById("progressText").value = "Uninstalling " + file.leafName;

    var ext = path.substring(path.lastIndexOf(".")+1, path.length);
    // close the jar filehandle so we can unlock it and delete it on 
    // OS's like Windows that like to lock their open files
    if (ext == "jar") {
      var IOService = jslibGetService("@mozilla.org/network/io-service;1", "nsIIOService");
      var handler = IOService.getProtocolHandler("jar");
      if (handler instanceof jslibI.nsIJARProtocolHandler) {
        var zrc = handler.JARCache;
        var nsIZipReader = zrc.getZip(file);
        nsIZipReader.close();
      }
    }
    jslibDebug("Uninstall ---- Delete " + file.path + "\n");
    if (this.ACTIVE_MODE && file.exists()) file.remove(false);
  }
  catch (ex){ jslibDebug(ex); }

  var callback = function() {  uninstallObj.doNextUninstallStep(uninstallObj,step + 1); }
  setTimeout(callback,50);
},

/**
 * Gather information about the package from a chrome.rdf file and remove it.
 */
handleChromeRDF :function(packagename, chromeDir, chromeDS, makeChanges)
{
  // remove package from content
  var rootseq = this.gRDF.GetResource("urn:mozilla:package:root");
  var packres = this.gRDF.GetResource("urn:mozilla:package:" + packagename);

  if (makeChanges){
    this.removeFromChrome(chromeDS, rootseq, packres);
  }
  else {
    this.generateUninstallData(chromeDS, rootseq, packres, chromeDir);

    if (!this.packageDisplayName){
      var displayNamePred = this.gRDF.GetResource(this.CHROME_NS + "displayName")
      var displayName = chromeDS.GetTarget(packres, displayNamePred, true);
      if (displayName instanceof Components.interfaces.nsIRDFLiteral){
        this.packageDisplayName = displayName.Value;
      }
      else {
        this.packageDisplayName = packagename;
      }
    }
  }

  // remove package from skin
  var provider = "skin";

  var handleSkinLocaleList = function(container, skinLocale, uninstallObj)
  {
    var rootseq = chromeDS.GetTarget(skinLocale,
                    uninstallObj.gRDF.GetResource(uninstallObj.CHROME_NS + "packages"),true);
    rootseq.QueryInterface(jslibI.nsIRDFResource);

    var skinLocaleName = chromeDS.GetTarget(skinLocale,
          uninstallObj.gRDF.GetResource(uninstallObj.CHROME_NS + "name"),true);

    if (skinLocaleName instanceof jslibI.nsIRDFLiteral){
      var skinLocaleRes = uninstallObj.gRDF.GetResource("urn:mozilla:" + provider + ":" +
                            skinLocaleName.Value + ":" + packagename);

      if (makeChanges) uninstallObj.removeFromChrome(chromeDS, rootseq, skinLocaleRes);
      else uninstallObj.generateUninstallData(chromeDS, rootseq, skinLocaleRes, chromeDir);
    }
  };

  var packreslist = this.gRDF.GetResource("urn:mozilla:skin:root");
  this.iterateContainer(chromeDS, packreslist, handleSkinLocaleList);

  // remove package from locale
  provider = "locale";

  packreslist = this.gRDF.GetResource("urn:mozilla:locale:root");
  this.iterateContainer(chromeDS, packreslist, handleSkinLocaleList);
},

/**
 * Perform an uninstallation given a contents.rdf datasource.
 *   aChromeDS   - chrome.rdf datasource
 *   rootseq     - root sequence
 *   packres     - packagename as a resource
 */
generateUninstallData : function(chromeDS, rootseq, packres, chromeDir)
{
  var baseUrlPred = this.gRDF.GetResource(this.CHROME_NS + "baseURL")
  var baseUrl = chromeDS.GetTarget(packres, baseUrlPred, true);
  if (baseUrl instanceof jslibI.nsIRDFLiteral){
    var ds;
    try {
      ds = this.gRDF.GetDataSourceBlocking(baseUrl.Value + "contents.rdf");
    }
    catch (ex){ jslibDebug(ex); return; }

    this.markJarForDeletion(baseUrl.Value);

    this.generateFilesToDelete(ds, packres);
    this.generateOverlaysToDelete(ds, chromeDir, "overlays");
    this.generateOverlaysToDelete(ds, chromeDir, "stylesheets");
  }
},

/**
 * Generate the files to delete, which are listed in the uninstallInfo section
 * of the contents.rdf
 */
generateFilesToDelete : function(aDS, node)
{
  var pred = this.gRDF.GetResource(this.CHROME_NS + "uninstallInfo");
  var uninstallInfo = aDS.GetTarget(node,pred,true);
  if (uninstallInfo){
    this.iterateContainer(aDS, uninstallInfo, this.makeFileForDeletion);
  }
},

/**
 * Mark a file for deletion.
 */
makeFileForDeletion : function(container, filename, uninstallObj)
{
  if (!(filename instanceof jslibI.nsIRDFLiteral)) return;
  filename = filename.Value;

  var filekey;
  var colonIdx = filename.indexOf(":");
  if (colonIdx >= 0){
    filekey = filename.substring(0,colonIdx);
    filename = filename.substring(colonIdx + 1);
  }
  else {
    filekey = "CurProcD";
  }

  var file;
  try {
     file = uninstallObj.gDirService.get(filekey, jslibI.nsIFile);
  } catch (ex) { return; }

  var fileparts = filename.split("/");
  for (var t=0; t<fileparts.length; t++){
    file.append(fileparts[t]);
  }

  if (!uninstallObj.filesToDeleteHash[file.path]){
    uninstallObj.filesToDeleteHash[file.path] = file;
    uninstallObj.filesToDelete.push(file);
  }
},

/**
 * Given a baseURI reference, determine the JAR file to delete.
 */
markJarForDeletion : function(url)
{
  this.baseURIs[url] = url;

  if (url.indexOf("jar:")) return;

  var jarfile;

  url = url.substring(4);

  var expos = url.indexOf("!");
  if (expos > 0){
    url = url.substring(0,expos);

    if (url.indexOf("resource:/") == 0){
      url = url.substring(10);

      jarfile = this.gDirService.get("CurProcD", jslibI.nsIFile);

      var fileparts = url.split("/");
      for (var t=0; t<fileparts.length; t++){
        jarfile.append(fileparts[t]);
      }
    }
    else if (url.indexOf("file://") == 0){
      var ioService = jslibGetService("@mozilla.org/network/io-service;1", "nsIIOService");
      var fileuri = ioService.newURI(url,"",null);
      if (fileuri instanceof jslibI.nsIFileURL){
        jarfile = fileuri.file;
      }
    }
  }

  if (!this.filesToDeleteHash[jarfile.path]){
    this.filesToDeleteHash[jarfile.path] = jarfile;
    this.filesToDelete.push(jarfile);
  }
},

/**
 * Generate the list of overlays referenced in a contents.rdf file.
 */
generateOverlaysToDelete : function(aDS, chromeDir, overlayType)
{
  var iterateOverlays = function(container, overlayFile, uninstallObj)
  {
    if ((container instanceof jslibI.nsIRDFResource) &&
        (overlayFile instanceof jslibI.nsIRDFLiteral)){
      uninstallObj.overlaysToDelete.push(
        { overlaidFile: container,
          overlayFile: overlayFile,
          chromeDir : chromeDir,
          type: overlayType });
    }
  }

  var iterateOverlaids = function(container, overlaidFile, uninstallObj)
  {
    uninstallObj.iterateContainer(aDS, overlaidFile, iterateOverlays);
  }

  var oroot = this.gRDF.GetResource("urn:mozilla:" + overlayType);
  this.iterateContainer(aDS, oroot, iterateOverlaids);
},

/**
 * Remove an overlay from the overlayinfo.
 */
removeOverlay : function(overlay)
{
  jslibPrint("removeOverlay");
  var overlayItems = this.splitURL(overlay.overlaidFile.Value);

  var overlayRdfFile = overlay.chromeDir.clone();
  overlayRdfFile.append("overlayinfo");
  overlayRdfFile.append(overlayItems.packagename);
  overlayRdfFile.append(overlayItems.provider);
  overlayRdfFile.append(overlay.type + ".rdf");

  var ioService = jslibGetService("@mozilla.org/network/io-service;1", "nsIIOService");
  var overlayRdfUrl = ioService.newFileURI(overlayRdfFile).spec;
  var dsource = this.gRDF.GetDataSourceBlocking(overlayRdfUrl);

  try {
    jslibDebug("Uninstall ---- Uncontain Overlay " + this.RDFGetValue(overlay.overlayFile) +
         " from " + this.RDFGetValue(overlay.overlaidFile) + "\n");
    var container = jslibCreateInstance("@mozilla.org/rdf/container;1", "nsIRDFContainer");
    container.Init(dsource, overlay.overlaidFile);
    if (this.ACTIVE_MODE) container.RemoveElement(overlay.overlayFile, true);
  }
  catch (ex) { jslibDebug(ex); }

  if (this.ACTIVE_MODE &&
      dsource instanceof jslibI.nsIRDFRemoteDataSource)
    dsource.Flush();
},

/**
 * split a chrome URL into component parts.
 *
 * The algorithm was taken from mozilla/rdf/chrome/src/nsChromeRegistry.cpp
 */
splitURL : function(url)
{
  if (url.indexOf("chrome://")) return null;

  var packagename = url.substring(9);
  var slashidx = packagename.indexOf("/");
  if (slashidx == -1) return null;

  var provider = packagename.substring(slashidx + 1);
  packagename = packagename.substring(0,slashidx);
 
  slashidx = provider.indexOf("/");
  if (slashidx >= 0){
    provider = provider.substring(0,slashidx);
  }

  return {
    packagename: packagename,
    provider: provider
  };
},

/**
 * Useful debugging function to convert an nsIRDFNode into a string.
 */
RDFGetValue : function(node)
{
  return ((node instanceof jslibI.nsIRDFResource) ? node.Value :
          ((node instanceof jslibI.nsIRDFLiteral) ? node.Value : ""));
},

/**
 * Remove references to a package from chrome.rdf.
 */
removeFromChrome : function (dsource, rootseq, packres) 
{
  jslibPrint("removeFromChrome");
  var packresnode = packres.QueryInterface(jslibI.nsIRDFNode);

  try {
    jslibDebug("Uninstall ---- Uncontain " + packres.Value + " from " +
               rootseq.Value + "\n");
    var container = jslibCreateInstance("@mozilla.org/rdf/container;1", "nsIRDFContainer");
    container.Init(dsource, rootseq);
    if (this.ACTIVE_MODE) container.RemoveElement(packresnode, true);
  }
  catch (ex) { jslibDebug(ex); }

  var arcs = dsource.ArcLabelsOut(packres);

  while(arcs.hasMoreElements()) {
    var arc = arcs.getNext();
    
    var prop = arc.QueryInterface(jslibI.nsIRDFResource);

    var targets = dsource.GetTargets(packres, prop, true);

    while (targets.hasMoreElements()) {
      var target = targets.getNext();

      var targetNode = target.QueryInterface(jslibI.nsIRDFNode);
      jslibDebug("Uninstall ---- Unassert [" + packres.Value + " , " +
            prop.Value + " , " + this.RDFGetValue(target) + "]\n");
      if (this.ACTIVE_MODE) dsource.Unassert(packres, prop, targetNode);
    }
  }
},

removeFromInstalledChrome : function(chromeDir)
{
  jslibPrint("removeFromInstalledChrome");
  chromeDir.append("installed-chrome.txt");
  var ifile = new File(chromeDir.path);
  ifile.open("r");

  var changeNeeded = false;

  try {
    var content = "";

    while (!ifile.EOF){
      var found = false;
      var ln = ifile.readline();

      for (uri in this.baseURIs){
        var idx = ln.indexOf(uri);
        if ((idx > 0) && (idx == ln.length - uri.length)){
          jslibDebug("Uninstall ---- Removing from installed-chrome.txt : " + ln + "\n");
          found = true;
          changeNeeded = true;
        }
      }
      if (!found) content += ln + "\n";
    }
  }
  finally {
    ifile.close();
  }

  if (this.ACTIVE_MODE && changeNeeded){
    ifile.open("w",0664);
    try {
      ifile.write(content);
    }
    finally {
      ifile.close();
    }
  }
}

  } // END CLASS
  jslibDebug('*** load: '+JS_LIB_UNINSTALL_FILE+' OK');

} // END BLOCK DOM WINDOW CHECK  

// END BLOCK JS_LIB_LOADED CHECK 
// If jslib base library is not loaded, dump this error.
} else {
  const JS_LIB_UNINSTALL_LOAD_MSG = "JS_LIB_UNISTALL library not loaded:\n" 
                                  + " \tTo load use: chrome://jslib/content/jslib.js\n" 
                                  + " \tThen: include(jslib_uninstall);\n\n";

  dump(JS_LIB_UNINSTALL_LOAD_MSG);
}
