using System;
using System.IO;
using System.Text;

/***
 * <copyright>
 *   ICalParser is a general purpose .Net parser for iCalendar format files (RFC 2445)
 * 
 *   Copyright (C) 2004  J. Tim Spurway
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program; if not, write to the Free Software
 *   Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
 * </copyright>
 */

namespace Semaview.Shared.ICalParser
{
    /// <summary>
    /// Summary description for ICalUtil.
    /// </summary>
    public class ICalUtil
    {
	private ICalUtil()
	{
	}	

	/**
	 * From RFC2445
	 * 
	 * Lines of text SHOULD NOT be longer than 75 octets, excluding the line
	 * break. Long content lines SHOULD be split into a multiple line
	 * representations using a line "folding" technique. That is, a long
	 * line can be split between any two characters by inserting a CRLF
	 * immediately followed by a single linear white space character (i.e.,
	 * SPACE, US-ASCII decimal 32 or HTAB, US-ASCII decimal 9). Any sequence
	 * of CRLF followed immediately by a single linear white space character
	 * is ignored (i.e., removed) when processing the content type.
	 * 
	 * For example the line:
	 * 
	 *   DESCRIPTION:This is a long description that exists on a long line.
	 * 
	 * Can be represented as:
	 * 
	 *   DESCRIPTION:This is a lo
	 *    ng description
	 *    that exists on a long line.
	 * 
	 **/
	public static string FoldLines( StringBuilder iCal )
	{
	    StringReader  reader = new StringReader( iCal.ToString() );
	    StringBuilder folded = new StringBuilder();
	    string line = reader.ReadLine();
	    while( line != null )
	    {
		int start = 0;
		int len   = line.Length;

		// I chose 72 instead of the max 75 cause I wanted to, so :P
		while( len > 72 )
		{
		    // Indent if not first line
		    if( start != 0 )
			folded.Append( " " );
		    folded.AppendFormat( "{0}\r\n", line.Substring( start, 72 ) );
		    start += 72;
		    len   -= 72;
		}
		// Indent if not first line
		if( start != 0 )
		    folded.Append( " " );
		folded.AppendFormat( "{0}\r\n", line.Substring( start, len ) );
		line = reader.ReadLine();
	    }
	    return folded.ToString();
	}

	/// <summary>
	/// Quotes newlines, tabs, commas, colons, semi-colons, double quotes and back slashes
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	public static string Quote( string str )
	{
	    string quoted = str.Replace( "\\", "\\\\" );
	    quoted = quoted.Replace( "\r\n", "\\n" );
	    quoted = quoted.Replace( "\n", "\\n" );
	    quoted = quoted.Replace( "\t", "\\t" );
	    quoted = quoted.Replace( ",", "\\," );
	    quoted = quoted.Replace( ":", "\\:" );
	    quoted = quoted.Replace( ";", "\\;" );
	    quoted = quoted.Replace( "\"", "\\\"" );
	    return quoted;
	}

	public static string Unquote( string str )
	{
	    string unquoted = str.Replace( "\\r\\n", "\r\n" );
	    unquoted = unquoted.Replace( "\\n", "\r\n" );
	    unquoted = unquoted.Replace( "\\t", "\t" );
	    unquoted = unquoted.Replace( "\\,", "," );
	    unquoted = unquoted.Replace( "\\:", ":" );
	    unquoted = unquoted.Replace( "\\;", ";" );
	    unquoted = unquoted.Replace( "\\\"", "\"" );
	    unquoted = unquoted.Replace( "\\\\", "\\" );
	    return unquoted.ToString();
	}
    }
}
