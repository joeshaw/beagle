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

<!-- FileType: document,directory,mail,image,audio,video,archive,package -->
<!-- 
<xsl:template match="Hits">
	<xsl:call-template name="DocumentHits"/>
	<xsl:call-template name="ImageHits"/>
	<xsl:call-template name="MediaHits"/>
	<xsl:call-template name="MailHits"/>
	<xsl:call-template name="IMLogHits"/>
	<xsl:call-template name="WebsiteHits"/>
	<xsl:call-template name="OtherHits"/>
</xsl:template>
-->

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
				<a href="#" class="Toggle" onclick='toggle_hit(this); return false;'>[-]</a>
				<a href="{@Uri}">
					<xsl:call-template name="Uri"/>
				</a>
			</span>
			<span class="Timestamp" name="Timestamp">
				<xsl:value-of select="@Timestamp"/>
			</span>
		</div><br/>
		<div class="Data" name="Data">
			<xsl:apply-templates select="Properties"/>
		</div>
		<div class="XML" name="XML">
			<xsl:copy-of select="."/> 
		</div>
	</div>
</xsl:template>

<!-- FIXME: This logic should go into mapping.xml and then be referenced from there. -->
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

<!-- FIXME: This (currently non-exitant) mappin should go into mapping.xml and then be referenced from there. -->
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

<!-- 
Includes:
 - FileType = {document,archive,source}
 - HitType != {WebHistory,MailMessage}
-->
<!--
<xsl:template name="DocumentHits">
	<div id="Documents">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and (@Value = 'document' or @Value = 'archive' or @Value = 'source')] and Properties/Property[@Key = 'beagle:HitType' and (@Value != 'WebHistory' and @Value != 'MailMessage')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="ImageHits">
	<div id="Images">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and @Value = 'image']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="MailHits">
	<div id="Mail">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:HitType' and @Value = 'MailMessage']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="MediaHits">
	<div id="Media">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and (@Value = 'audio' or @Value = 'video')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="IMLogHits">
	<div id="IMLogs">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:HitType' and @Value = 'IMLog']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="WebsiteHits">
	<div id="Websites">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:HitType' and (@Value = 'WebHistory' or @Value = 'Bookmark' or @Value = 'FeedItem')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<xsl:template name="OtherHits">
	<div id="Others">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and (@Value = 'directory')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>
-->

</xsl:stylesheet>
