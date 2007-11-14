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
 * - Code needs to be made cleaner...
 */

/************ Code for 'Current Status' **************/

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

function state_change_info ()
{
	if (xmlhttp.readyState == 4) {
		// FIXME: Should also check for status 200
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

			var response_dom = parser.parseFromString (responses [i], "text/xml");
			var fragment = query_processor.transformToFragment (response_dom, document);
			document.getElementById ('info').appendChild (fragment);
		}

		document.getElementById ('status').style.display = 'none';
		document.queryform.querytext.disabled = false;
		document.queryform.querysubmit.disabled = false;
	}
}

function shutdown_beagle ()
{
	if ( ! window.confirm ("Are you sure you want to Shutdown Beagle?")) {
		return;
	}
	var req_string = '<?xml version="1.0" encoding="utf-8"?><RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Message xsi:type="ShutdownRequest"/></RequestWrapper>';

	xmlhttp.onreadystatechange = function () {
		var message_div = document.getElementById ('shutdown_beagle');
		if (xmlhttp.readyState == 4) {
			var message = document.createElement ('i');
			var text = document.createTextNode ('Shutdown request sent to beagle');
			message.appendChild (text);
			message_div.replaceChild (message, message_div.firstChild);
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

/******** Code to handle searching of properties names ******/

function search_property (search_property_node)
{
	// <tr><td><a>Search</a></td><td key="{@Key}">Key</td><td type="{@Type}">Value</td></tr>
	//
	var property_key  = search_property_node.parentNode.nextSibling.getAttribute ("key");
	var property_type = search_property_node.parentNode.nextSibling.nextSibling.getAttribute ("type");
	var property_val  = search_property_node.parentNode.nextSibling.nextSibling.textContent;
	dump ("type = " + property_type + " key = " + property_key + " val = " + property_val + "\n");

	var prefix = "";
	if (property_type == "Text")
		prefix = "property:";
	else if (property_type == "Keyword")
		prefix = "keyword:";
	else
		return;

	document.queryform.querytext.value = (prefix + '"' + property_key + '=' + property_val + '"');
	search ();
}

/*************** Main code to handle search ****************/

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

	var req_string = '<?xml version="1.0" encoding="utf-8"?> <RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"> <Message xsi:type="Query"> <IsIndexListener>false</IsIndexListener> <Parts> <Part xsi:type="QueryPart_Human"> <Logic>Required</Logic> <QueryString>' + query_str + '</QueryString> </Part> </Parts> <QueryDomain>Local System</QueryDomain> <MaxHits>100</MaxHits> </Message> </RequestWrapper> ';

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
		reset_document_style ();
		reset_document_content ();
		document.getElementById ('timetaken').textContent = elapsed + ' secs';
		var num_matches = 0;

		//var _start = Date.now ();

		// Process hit xml nodes with xsl and append with javascript
		for (var i = 1; i < responses.length; ++i) {
			if (responses [i].length <= 0)  {
				continue;
			}
			var response_dom = parser.parseFromString (responses [i], "text/xml");
			// FIXME: ignoring all other messages
			var hits = response_dom.getElementsByTagName ('Hit');
			for (var j = 0; j < hits.length; ++j) {
				// Get timestamp and process it
				var timestamp = (hits [j]).getAttribute ('Timestamp');
				(hits [j]).setAttribute ('Timestamp', humanise_timestamp (timestamp));

				var div_id = process_hit (hits [j]);
				var div = document.getElementById (div_id);
				// Process Hit using hitresult.xsl and append to `div`
				var hit = hit_processor.transformToFragment (hits [j], document);
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

		//alert (Date.now () - _start);

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

/************ Hit categorization ****************/

// The following logic is taken mappings.xml;
// ideally, these functions should be auto-generated
// from mappings.xml

function is_document (properties)
{
	var hit_type = properties ['beagle:HitType'];
	if (hit_type == null)
		hit_type = '';

	var file_type = properties ['beagle:FileType'];
	if (file_type == null)
		file_type = '';

	return (hit_type != 'MailMessage' &&
		hit_type != 'WebHistory' && (
			file_type == 'document' ||
			file_type == 'archive' ||
			file_type == 'source' ||
			hit_type == 'note'));
}

function is_image (properties)
{
	var file_type = properties ['beagle:FileType'];
	if (file_type == null)
		file_type = '';

	return (file_type == 'image');
}

function is_media (properties)
{
	var file_type = properties ['beagle:FileType'];
	if (file_type == null)
		file_type = '';

	return (file_type == 'audio' ||
		file_type == 'video');
}

function is_mail (properties)
{
	var hit_type = properties ['beagle:HitType'];
	if (hit_type == null)
		hit_type = '';

	return (hit_type == 'MailMessage');
}

function is_imlog (properties)
{
	var hit_type = properties ['beagle:HitType'];
	if (hit_type == null)
		hit_type = '';

	return (hit_type == 'IMLog');
}

function is_website (properties)
{
	var hit_type = properties ['beagle:HitType'];
	if (hit_type == null)
		hit_type = '';

	return (hit_type == 'WebHistory' ||
		hit_type == 'Bookmark' ||
		hit_type == 'FeedItem');
}

function is_other (properties)
{
	var file_type = properties ['beagle:FileType'];
	if (file_type == null)
		file_type = '';

	return (file_type == 'directory');
}

// The order is important
var category_funcs = new Array (
	{'name': 'Mail'	    , 'func': is_mail	    },
	{'name': 'Documents', 'func': is_document   },
	{'name': 'Images'   , 'func': is_image	    },
	{'name': 'Media'    , 'func': is_media	    },
	{'name': 'IM Logs'  , 'func': is_imlog	    },
	{'name': 'Websites' , 'func': is_website    },
	{'name': 'Others'   , 'func': is_other	    }
);

function classify_hit (properties)
{
	for (var i = 0; i < category_funcs.length; ++ i) {
		if ((category_funcs [i]).func (properties))
			return (category_funcs [i]).name;
	}

	return "Others";
}

/* Processes the hit and return the category */
function process_hit (hit)
{
	var properties = hit.getElementsByTagName ('Property');
	var property_table = {};

	for (var k = 0; k < properties.length; ++k) {
		var key = properties [k].getAttribute ('Key');
		var value = properties [k].getAttribute ('Value');

		property_table [key] = value;

		// Change the property names to more human friendly ones
		var property_name = PropertyNameTable [key];
		// Search for parent properties
		if (property_name == null && (key.indexOf ('parent:') == 0))
			property_name = PropertyNameTable [key.slice (7)];
		if (property_name == null)
			property_name = key;
		properties [k].setAttribute ('Name', property_name);

		// Change the value of date properties
		// Date properties are never used in mappings.xml
		if (properties [k].getAttribute ('Type') == 'Date')
			properties [k].setAttribute ('Value', humanise_timestamp (value));
	}

	return classify_hit (property_table);
}

/************* Datetime parsing routines ***************/

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

/**************** Code to handle styles and ui issues ******************/

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
	var div, div_category_name;
	// Reset the divs
	document.getElementById ('info').innerHTML = '';
	document.getElementById ('help').innerHTML = '';
	results.innerHTML = '';
	for (var i = 0; i < categories.length; ++i) {
		div = document.createElement ('div');
		div.setAttribute ('class', 'Hits');
		div.setAttribute ('id', categories [i].name);
		div_category_name = document.createElement ('div');
		// Not making it class="Hit" because it results in too much padding
		div_category_name.innerHTML = '<h3>'+categories [i].name+'</h3>';
		div.appendChild (div_category_name);
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
			if (results_categories [i].childNodes.length > 1)
				results_categories [i].style.display = 'block';
		}
	}
}

function toggle_hit (hit_toggle)
{
	if (hit_toggle.textContent == '[-]') {
		hit_toggle.textContent = '[+]';
		//this.<span class="Uri">.<div class="Title">.<div class="Data">
		hit_toggle.parentNode.parentNode.nextSibling.style.display = 'none';

	} else {
		hit_toggle.textContent = '[-]';
		hit_toggle.parentNode.parentNode.nextSibling.style.display = 'block';
	}
}

function show_all_categories ()
{
	// Get all the category checkboxes
	var category_checkboxes = document.getElementById ('topbar-left').getElementsByTagName ('input');
	// Get all the result categories
	var results_categories = document.getElementById ('results').childNodes;
	// Uncheck all the categories
	for (var i = 0; i < category_checkboxes.length; ++i) {
		category_checkboxes [i].checked = false;
	}
	// Show results
	// This should've been used here instead of that loop from the start..
	set_results_style ();
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
