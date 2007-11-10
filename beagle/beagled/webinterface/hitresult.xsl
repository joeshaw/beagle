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
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
<xsl:output method="html"/>

<xsl:template match="NumMatches">
	<p class="NumMatches">(Showing <xsl:value-of select="text()"/> matches)</p>
</xsl:template>

<xsl:template match="@Timestamp">
	Last modified: <b><xsl:value-of select="."/></b>
</xsl:template>

<xsl:template match="Hit">
	 <div class="Hit" id="{@Uri}" name="Hit">
		<div class="Title" name="Title">
			<span class="Uri" name="Uri">
				<a href="#" class="Toggle" onclick='toggle_hit(this); return false;'>[-]</a>&nbsp;
				<a target="_blank" href="{@Uri}">
					<xsl:call-template name="Uri"/>
				</a>
			</span>
			<span class="Timestamp" name="Timestamp">
				<xsl:value-of select="@Timestamp"/>
			</span>
		</div>
		<div class="Data" name="Data">
			<xsl:apply-templates select="Properties"/>
		</div>
		<div class="XML" name="XML">
			<xsl:copy-of select="."/> 
		</div>
	</div>
</xsl:template>

<!-- FIXME: This logic should go into mappings.xml and then be referenced from there. -->
<xsl:template name="Uri">
	<xsl:choose>
		<xsl:when test="Properties/Property[@Key='beagle:ExactFilename']">
			<xsl:value-of select="Properties/Property[@Key='beagle:ExactFilename']/@Value"/>
		</xsl:when>
		<xsl:when test="Properties/Property[@Key='dc:title']">
			<xsl:value-of select="Properties/Property[@Key='dc:title']/@Value"/>
		</xsl:when>
		<xsl:when test="Properties/Property[@Key='parent:dc:title']">
			<xsl:value-of select="Properties/Property[@Key='parent:dc:title']/@Value"/>
		</xsl:when>
		<xsl:when test="Properties/Property[@Key='beagle:HitType' and @Value='IMLog']">
			Conversation with&nbsp;
			<xsl:choose>
				<xsl:when test="Properties/Property[@Key='fixme:alias']">
					<xsl:value-of select="Properties/Property[@Key='fixme:speakingto_alias']/@Value"/>
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="Properties/Property[@Key='fixme:speakingto']/@Value"/>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:when>
		<xsl:otherwise>
			<xsl:value-of select="@Uri"/>
		</xsl:otherwise>
	</xsl:choose>
</xsl:template>

<!-- FIXME: This (currently non-exitant) mapping should go into mappings.xml and then be referenced from there. -->
<xsl:template match="Properties">
	<table class="Properties">
		<xsl:for-each select="Property">
			<tr>
			<td class="PropertyKey"><xsl:value-of select="@Key"/></td>
			<td class="PropertyValue"><xsl:value-of select="@Value"/></td>
			</tr>
		</xsl:for-each>
	</table>
</xsl:template>

</xsl:stylesheet>
