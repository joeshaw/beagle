<?xml version="1.0"?>
<!--
//
// Copyright (2007) Debajyoti Bera
// Copyright (2007) Nirbheek Chauhan
//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//
-->

<!DOCTYPE xsl:stylesheet [<!ENTITY nbsp "&#160;">]>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
<xsl:output method="html"/>

<!-- 
	Start Template HTML doc 
-->

<xsl:template match="/">
	<html>
		<xsl:call-template name="head"/>
		<xsl:call-template name="body"/>
	</html>
</xsl:template>

<xsl:template name="head">
	<head>
		<title><xsl:value-of select="/document/title"/></title>
		<link rel="stylesheet" href="default.css" type="text/css"/>
		<script src="default.js" type="text/javascript"></script>
		<link rel="icon" href="images/favicon.png" type="image/png"/>
	</head>
</xsl:template>

<xsl:template name="body">
	<body onload="document.queryform.querytext.focus ();">
		<div id="header">
			<xsl:call-template name="header"/>
		</div>
		<div id="status">
			<img src="images/busy-animation.gif"/>
		</div>
		<div id="topbar">
			<xsl:call-template name="topbar"/>
		</div>
		<!-- Placeholder div -->
		<div id="login">
		</div>
		<div id="results">
			<xsl:call-template name="results"/>
		</div>
		<div id="footer">
			<xsl:call-template name="footer"/>
		</div>
	</body>
</xsl:template>

<xsl:template name="header">
	<a href="."><img src="images/beagle-logo.png"/></a>
	<form name="queryform" onsubmit='search (); return false;' action="POST">
		<input name="querytext" type="text" size="50" />
		<input name="querysubmit" type="submit" value="Search"/>
	</form>
	<span id="headerlinks">
		<a href="" onclick='get_information (); return false;'>Current Status</a>&nbsp;|&nbsp;
		<a href="" onclick='get_process_information (); return false;'>Process Information</a>&nbsp;|&nbsp;
		<a href="" onclick='alert ("Not implemented"); return false;'>Beagle settings</a>
	</span>
</xsl:template>

<xsl:template name="topbar">
	<span id="topbar-left">
		<form name="categories" autocomplete="off">
			<a href="#" onclick='show_all (this); return false;' name="All">Show All</a>&nbsp;|&nbsp;
			<xsl:for-each select="document ('mappings.xml')/Mappings/Categories/Category/@Name">
				<input type="checkbox" name="{.}" onClick='toggle_category (this);'/><xsl:value-of select="."/>
			</xsl:for-each>
		</form>
	</span>
	<span id="topbar-right">
		<span id="numhits">0</span> results for "<span id="query_str" stemmed=""></span>" in <span id="timetaken">0 secs</span>
	</span>
</xsl:template>

<xsl:template name="results">
	<xsl:for-each select="document ('mappings.xml')/Mappings/Categories/Category/@Name">
		<div class="Hits" id='{.}'>
		</div>
	</xsl:for-each>
	<div class="Hits" id="NoResults" style="display: none;">
		No Results
	</div>
</xsl:template>

<xsl:template name="footer">
	<a href="http://beagle-project.org/Beagle_Webinterface">Web interface</a> for <a href="http://beagle-project.org">Beagle</a> desktop search service<br/>
	<p class='license'>Copyright &#xA9; 2007 Debajyoti Bera, Nirbheek Chauhan, Licensed under <a href="http://www.opensource.org/licenses/mit-license.php">MIT license</a></p>	
	<p style="color:silver">Version: <xsl:value-of select="/document/version" /> last updated on <xsl:value-of select="/document/last_time" /></p>
</xsl:template>

<!-- 
	End Template HTML doc 
-->

</xsl:stylesheet>
