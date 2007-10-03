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

/* Coding guidelines:
 * Follow ../../HACKING +
 *	use under_score_in_method_and_variable_names
 */

function search ()
{
	// get the search string
	var query_str = document.queryform.querytext.value;
	// FIXME: Escape query_str
	if (query_str.length == 0)
		return;

	var req_string = '<?xml version="1.0" encoding="utf-8"?> <RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"> <Message xsi:type="Query"> <IsIndexListener>false</IsIndexListener> <Parts> <Part xsi:type="QueryPart_Human"> <Logic>Required</Logic> <QueryString>' + query_str + '</QueryString> </Part> </Parts> <MimeTypes /> <HitTypes /> <Sources /> <QueryDomain>Local System</QueryDomain> <MaxHits>100</MaxHits> </Message> </RequestWrapper> ';

	var begin_date = Date.now ();

	xmlhttp.onreadystatechange = function () {state_change_search (begin_date);};
	xmlhttp.open ("POST", "/", true);
	//XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
	xmlhttp.overrideMimeType ('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
	//xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
	xmlhttp.send (req_string);

	document.queryform.querytext.disabled = true;
	document.getElementById ('status').style.display = 'block';
	// START FIXME: Remove when search filtering is "remembered" between searches
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	// Uncheck all the categories
	for (var i = 0; i < category_checkboxes.length; ++i) {
		category_checkboxes [i].checked = false;
	}
	category_is_being_shown = false;
	// END FIXME
	return false;
}

//function get_information ()
//{
//	var req_string = '<?xml version="1.0" encoding="utf-8"?><RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Message xsi:type="DaemonInformationRequest"> <GetVersion>true</GetVersion><GetSchedInfo>true</GetSchedInfo><GetIndexStatus>true</GetIndexStatus> <GetIsIndexing>true</GetIsIndexing></Message></RequestWrapper>';
//
//	xmlhttp.onreadystatechange = state_change_info;
//	xmlhttp.open ("POST", "/", true);
//	//XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
//	xmlhttp.overrideMimeType ('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
//	//xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
//	xmlhttp.send (req_string);
//
//	document.queryform.querytext.disabled = true;
//	document.getElementById ('status').style.display = 'block';
//	return false;
//}

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
		var parser = new DOMParser ();

		// Appending without clearing is bad... mmkay?
		document.getElementById ('results').innerHTML = '';
		document.getElementById ('timetaken').innerHTML = (elapsed + ' seconds');
		document.getElementById ('numhits').innerHTML = 0;
		var num_matches = 0;

		// Create blank xml document
		var doc = document.implementation.createDocument ('', '', null);
		// Hack to get the xml declaration (https://bugzilla.mozilla.org/show_bug.cgi?id=318086)
		var xml_decl = doc.createProcessingInstruction ('xml', 'version="1.0" encoding="utf-8"'); 

		// Create element <ResponseWrapper ...>
		var res_wrapper = doc.createElement ('ResponseWrapper');
		res_wrapper.setAttribute ('xmlns:xsi', 'http://www.w3.org/2001/XMLSchema-instance');
		res_wrapper.setAttribute ('xmlns:xsd', 'http://www.w3.org/2001/XMLSchema');

		// Create element <Message ...>
		var msg_elem = doc.createElement ('Message');
		msg_elem.setAttributeNS ('http://www.w3.org/2001/XMLSchema-instance', 'xsi:type', 'HitsAddedResponse');

		// Create element <Hits>
		var hits_elem = doc.createElement ('Hits');

		// XXX: Process xml nodes before merging and sending to xsl
		for (var i = 0; i < responses.length; ++i) {
			if (responses [i].length <= 0)
				continue;

			var response_dom = parser.parseFromString (responses [i], "text/xml");
			// FIXME: ignoring all other messages
			hits = response_dom.getElementsByTagName ('Hit');
			for (var j = 0; j < hits.length; ++j) {
				// Clone every <Hit> (and its children) and append inside <Hits>
				hits_elem.appendChild (hits [j].cloneNode (true));
			}

			var num_matches_elems = response_dom.getElementsByTagName ('NumMatches');
			if (num_matches_elems.length > 0) {
				var n = parseInt (num_matches_elems [0].textContent);
				if (n > 0) {
					num_matches += n;
					document.getElementById ('numhits').innerHTML = num_matches;
				}
			}
		}

		// Append <Hits> inside <Message ...>
		msg_elem.appendChild (hits_elem);
		// Append <Message ...> inside <ResponseWrapper ...>
		res_wrapper.appendChild (msg_elem);
		// Append everything inside the blank xml doc
		doc.appendChild (res_wrapper);
		doc.insertBefore (xml_decl, res_wrapper);

		// Send	xml doc to xsl for processing
		var result = xsltProcessor.transformToFragment (doc, document);
		// Append resultant html inside <div id="results">
		document.getElementById ('results').appendChild (result);
		// If no results..
		if (document.getElementById ('results').textContent == '') {
			var no_results = document.createElement ('p');
			no_results.appendChild (document.createTextNode ('No Results'));
			document.getElementById ('results').appendChild (no_results);
		}
		document.getElementById ('topbar').style.display = 'block';
		document.getElementById ('status').style.display = 'none';
	}

	document.queryform.querytext.disabled = false;
}

function state_change_info ()
{
	if (xmlhttp.readyState == 4) {
		// FIXME: Should also check for status 200

		//dump("Response:\n");
		//dump(xmlhttp.responseText);
		//dump("\n");

		res = xmlhttp.responseText;

		// if charset is x-user-defined split by \uF7FF
		// if charset is utf-8, split by FFFD
		// And dont ask me why!
		var responses = res.split ('\uFFFD'); 
		var parser = new DOMParser ();

//		Appending without clearing is bad... mmkay?
		document.getElementById ('results').innerHTML = '';
		document.getElementById ('topbar').style.display = 'none';

		// there should be only one response in responses
		for (var i = 0; i < responses.length; ++i) {
			if (responses [i].length <= 0)
				continue;
			dump (responses [i]);
			dump ('\n');

			var response_dom = parser.parseFromString (responses [i], "text/xml");
			var fragment = xsltProcessor.transformToFragment (response_dom, document);
			document.getElementById ('results').appendChild (fragment);
		}

		document.getElementById ('status').style.display = 'none';
	}

	document.queryform.querytext.disabled = false;
}

/* Start Fancy Stuff */
/* This part shows/hides results
 * TODO:
 * - "Remember" choices between searches
 * - Categories.. opinions?
 */

function toggle_hit (hit_toggle)
{
	if (hit_toggle.innerHTML == '[-]') {
		hit_toggle.innerHTML = '[+]';
		//this.<span class="Uri">.<div class="Title">.<br/>.<div class="Data">
		hit_toggle.parentNode.parentNode.nextSibling.nextSibling.style.display = 'none';

	} else {
		hit_toggle.innerHTML = '[-]';
		hit_toggle.parentNode.parentNode.nextSibling.nextSibling.style.display = 'block';
	}
}

// Initially, no specific category is being shown, all results are being shown.
var category_is_being_shown = false;

function show_all ()
{
	// Get all the category checkboxes
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	// Get all the result categories
	var result_categories = document.getElementById ('results').childNodes;
	// Show all results
	for (var i = 0; i < result_categories.length; ++i) {
		result_categories [i].style.display = 'block';
	}
	// Uncheck all the categories
	for (var i = 0; i < category_checkboxes.length; ++i) {
		category_checkboxes [i].checked = false;
	}
	category_is_being_shown = false;
}

function toggle_category (category)
{
	// Get all the result categories
	var result_categories = document.getElementById ('results').childNodes;
	// if checked right now
	if (category.checked) {
		// If none of the categories are being shown..
		if (category_is_being_shown == false) {
			// Hide all results except the one selected
			for (var i = 0; i < result_categories.length; ++i) {
				if (result_categories [i].id == category.name)
					continue;
				result_categories [i].style.display = 'none';
			}
		} else {
			// Show result corresponding to category
			document.getElementById (category.name).style.display = 'block';
		}
		category_is_being_shown = true;
	} else {
		// Hide result corresponding to category
		document.getElementById (category.name).style.display = 'none';
	}
}
