/*
 * Beagle Extension: Index webpages you visit using the Beagle Indexing Engine.
 * An Extension for the Firefox (and Mozilla?) Browser.
 */

// Initiate a new preference instance.
var gPref = Components.classes['@mozilla.org/preferences-service;1'].getService(Components.interfaces.nsIPrefBranch);

// Declare form variables.
var _elementIDs = [
  'beagle.context.active',
  'beagle.security.active',
  'beagle.security.filters'
];

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
    else if (element.id == 'beagle.security.filters')
    {
      try {
        var val = gPref.getCharPref("beagle.security.filters");
        var items = val.split(';');
        var listbox = document.getElementById('beagle.security.filters');

        for (var j = 0; j < items.length; j++){
          if(items[j] != ''){
            var item = listbox.appendItem(items[j], items[j]);
          }
        }
      } catch(e) {
          // We don't seem to care about this.
      }
    }
  }

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

        for (var j = 0; j < sPref.length; j++)
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
    else if (element.id == 'beagle.security.filters')
    {
      var val = "";
      for (var j = 0; j < element.getRowCount(); j++){
        var listitem = element.getItemAtIndex(j);
        val += listitem.value + ";";
      }
      gPref.setCharPref(element.id, val);
    }
  }
}

function beaglePrefsAddFilter() {
  var filter = document.getElementById('beagle.filter');
  var listbox = document.getElementById('beagle.security.filters');
  if (filter.value != ''){
      listbox.appendItem(filter.value, filter.value);
  }
  filter.value = '';
}

function beaglePrefsRemoveFilter() {
  var listbox = document.getElementById('beagle.security.filters');
  listbox.removeItemAt(listbox.selectedIndex);
}

function updateFilterAddButton() {
  var button = document.getElementById('beagle.filter.add');
  var filter = document.getElementById('beagle.filter');

  if (filter.value != ''){
    button.disabled = false;
  } else {
    button.disabled = true;
  }
}

function updateFilterRemoveButton() {
  var button = document.getElementById('beagle.filter.remove');
  var listbox = document.getElementById('beagle.security.filters');

  if (listbox.selectedCount > 0){
    button.disabled = false;
  } else {
    button.disabled = true;
  }
}
