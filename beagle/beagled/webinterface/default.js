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

function search() {
    // get the search string
    var query_str=document.queryform.querytext.value;
    // FIXME: Escape query_str
    if (query_str.length == 0)
	return;

    var req_string = '<?xml version="1.0" encoding="utf-8"?> <RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"> <Message xsi:type="Query"> <IsIndexListener>false</IsIndexListener> <Parts> <Part xsi:type="QueryPart_Human"> <Logic>Required</Logic> <QueryString>' + query_str + '</QueryString> </Part> </Parts> <MimeTypes /> <HitTypes /> <Sources /> <QueryDomain>Local System</QueryDomain> <MaxHits>100</MaxHits> </Message> </RequestWrapper> ';

    var begin_date = Date.now();

    xmlhttp.onreadystatechange = function () {state_change_search (begin_date);};
    xmlhttp.open("POST", "/", true);
    //XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
    xmlhttp.overrideMimeType('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
    //xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
    xmlhttp.send(req_string);

    document.queryform.querytext.disabled=true;
    document.getElementById('status').style.display='block';
    return false;
}

function get_information() {
    var req_string = '<?xml version="1.0" encoding="utf-8"?><RequestWrapper xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema"><Message xsi:type="DaemonInformationRequest"> <GetVersion>true</GetVersion><GetSchedInfo>true</GetSchedInfo><GetIndexStatus>true</GetIndexStatus> <GetIsIndexing>true</GetIsIndexing></Message></RequestWrapper>';

    xmlhttp.onreadystatechange = state_change_info;
    xmlhttp.open("POST", "/", true);
    //XHR binary charset opt by mgran 2006 [http://mgran.blogspot.com]
    xmlhttp.overrideMimeType('text/txt; charset=utf-8'); // if charset is changed, need to handle bom
    //xmlhttp.overrideMimeType('text/txt; charset=x-user-defined');
    xmlhttp.send(req_string);

    document.queryform.querytext.disabled=true;
    document.getElementById('status').style.display='block';
    return false;
}

function onError(e)
{
    alert("Error " + e.target.status + " occurred while receiving the document.");
}

var ii=0

function state_change_search(begin_date)
{
    //alert("help");
    if (xmlhttp.readyState==4) {
	// FIXME: Should also check for status 200

	var end_date = Date.now();
	var elapsed = (end_date - begin_date)/1000;

	//dump("Response:\n");
	//dump(xmlhttp.responseText);
	//dump("\n");
	res = xmlhttp.responseText;

	//alert(res.charCodeAt (0));
	//alert(res.charCodeAt (1));
	//alert(res.charCodeAt (2));
	//var debug_index = res.indexOf ('</ResponseWrapper>');
	//alert(res.charCodeAt(debug_index + 17));
	//alert(res.charCodeAt(debug_index + 18));
	//alert(res.charCodeAt(debug_index + 19));

	// if charset is x-user-defined split by \uF7FF
	// if charset is utf-8, split by FFFD
	// And dont ask me why!
	var responses = res.split('\uFFFD'); 
	var parser = new DOMParser();

//	Appending without clearing is bad... mmkay?
//	document.getElementById('status').innerHTML='';
	document.getElementById('results').innerHTML='';
	document.getElementById('timetaken').innerHTML=(elapsed + ' seconds');
	document.getElementById('numhits').innerHTML=0;
	var num_matches = 0;

	for (var i=0; i<responses.length; ++i) {
	    if (responses[i].length <= 0)
		continue;
	    dump(responses[i]);
	    dump('\n');

	    //document.getElementById('xml').innerHTML += (responses[i].length + "</br>");
	    var response_dom = parser.parseFromString(responses[i], "text/xml");

	    var num_matches_elems = response_dom.getElementsByTagName('NumMatches');
	    if (num_matches_elems.length > 0) {
		var n = parseInt (num_matches_elems[0].textContent);
		if (n > 0) {
		    num_matches += n;
		    document.getElementById('numhits').innerHTML=num_matches;
		}
	    }
	    //var hits = response_dom.getElementsByTagName('Hit');
	    //if (hits.length > 0) {
	    //    document.getElementById('results').innerHTML = ("<i>" + hits[0].getAttribute('Timestamp') + "</i>");
	    //    document.getElementById('xml').innerHTML = ("<i>" + hits[1].getAttribute('Timestamp') + "</i>");
	    //}
	    var fragment = xsltProcessor.transformToFragment(response_dom, document);
	    document.getElementById('results').appendChild(fragment);
	}

	document.getElementById('topbar-right').style.display='block';
	document.getElementById('status').style.display='none';
    }

    document.queryform.querytext.disabled=false;
}

function state_change_info()
{
    //alert("help");
    if (xmlhttp.readyState==4) {
	// FIXME: Should also check for status 200

	//dump("Response:\n");
	//dump(xmlhttp.responseText);
	//dump("\n");

	res = xmlhttp.responseText;

	// if charset is x-user-defined split by \uF7FF
	// if charset is utf-8, split by FFFD
	// And dont ask me why!
	var responses = res.split('\uFFFD'); 
	var parser = new DOMParser();

//	Appending without clearing is bad... mmkay?
	document.getElementById('results').innerHTML='';
	document.getElementById('topbar-right').style.display='none';

	// there should be only one response in responses
	for (var i=0; i<responses.length; ++i) {
	    if (responses[i].length <= 0)
		continue;
	    dump(responses[i]);
	    dump('\n');

	    var response_dom = parser.parseFromString(responses[i], "text/xml");
	    var fragment = xsltProcessor.transformToFragment(response_dom, document);
	    document.getElementById('results').appendChild(fragment);
	}

	document.getElementById('status').style.display='none';
    }

    document.queryform.querytext.disabled=false;
}


