/*** -*- Mode: Javascript; tab-width: 2;

The contents of this file are subject to the Mozilla Public
License Version 1.1 (the "License"); you may not use this file
except in compliance with the License. You may obtain a copy of
the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS
IS" basis, WITHOUT WARRANTY OF ANY KIND, either express or
implied. See the License for the specific language governing
rights and limitations under the License.

The Original Code is Mozdev Group, Inc. code.
The Initial Developer of the Original Code is Pete Collins.

Portions created by Mozdev Group, Inc. are
Copyright (C) 2003 Mozdev Group, Inc.  All
Rights Reserved.

Contributor(s): Pete Collins <pete@mozdev.org)> (original author)
Henrik Gemal <http://gemal.dk>

***/

/****************** Globals **********************/

if(typeof(JS_LIB_LOADED)=='boolean') {

const JS_COMMONWINDOW_FILE     = "commonWindow.js";
const JS_COMMONWINDOW_LOADED   = true;

const JS_TOP_LEFT = 0;
const JS_TOP_CENTER = 1;
const JS_TOP_RIGHT = 2;

const JS_MIDDLE_LEFT = 3;
const JS_MIDDLE_CENTER = 4;
const JS_MIDDLE_RIGHT = 5;

const JS_BOTTOM_LEFT = 6;
const JS_BOTTOM_CENTER = 7;
const JS_BOTTOM_RIGHT = 8;
const JS_UNINSTALL_URL = JS_LIB_PATH + "install/uninstall.xul";

/****************** Common Dialog Object Class *********************/

function CommonWindow(aURL, aWidth, aHeight) 
{
  if (typeof(aWidth)=="number")
    this.mWidth=aWidth;

  if (typeof(aHeight)=="number")
    this.mHeight=aHeight;

  if (typeof(aURL)=="string")
    this.mURL=aURL;
  
  return; 

} // constructor

CommonWindow.prototype  = {

  mWidth : 0,
  mHeight : 0,
  mURL : null,
  mPosition : JS_TOP_LEFT,
  mX : 0,
  mY : 0,

set width (aWidth) {
  this.mWidth = aWidth;
},

get width () {
  return this.mWidth;
},

set height (aHeight) {
  this.mHeight = aHeight;
},

get height () {
  return this.mHeight;
},

set position (aPos) 
{
 var x,y;
 switch (aPos) {
   case JS_MIDDLE_CENTER:
     this.mPosition=aPos;
     x = Math.round(this.mWidth/2);
     y = Math.round(this.mHeight/2);

     var sY = window.screen.height/2;
     var sX = window.screen.width/2;

     jslibPrint("width: "+this.mWidth);
     jslibPrint("height: "+this.mHeight);

     jslibPrint("x: "+x+"y: "+y);
     jslibPrint("sX: "+sX+"sY: "+sY);

     this.mY = sY-y;
     this.mX = sX-x;
     jslibPrint("mX: "+this.mX);
     jslibPrint("mY: "+this.mY);
     break;
  
   default:
    this.mPosition=JS_TOP_LEFT;
    jslibDebug("not implemented yet setting to JS_TOP_LEFT for now");
  }
},

get position () {

  var rv="";
  switch (this.mPosition) {
    case JS_TOP_LEFT:
      rv="JS_TOP_LEFT";
      break;

    case JS_TOP_CENTER:
      rv="JS_TOP_CENTER";
      break;

    case JS_TOP_RIGHT:
      rv="JS_TOP_RIGHT";
      break;

    case JS_MIDDLE_LEFT:
      rv="JS_MIDDLE_LEFT";
      break;

    case JS_MIDDLE_CENTER:
      rv="JS_MIDDLE_CENTER";
      break;

    case JS_MIDDLE_RIGHT:
      rv="JS_MIDDLE_RIGHT";
      break;

    case JS_BOTTOM_LEFT:
      rv="JS_BOTTOM_LEFT";
      break;

    case JS_BOTTOM_CENTER:
      rv="JS_BOTTOM_CENTER";
      break;

    case JS_BOTTOM_RIGHT:
      rv="JS_BOTTOM_RIGHT";
      break;
  }
  return rv;
},

set url (aURL) {
  this.mURL = aURL;
},

get url () {
  return this.mURL;
},

openFullScreen : function () {
  
  if (!this.mURL)
    return;

  var h = window.screen.height;
  var w = window.screen.width;

  var win_prefs = "chrome,dialog=no,width="+w+
                  ",height="+h+",screenX=0,screenY=0";

  window.openDialog(this.mURL, "_blank", win_prefs);
},

openWebTop : function () {
  
  if (!this.mURL)
    return;

  var h = window.screen.height;
  var w = window.screen.width;
  var win_prefs = "chrome,popup,scrollbars=yes,width="+w+
                  ",height="+h+",screenX=0,screenY=0";

  jslibDebug(this.mURL+"_blank"+win_prefs);
  window.openDialog(this.mURL, "_blank", win_prefs);
},

openUninstallWindow : function (aPackage, aCallback) {
  
  if (!aPackage) {
    jslibDebug("Please provide a package name to uninstall")
    return;
  }
  var win_prefs = "chrome,dialog,dependent=no,resize=yes,screenX="+this.mX+
                  ",screenY="+this.mY+
                  ",width="+this.mWidth+",height="+this.mHeight;

  window.openDialog(JS_UNINSTALL_URL, "_blank", win_prefs, aPackage, aCallback);
},

openAbout : function () {
  
  if (!this.mURL)
    return;

  var h = this.mHeight;
  var w = this.mWidth;
  var win_prefs = "chrome,dialog,modal,dependent=no,resize=no,width="+w+
                  ",height="+h+",screenX="+this.mX+",screenY="+this.mY;

  window.openDialog(this.mURL, "_blank", win_prefs);
},

openSplash : function () {
  
  jslibPrint("open splash . . . ");

  if (!this.mURL)
    return;

  var h = this.mHeight;
  var w = this.mWidth;
  var popup="popup,";
  if (/Mac/g.test(window.navigator.platform))
    popup="";
  var win_prefs = "chrome,dialog=no,titlebar=no,"+popup+"width="+w+
                  ",height="+h+",screenX="+this.mX+",screenY="+this.mY;

  window.openDialog(this.mURL, "_blank", win_prefs);
},

open : function () 
{
  if (!this.mURL)
    return;

  var win_prefs = "chrome,dialog=no,dependent=no,resize=yes,screenX="+this.mX+",screenY="+this.mY+
                  ",width="+this.mWidth+",height="+this.mHeight;

  window.openDialog(this.mURL, "_blank", win_prefs);
},

openDialog : function () {
  
  if (!this.mURL)
    return;

  var win_prefs = "chrome,dialog,dependent=yes,resize=yes,screenX="+this.mX+",screenY="+this.mY+
                  ",width="+this.mWidth+",height="+this.mHeight;

  window.openDialog(this.mURL, "_blank", win_prefs);
},

openModalDialog : function () {
  
  if (!this.mURL)
    return;

  var win_prefs = "chrome,dialog,dependent=no,modal,resize=yes,screenX="+this.mX+",screenY="+this.mY+
                  ",width="+this.mWidth+",height="+this.mHeight;

  window.openDialog(this.mURL, "_blank", win_prefs);
},

get openWindows ()
{
  var wm = jslibGetService("@mozilla.org/appshell/window-mediator;1", 
                           "nsIWindowMediator");
  jslibDebug(wm);
  var enumerator = wm.getEnumerator(null);

  var winArray = new Array();
  while (enumerator.hasMoreElements()) {
    var domWindow = enumerator.getNext();
    winArray.push(domWindow);
  }
   
  return winArray;
},
/********************* help *****************************
* void getter help
*
*   Returns the methods in this object
*
* return values on success and failure
*   aStr   The methods in this object
*
* useage:
*   <string> = obj.help;
****************************************************/
get help() {

  const help =

    "\n\nFunction and Attribute List:\n"                  +
    "\n";                  

  return help;
} 

} 

jslibDebug('*** load: '+JS_COMMONWINDOW_FILE+' OK');

} // END BLOCK JS_LIB_LOADED CHECK

// If jslib base library is not loaded, dump this error.
else {
   dump("JS_BASE library not loaded:\n"
        + " \tTo load use: chrome://jslib/content/jslib.js\n" 

        + " \tThen: include('chrome://jslib/content/xul/commonWindow.js');\n\n");

}; // END FileSystem Class
