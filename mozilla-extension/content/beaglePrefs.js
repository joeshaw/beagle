/*
 * Beagle Extension: Index webpages you visit using the Beagle Indexing Engine.
 * An Extension for the Firefox (and Mozilla?) Browser.
 */

// Initiate a new preference instance.
var gPref = Components.classes['@mozilla.org/preferences-service;1'].getService(Components.interfaces.nsIPrefBranch);

// Declare form variables.
var _elementIDs = [
  'beagle.context.active',
  'beagle.context.sep.a',
  'beagle.context.sep.b',
  'beagle.security.active',
];

function beaglePrefsFlip()
{
  // Handle enabling/disabling prefs based on settings
  /*
  var bPref = document.getElementById('beagle.context.active').checked;
  document.getElementById('beagle.context.sep.a').disabled = !(bPref);
  document.getElementById('beagle.context.sep.b').disabled = !(bPref);
  */
}

function beaglePrefsInit()
{
  for( var i = 0; i < _elementIDs.length; i++ )
  {
    var elementID = _elementIDs[i];
    var element = document.getElementById(elementID);

    if (!element)
    {
      continue;
    }
    else if (element.localName == 'checkbox')
    {
      try { element.checked = gPref.getBoolPref(elementID); }
      catch(e) { element.checked = false; }
    }
    else if (element.localName == 'radiogroup')
    {
      try { element.selectedItem = element.childNodes[gPref.getIntPref(elementID)]; }
      catch(e) { element.selectedItem = element.childNodes[0]; }
    }
    else if (element.localName == 'textbox')
    {
      if (element.getAttribute('preftype') == 'int')
      {
        try { element.value = gPref.getIntPref(elementID); }
        catch(e) { element.value = 180; }
      }
      else
      {
        try { element.value = gPref.getCharPref(elementID); }
        catch(e) { element.value = ''; }
      }
    }
  }

  beaglePrefsFlip();
}

function beaglePrefsSave()
{
  for( var i = 0; i < _elementIDs.length; i++ )
  {
    var elementID = _elementIDs[i];
    var element = document.getElementById(elementID);

    if (!element)
    {
      continue;
    }
    else if (element.localName == 'checkbox')
    {
      gPref.setBoolPref(elementID, element.checked);
    }
    else if (element.localName == 'radiogroup')
    {
      gPref.setIntPref(elementID, parseInt(element.value));
    }
    else if (element.localName == 'textbox')
    {
      if (element.getAttribute('preftype') == 'int')
      {
        var bOkay = true;
        var cPref = '';
        var sPref = element.value.replace(/^[0]*/);
        var sWork = "0123456789";

        for (j = 0; j < sPref.length; j++)
        {
          if (sWork.indexOf(sPref.charAt(j)) == -1) bOkay = false;
          else cPref = cPref + sPref.charAt(j);
        }

        if (cPref.length == 0 ) cPref = '0';
        var iPref = parseInt(cPref);
        if (iPref < 180) iPref = 180;
        gPref.setIntPref(elementID, iPref);
      }
      else
      {
        gPref.setCharPref(elementID, element.value);
      }
    }
  }

  beaglePrefsFlip();
}
