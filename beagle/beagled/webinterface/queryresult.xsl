<?xml version="1.0"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" />

<xsl:template match="/">
    <!--<html><body>-->
    <xsl:apply-templates select="ResponseWrapper"/>
    <!--</body></html>-->
</xsl:template>

<xsl:template match="ResponseWrapper">
    <xsl:apply-templates select="Message"/>
</xsl:template>

<xsl:template match="Message">
    <!--<xsl:apply-templates select="NumMatches"/>-->
    <xsl:apply-templates select="Hits"/>
    <!-- FIXME: Ignoring other types of messages -->
</xsl:template>

<xsl:template match="NumMatches">
    <p class="NumMatches">(Showing <xsl:value-of select="text()"/> matches)</p>
</xsl:template>

<xsl:template match="Hits">
	<xsl:for-each select="Hit">
	    <div class="Hit" id="{@Uri}"><a href="{@Uri}"><xsl:value-of select="@Uri"/></a><br/>
		<span class="Timestamp">Timestamp:<i><xsl:value-of select="@Timestamp"/></i><b>|</b></span>
		<span class="Type">Type:<i><xsl:value-of select="@Type"/></i><b>|</b></span>
		<span class="MimeType">Mimetype:<i><xsl:value-of select="@MimeType"/></i><b>|</b></span>
		<span class="Source">Source:<i><xsl:value-of select="@Source"/></i><b>|</b></span>
		<span class="Score">Score:<i><xsl:value-of select="@Score"/></i></span>
	    <xsl:apply-templates select="Properties"/>
	    </div><hr/>
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
