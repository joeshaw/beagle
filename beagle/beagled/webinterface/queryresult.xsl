<?xml version="1.0"?>
<!DOCTYPE xsl:stylesheet [<!ENTITY nbsp "&#160;">]>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" >
<xsl:output method="html" />

<xsl:template match="/">
	<!--<html><body>-->
	<xsl:apply-templates select="ResponseWrapper"/>
	<!--</body></html>-->
</xsl:template>

<xsl:template match="ResponseWrapper">
	<xsl:apply-templates select="Message[@xsi:type = 'HitsAddedResponse']"/>
	<xsl:apply-templates select="Message[@xsi:type = 'DaemonInformationResponse']"/>
</xsl:template>

<xsl:template match="Message[@xsi:type = 'HitsAddedResponse']">
	<!--<xsl:apply-templates select="NumMatches"/>-->
	<xsl:apply-templates select="Hits"/>
	<!-- FIXME: Ignoring other types of messages -->
</xsl:template>

<xsl:template match="Message[@xsi:type = 'DaemonInformationResponse']">
	<div id="version"><b>Version</b>: <i><xsl:value-of select="Version"/></i></div>
	<div id="is_indexing"><b>Indexing in progress</b>: <i><xsl:value-of select="IsIndexing"/></i></div>
	<xsl:apply-templates select="SchedulerInformation"/>
	<xsl:apply-templates select="IndexStatus"/>
</xsl:template>

<xsl:template match="SchedulerInformation">
	<div class="scheduler_information"><b>Tasks:</b><br/>
		<span class="total_task_count">(<xsl:value-of select="@TotalTaskCount"/> tasks submitted)</span>
		<ul>
		<xsl:for-each select="PendingTasks/PendingTask">
			<li><i>(Pending)</i>&nbsp;<xsl:value-of select="."/></li>
		</xsl:for-each>

		<xsl:for-each select="FutureTasks/FutureTask">
			<li><i>(Future)</i>&nbsp;<xsl:value-of select="."/></li>
		</xsl:for-each>

		<xsl:for-each select="BlockedTasks/BlockedTask">
			<li><i>(Blocked)</i>&nbsp;<xsl:value-of select="."/></li>
		</xsl:for-each>
		</ul>
	</div>
</xsl:template>

<xsl:template match="IndexStatus">
	<b>Details of backends:</b><br/>
	<ul class="indexstatus">
		<xsl:for-each select="QueryableStatus">
			<li><div class="queryablestatus">
			<span class="queryablestatus_name"><xsl:value-of select="@Name"/></span>&nbsp;
			<span class="queryablestatus_progresspercent">(<xsl:value-of select="@ProgressPercent"/> %)</span>:
			<span class="queryablestatus_itemcount"><xsl:value-of select="@ItemCount"/> items,</span>
			<span class="queryablestatus_isindexing">currently indexing? <xsl:value-of select="@IsIndexing"/></span>,
			</div></li>
		</xsl:for-each>
	</ul>
</xsl:template>

<!-- FileType: document,directory,mail,image,audio,video,archive,package -->
<xsl:template match="Hits">
	<xsl:call-template name="DocumentHits"/>
	<xsl:call-template name="ImageHits"/>
	<xsl:call-template name="MediaHits"/>
	<xsl:call-template name="MailHits"/>
	<xsl:call-template name="IMLogHits"/>
	<xsl:call-template name="WebsiteHits"/>
	<xsl:call-template name="OtherHits"/>
	<!--
	<xsl:call-template name="DocumentHits"/>
	-->
</xsl:template>


<xsl:template match="NumMatches">
	<p class="NumMatches">(Showing <xsl:value-of select="text()"/> matches)</p>
</xsl:template>

<xsl:template match="@Timestamp">
	<span class="Timestamp">
		Last modified: <b><xsl:value-of select="../@Timestamp"/></b>
	</span>
</xsl:template>

<xsl:template match="Hit">
	 <div class="Hit" id="{@Uri}">
		<div class="Title">
			<span class="Uri">
				<a href="#" class="Toggle" onclick='toggle_hit(this); return false;'>[-]</a>
				<a href="{@Uri}">
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
							<xsl:value-of select="Properties/Property[@Key='fixme:alias']/@Value"/>
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
				</a>
			</span>
			<xsl:apply-templates select="@Timestamp"/>
		</div><br/>
		<div class="Data">
			<xsl:apply-templates select="Properties"/>
		</div>
	</div>
</xsl:template>

<xsl:template match="NumMatches">
	<p class="NumMatches">(Showing <xsl:value-of select="text()"/> matches)</p>
</xsl:template>

<xsl:template match="@Timestamp">
	<span class="Timestamp">
		Last modified: <b><xsl:value-of select="../@Timestamp"/></b>
	</span>
</xsl:template>

<!-- 
Includes:
 - FileType = {document,archive,source}
 - HitType != {WebHistory,MailMessage}
-->
<xsl:template name="DocumentHits">
	<div id="Documents">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and (@Value = 'document' or @Value = 'archive' or @Value = 'source')] and Properties/Property[@Key = 'beagle:HitType' and (@Value != 'WebHistory' and @Value != 'MailMessage')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

<!-- Merged into 'DocumentHits' -->
<!--
<xsl:template name="FolderHits">
	<div id="Folders">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and @Value = 'directory']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>
-->

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

<!-- Merged into 'MediaHits' -->
<!--
<xsl:template name="VideoHits">
	<div id="Video">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and @Value = 'video']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>
-->

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

<!-- Merged into 'DocumentHits' -->
<!--
<xsl:template name="SourceHits">
	<div id="Source">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and @Value = 'source']]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>
-->

<!-- Everything else goes here. So, fill 'er up :) -->
<xsl:template name="OtherHits">
	<div id="Others">
		<xsl:for-each select="Hit[Properties/Property[@Key = 'beagle:FileType' and (@Value = 'directory')]]">
			<xsl:apply-templates select="."/>
		</xsl:for-each>
	</div>
</xsl:template>

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
