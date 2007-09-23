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

<xsl:template match="NumMatches">
    <p class="NumMatches">(Showing <xsl:value-of select="text()"/> matches)</p>
</xsl:template>

<xsl:template match="Hits">
	<xsl:for-each select="Hit">
	    <div class="Hit" id="{@Uri}">
		<div class="Uri"><a href="{@Uri}"><xsl:value-of select="@Uri"/></a></div><br/>
		<span class="Timestamp">Timestamp:<i><xsl:value-of select="@Timestamp"/></i><b>|</b></span>
		<span class="Score">Score:<i><xsl:value-of select="@Score"/></i></span>
	    <xsl:apply-templates select="Properties"/>
	    </div>
	</xsl:for-each>
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
