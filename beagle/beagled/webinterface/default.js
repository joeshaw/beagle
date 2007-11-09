/*
 * Copyright (2007) Debajyoti Bera
 * Copyright (2007) Nirbheek Chauhan
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 * 
 *
 */

/* Generic TODOs/FIXMEs:
 * - set_results_style: instead of showing/hiding results, only search for those that the user has checked
 *   query and append results as he checks the categories
 * - Ev!L stuff like `element.innerHTML` needs to be replaced by nice clean DOM stuff; except where good for performance
 * - Code needs to be made cleaner... (category_is_being_shown/categories_being_shown comes to mind as an example)
 */

function search ()
{
	// get the search string
	var query_str = document.queryform.querytext.value;
	// FIXME: Escape query_str
	// What kind of escaping? I couldn't do any code injection :-/
	if (query_str.length == 0) {
		return;
	} else if (query_str == '42') {
		window.location = "http://en.wikipedia.org/wiki/The_Answer_to_Life,_the_Universe,_and_Everything";
		return;
	} else if (query_str == '4u7h0rz') {
		window.location = "http://svn.gnome.org/viewvc/beagle/trunk/beagle/AUTHORS?view=markup";
		return;
	}

	var req_string = '<?xml version="1.0" encoding="utf-8"?> <RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"> <Message xsi:type="Query"> <IsIndexListener>false</IsIndexListener> <Parts> <Part xsi:type="QueryPart_Human"> <Logic>Required</Logic> <QueryString>' + query_str + '</QueryString> </Part> </Parts> <MimeTypes/> <HitTypes/> <Sources/> <QueryDomain>Local System</QueryDomain> <MaxHits>100</MaxHits> </Message> </RequestWrapper> ';

	var begin_date = Date.now ();

	xmlhttp.onreadystatechange = function () {
		state_change_search (begin_date);
	};
	xmlhttp.open ("POST", "/", true);
	//XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
	xmlhttp.overrideMimeType ('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
	//xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
	xmlhttp.send (req_string);

	// https://bugzilla.mozilla.org/show_bug.cgi?id=167801
	// The focus would have moved to some hidden element and would be
	// lost foreveh! Instead ... we cheat and set the focus explicitly
	// to a harmless element.
	document.queryform.querysubmit.focus ();

	document.queryform.querytext.disabled = true;
	document.queryform.querysubmit.disabled = true;
	document.getElementById ('status').style.display = 'block';
	return false;
}

function get_information ()
{
	var req_string = '<?xml version="1.0" encoding="utf-8"?><RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Message xsi:type="DaemonInformationRequest"> <GetVersion>true</GetVersion><GetSchedInfo>true</GetSchedInfo><GetIndexStatus>true</GetIndexStatus> <GetIsIndexing>true</GetIsIndexing></Message></RequestWrapper>';

	xmlhttp.onreadystatechange = function () {
		state_change_info ();
	};
	xmlhttp.open ("POST", "/", true);
	// XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
	xmlhttp.overrideMimeType ('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
	//xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
	xmlhttp.send (req_string);

	document.queryform.querytext.disabled = true;
	document.queryform.querysubmit.disabled = true;
	document.getElementById ('status').style.display = 'block';
	return false;
}

function get_process_information ()
{
	xmlhttp.onreadystatechange = state_change_info;
	xmlhttp.open ("GET", "/processinfo", true);
	xmlhttp.send (null);
}

function shutdown_beagle ()
{
	var req_string = '<?xml version="1.0" encoding="utf-8"?><RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Message xsi:type="ShutdownRequest"/></RequestWrapper>';

	xmlhttp.onreadystatechange = function () {
		var results = document.getElementById ('results');
		reset_document_content ();
		if (xmlhttp.readyState == 4) {
			var message = document.createElement ('i');
			var text = document.createTextNode ('Shutdown request sent to beagle');
			message.appendChild (text);
			results.appendChild (message);
			document.getElementById ('status').style.display = 'none';
			document.queryform.querytext.disabled = false;
			document.queryform.querysubmit.disabled = false;
		}
	};

	xmlhttp.open ("POST", "/", true);
	// XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
	xmlhttp.overrideMimeType ('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
	//xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
	xmlhttp.send (req_string);

	document.queryform.querytext.disabled = true;
	document.queryform.querysubmit.disabled = true;
	document.getElementById ('status').style.display = 'block';
	return false;
}

function state_change_search (begin_date)
{
	if (xmlhttp.readyState == 4) {
		// FIXME: Should also check for status 200

		var end_date = Date.now ();
		var elapsed = (end_date - begin_date)/1000;

		//dump("Response:\n");
		//dump(xmlhttp.responseText);
		//dump("\n");
		res = xmlhttp.responseText;

		// if charset is x-user-defined split by \uF7FF
		// if charset is utf-8, split by FFFD
		// And dont ask me why!
		var responses = res.split ('\uFFFD'); 

		// Appending without clearing is bad... mmkay?
		reset_document_content ();
		document.getElementById ('timetaken').textContent = elapsed + ' secs';
		var num_matches = 0;

		// Process hit xml nodes with xsl and append with javascript
		for (var i = 1; i < responses.length; ++i) {
			if (responses [i].length <= 0)  {
				continue;
			}
			var response_dom = parser.parseFromString (responses [i], "text/xml");
			// FIXME: ignoring all other messages
			var hits = response_dom.getElementsByTagName ('Hit');
			for (var j = 0; j < hits.length; ++j) {
				// Copy the hit for modification(s)
				var div_id = classify_hit (hits [j]);
				var div = document.getElementById (div_id);
				// Modifications...
				var hit = hit_processor.transformToFragment (hits [j], document);
				// Get timestamp and process it
				var timestamp = hit.firstChild.firstChild.lastChild;
				timestamp.innerHTML = '<b>Last Edited:</b>&nbsp;'+humanise_timestamp (timestamp.textContent);
				// Process Hit using hitresult.xsl and append to `div`
				div.appendChild (hit);
			}

			var num_matches_elems = response_dom.getElementsByTagName ('NumMatches');
			if (num_matches_elems.length > 0) {
				var n = parseInt (num_matches_elems [0].textContent);
				if (n > 0) {
					num_matches += n;
					document.getElementById ('numhits').textContent = num_matches;
				}
			}
		}

		set_results_style ();
		if (num_matches  == 0) {
			document.getElementById ('NoResults').style.display = 'block';
		} else {
			document.getElementById ('NoResults').style.display = 'none';
		}
		document.getElementById ('topbar').style.display = 'block';
		document.getElementById ('status').style.display = 'none';

		document.queryform.querytext.disabled = false;
		document.queryform.querytext.focus ();
		document.queryform.querysubmit.disabled = false;
	}
}

function state_change_info ()
{
	if (xmlhttp.readyState == 4) {
		// FIXME: Should also check for status 200

		//dump("Response:\n");
		//dump(xmlhttp.responseText);
		//dump("\n");

		var res = xmlhttp.responseText;

		// if charset is x-user-defined split by \uF7FF
		// if charset is utf-8, split by FFFD
		// And dont ask me why!
		var responses = res.split ('\uFFFD'); 

		// Appending without clearing is bad... mmkay?
		reset_document_style ();
		reset_document_content ();

		// There should be only one response in responses
		for (var i = 0; i < responses.length; ++i) {
			if (responses [i].length <= 0)
				continue;
			//dump (responses [i]);
			//dump ('\n');

			var response_dom = parser.parseFromString (responses [i], "text/xml");
			var fragment = query_processor.transformToFragment (response_dom, document);
			document.getElementById ('results').appendChild (fragment);
		}

		document.getElementById ('status').style.display = 'none';

		document.queryform.querytext.disabled = false;
		document.queryform.querysubmit.disabled = false;
	}
}

function classify_hit (hit)
{
	var categories = mappings.getElementsByTagName ('Categories') [0].getElementsByTagName ('Category');
	var properties = hit.getElementsByTagName ('Property');
	var matchers, matchers_value, matchers_key, matcher;
	// Iterate over all the categories in mappings.xml
categs:	for (var i = 0; i < categories.length; ++i) {
		matchers = mappings.evaluate ('NotType|Type', categories [i], null, XPathResult.ORDERED_NODE_ITERATOR_TYPE, null);
		// Iterate over all the <NotType> and <Type>s
		while (matcher = matchers.iterateNext ()) {
			matchers_key = matcher.getAttribute ('Key');
			matchers_value = matcher.getAttribute ('Value');
			// For each property of the hit
			for (var k = 0; k < properties.length; ++k) {
				// If it matches
				if (properties [k].getAttribute ('Key') == matchers_key &&
				    properties [k].getAttribute ('Value') == matchers_value) {
					if (matcher.nodeName == "NotType") {
						// It does not match this category
						// Go to the next category.
						continue categs;
					} else if (matcher.nodeName == "Type") {
						// Match found, return corresponding div id
						return categories [i].getAttribute ('Name');
					}
				}
			}
		}
	}
	// No rule for `hit` found, classifying as "Others"
	return 'Others';
}

// We're putting these here so they're reused. Much faster this way.
var regexp = /^(.{4})(.{2})(.{2})/;
var time = new Date ().toLocaleFormat ("%Y%m%d%H%M%S");
var timestamp = new Date ();
function humanise_timestamp (ts) 
{
	var array = regexp.exec (ts);
	timestamp.setFullYear (array [1]);
	// Erm. Months are counted from 0 in javascript for some reason <_<
	timestamp.setMonth (array [2] - 1);
	timestamp.setDate (array [3]);
	// < 1 day
	if ( (time - ts) < 1000000 ) {
		return "Today";
	// < 2 days
	} else if ( (time - ts) < 2000000 ) {
		return "Yesterday";
	// < 7 days
	} else if ( (time - ts) < 7000000 ) {
		return timestamp.toLocaleFormat ('%A');
	// < 1 year
	} else if ( (time - ts) < 10000000000 ) {
		return timestamp.toLocaleFormat ('%B %e');
	} else {
		return timestamp.toLocaleFormat ('%B %e, %Y')
	}
}

function categories_being_shown ()
{
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	var numcategs = 0;
	for (var i = 0; i < category_checkboxes.length; ++i) {
		if (category_checkboxes [i].checked) {
			numcategs++;
		}
	}
	return numcategs;
}

// FIXME: This should just use index.xsl or something
function reset_document_content ()
{
	var results = document.getElementById ('results');
	var categories = document.getElementById ('topbar-left').getElementsByTagName ('input');
	var div;
	// Reset the hit results
	results.innerHTML = '';
	for (var i = 0; i < categories.length; ++i) {
		div = document.createElement ('div');
		div.setAttribute ('class', 'Hits');
		div.setAttribute ('id', categories [i].name);
		results.appendChild (div);
	}
	div = document.createElement ('div');
	div.setAttribute ('class', 'Hits');
	div.setAttribute ('id', 'NoResults');
	div.setAttribute ('style', 'display: none;');
	div.appendChild (document.createTextNode ('No Results'));
	results.appendChild (div);
	document.getElementById ('numhits').textContent = '0';
}

function reset_document_style ()
{
	document.getElementById ('topbar').style.display = 'none';
	var results_categories = document.getElementById ('results').childNodes;
	// Reset the hit results' display
	for (var i = 0; i < results_categories.length; ++i) {
		results_categories [i].style.display = 'none';
	}
}

function set_results_style ()
{
	// XXX Gotcha: this code assumes the arrays below match w.r.t. their indexes
	// This will always be satisfied however, see mappings.xml: Note 2
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	var results_categories = document.getElementById ('results').childNodes;
	if (categories_being_shown () > 0) {
		for (var i = 0; i < category_checkboxes.length; ++i) {
			if (category_checkboxes [i].checked) {
				results_categories [i].style.display = 'block';
			}
		}
	} else {
		for (var i = 0; i < results_categories.length; ++i) {
			results_categories [i].style.display = 'block';
		}
	}
}

function toggle_hit (hit_toggle)
{
	if (hit_toggle.textContent == '[-]') {
		hit_toggle.textContent = '[+]';
		//this.<span class="Uri">.<div class="Title">.<br/>.<div class="Data">
		hit_toggle.parentNode.parentNode.nextSibling.nextSibling.style.display = 'none';

	} else {
		hit_toggle.textContent = '[-]';
		hit_toggle.parentNode.parentNode.nextSibling.nextSibling.style.display = 'block';
	}
}

function show_all_categories ()
{
	// Get all the category checkboxes
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	// Get all the result categories
	var results_categories = document.getElementById ('results').childNodes;
	// Show all results
	for (var i = 0; i < results_categories.length; ++i) {
		if (results_categories [i].id == "NoResults")
			continue;
		results_categories [i].style.display = 'block';
	}
	// Uncheck all the categories
	for (var i = 0; i < category_checkboxes.length; ++i) {
		category_checkboxes [i].checked = false;
	}
}

function toggle_category (category)
{
	// Get all the results' categories
	var results_categories = document.getElementById ('results').childNodes;
	// If the user just checked the box
	if (category.checked) {
		// If this is the first category being shown..
		if (categories_being_shown () == 1) {
			// Hide all results except the one selected
			for (var i = 0; i < results_categories.length; ++i) {
				if (results_categories [i].id == category.name)
					continue;
				results_categories [i].style.display = 'none';
			}
		} else {
			// Show result corresponding to category
			document.getElementById (category.name).style.display = 'block';
		}
	} else {
		// If none of the categories are being shown now
		if (categories_being_shown () == 0) {
			// The user doesn't like being shown nothing; show all the categories
			show_all_categories ();
		} else {
			// Hide result corresponding to category
			document.getElementById (category.name).style.display = 'none';
		}
	}
}

/******* Initial fetching and loading of the xsl/xml files *********/

// This works everywhere except IE
var xmlhttp = new XMLHttpRequest (); 
var query_processor = new XSLTProcessor ();
var hit_processor = new XSLTProcessor ();
var parser = new DOMParser ();
var mappings;

// Load statusresult.xsl using synchronous (third param is set to false) XMLHttpRequest
xmlhttp.open ("GET", "/statusresult.xsl", false);
xmlhttp.send (null);

// Process it and store it for later reuse
query_processor.importStylesheet (xmlhttp.responseXML);

// Get Hit processing xsl
xmlhttp.open ("GET", "/hitresult.xsl", false);
xmlhttp.send (null);
hit_processor.importStylesheet (xmlhttp.responseXML);

// Get the mappings.xml
xmlhttp.open ("GET", "/mappings.xml", false);
xmlhttp.send (null);
mappings = xmlhttp.responseXML;
