using System;
using System.Collections;
using System.Text.RegularExpressions;
using System.Text;
using System.Web;

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

    // TODO:  remove the reserved keywords from the Token Base Class - this will allow
    // generic parsing/scanning.

    // note that order is important in this enum type simple tokens are '<= Equals' and 
    // reserved words are '>= Tcalscale' - furthermore the reserved words are broken down into
    // a set of ranges that indicate similar 'types' of tokens (ie. the parser will treat the
    // objects in the range similarily) - this means that if new values are inserted into the
    // ranges, they should occur between the two tokens on each end.  For example to add a new
    // keyword to the symbolic property range, it must be lexically after 'Tcalscale' and lexically
    // before 'Tcutype', otherwise it will not be recognized by the 'isSymbolicProperty()' method.
    //
    // note that there is convenience methods on the token class for these classifications
    // (ie. isResourceProperty() isMailtoProperty() etc)
    public enum TokenValue 
    {
	SemiColon=0, Colon=1, Comma=2, Hyphen=3, CRLF=4, Equals=5, // simple tokens
	QuotedString=6, Value=7, Xtension=8, ID=11, Parm=12, Error=13,  // general tokens
	// reserved words	
	Tcalscale, Taction, Tclass, Ttransp, Tstart, Tpartstat, Trsvp, Trole, Tcutype,  // this range is the Symbolic Properties
	Tstandard, Tdaylight, Tvalarm, Ttrigger, // this range is the resource properties
	Tattendee, Torganizer, // this range is the 'mailto:' type properties
	Tbegin, Tend, Tvalue, TrecurrenceId,
	Tvcalendar, // this range is the BEGINEND keyword properties (up to Tvtimezone)
	    Tvevent, Tvtodo, Tvjournal, Tvfreebusy, Tvtimezone,  // these are a subproperty of BEGINEND which are COMPONENTs
	Tdtstart, Tdtstamp, Tdtend, Trrule, Texdate, //  this range are the ValueProperties
    };

    /// <summary>
    /// Represents the individual tokens returned from the scanner to the parser.  Note that the
    /// Token creation process is sensitive to the ScannerState.  This state is defined by what context
    /// the scanner currently is in - Parsing IDs, Parmeters, or values:
    ///   e.g.  the iCalendar grammar defines the following possible states
    ///   id;id=parm:value
    /// each string parsed out of the value has to be treated differently (eg. quoted strings are
    /// allowed in 'parm' but not in 'id')
    /// 
    /// </summary>
    public class Token
    {
	private string tokenText;
	private TokenValue tokenVal;
	private ScannerState state;
	private string errorMessage;

	private static Hashtable reservedWords;

	static Token()
	{
	    // static initialization for reserved words - for quick parsing
	    reservedWords = new Hashtable( );
	    //reservedWords[ "calscale" ] = TokenValue.Tcalscale;
	    reservedWords[ "action" ] = TokenValue.Taction;
	    reservedWords[ "class" ] = TokenValue.Tclass;
	    reservedWords[ "transp" ] = TokenValue.Ttransp;
	    reservedWords[ "start" ] = TokenValue.Tstart;
	    reservedWords[ "partstat" ] = TokenValue.Tpartstat;
	    reservedWords[ "rsvp" ] = TokenValue.Trsvp;
	    reservedWords[ "role" ] = TokenValue.Trole;
	    reservedWords[ "cutype" ] = TokenValue.Tcutype;
	    reservedWords[ "standard" ] = TokenValue.Tstandard;
	    reservedWords[ "daylight" ] = TokenValue.Tdaylight;
	    reservedWords[ "valarm" ] = TokenValue.Tvalarm;
	    reservedWords[ "trigger" ] = TokenValue.Ttrigger;
	    reservedWords[ "attendee" ] = TokenValue.Tattendee;
	    reservedWords[ "organizer" ] = TokenValue.Torganizer;
	    reservedWords[ "begin" ] = TokenValue.Tbegin;
	    reservedWords[ "end" ] = TokenValue.Tend;
	    reservedWords[ "vevent" ] = TokenValue.Tvevent;
	    reservedWords[ "vtodo" ] = TokenValue.Tvtodo;
	    reservedWords[ "vjournal" ] = TokenValue.Tvjournal;
	    reservedWords[ "vfreebusy" ] = TokenValue.Tvfreebusy;
	    reservedWords[ "vtimezone" ] = TokenValue.Tvtimezone;
	    reservedWords[ "vcalendar" ] = TokenValue.Tvcalendar;
	    reservedWords[ "dtstart" ] = TokenValue.Tdtstart;
	    reservedWords[ "dtend" ] = TokenValue.Tdtend;
	    reservedWords[ "rrule" ] = TokenValue.Trrule;
	    reservedWords[ "exdate" ] = TokenValue.Texdate;
	    reservedWords[ "value" ] = TokenValue.Tvalue;
	    reservedWords[ "dtstamp" ] = TokenValue.Tdtstamp;
	    reservedWords[ "recurrence-id" ] = TokenValue.TrecurrenceId;
	}

	public static bool isID( string str )
	{
	    return Regex.IsMatch( str, @"[a-zA-Z][a-zA-Z0-9_]*");
	}

	public static string CapsCamelCase( string str )
	{
	    if( str.Length > 0 )
	    {
		return CamelCase( str.Substring( 0, 1 ).ToUpper() + str.Substring( 1 ));
	    }
	    else
	    {
		return CamelCase( str );
	    }
	}

	public static string CamelCase( string str )
	{
	    bool upper = false;
	    char[] lstr = str.ToCharArray();
	    StringBuilder buff = new StringBuilder();

	    for( int i = 0; i < lstr.Length; ++i )
	    {
		if( lstr[ i ] == '-' )
		{
		    upper = true;
		}
		else
		{
		    if( upper )
		    {
			buff.Append( Char.ToUpper( lstr[ i ] ));
			upper = false;
		    }
		    else
		    {
			buff.Append( lstr[ i ] );
		    }
		}
	    }
	    return buff.ToString();
	}

	public static string ParseDateTime( string icalDate )
	{
	    try
	    {
		if( icalDate.Length >= 15 )
		{
		    string rval = icalDate.Substring( 0, 4 ) + '-' 
			+ icalDate.Substring( 4, 2 ) + '-'
			+ icalDate.Substring( 6, 2 ) + 'T'
			+ icalDate.Substring( 9, 2 ) + ':'
			+ icalDate.Substring( 11, 2 ) + ':'
			+ icalDate.Substring( 13, 2 );
		    if( icalDate.EndsWith( "Z" ))
			rval += "Z";
		    return rval;
		}
		return ParseDate( icalDate );
	    }
	    catch( Exception )
	    {
		return ParseDate( icalDate );  // if we can't convert it, try just the date format
	    }
	}

	public static string ParseDate( string icalDate )
	{
	    try
	    {
		string rval = icalDate.Substring( 0, 4 ) + '-' 
		    + icalDate.Substring( 4, 2 ) + '-'
		    + icalDate.Substring( 6, 2 ) + "T00:00:00";
		if( icalDate.EndsWith( "Z" ))
		    rval += "Z";
		return rval;
	    }
	    catch( Exception )
	    {
		//return icalDate;  // if we can't convert it - return maxdate string...
		string rval = String.Format( "{0:s}", DateTime.MaxValue );
		rval.Replace( " ", "T" );
		return rval;
	    }
	}

	public Token( string _tokenText, ScannerState _state ) : this( _tokenText, _state, false )
	{
	}

	public Token( string _tokenText ) : this( _tokenText, ScannerState.ParseValue, false )
	{
	}

	public Token( string _tokenText, ScannerState _state, bool quoteFlag )
	{
	    state = _state;

	    if( _tokenText == null )//|| _tokenText.Length == 0 )
	    {
		//tokenVal = TokenValue.Error;
		//errorMessage = "Bad Token String - 0 length";
		_tokenText = "";
	    }
	    //else
	    //{
		switch( state )
		{
		    case ScannerState.ParseID:
			tokenText = _tokenText.ToLower();
			if( reservedWords.Contains( tokenText ))
			{
			    tokenVal = (TokenValue) reservedWords[ tokenText ];
			}
			else if( tokenText.StartsWith( "x-" ))
			{
			    tokenVal = TokenValue.Xtension;
			    tokenText = "x:" + tokenText.Substring( 2 );
			}
			else if( isID( tokenText ))  // this check may be unnecessary by virute of the scanner....
			{
			    tokenVal = TokenValue.ID;
			}
			else
			{
			    tokenVal = TokenValue.Error;
			    errorMessage = "Illegal value for ID";
			}
			if( isBeginEndValue() )
			{
			    tokenText = CapsCamelCase( tokenText );
			}
			else
			{
			    tokenText = CamelCase( tokenText );
			}
			break;

		    case ScannerState.ParseParms:
			tokenText = HttpUtility.HtmlEncode( _tokenText );
			if( quoteFlag )
			{
			    tokenVal = TokenValue.QuotedString;
			}
			else
			{
			    tokenVal = TokenValue.Parm;
			}
			break;

		    case ScannerState.ParseValue:
			tokenText = HttpUtility.HtmlEncode( _tokenText );
			tokenVal = TokenValue.Value;
			break;

		    case ScannerState.ParseSimple:
			tokenVal = TokenValue.Error;
			errorMessage = "Bad constructor call - ParseSimple and text...";
			break;
		}
	    //}
	}

	public Token( TokenValue _tokenVal )
	{
	    tokenText = null;
	    if( _tokenVal <= TokenValue.Equals )
	    {
		tokenVal = _tokenVal;
	    }
	    else
	    {
		tokenVal = TokenValue.Error;
	    }
	}

	public bool isError( )
	{
	    return tokenVal == TokenValue.Error;
	}

	public bool isSymbolicProperty( )
	{
	    return tokenVal >= TokenValue.Tcalscale && tokenVal <= TokenValue.Tcutype;
	}

	public bool isResourceProperty( )
	{
	    return tokenVal >= TokenValue.Tstandard && tokenVal <= TokenValue.Ttrigger;
	}

	public bool isMailtoProperty( )
	{
	    return tokenVal >= TokenValue.Tattendee && tokenVal <= TokenValue.Torganizer;
	}

	public bool isBeginEndValue( )
	{
	    return (tokenVal >= TokenValue.Tvcalendar && tokenVal <= TokenValue.Tvtimezone) || tokenVal == TokenValue.Tvalarm;
	}

	public bool isComponent( )
	{
	    return tokenVal >= TokenValue.Tvevent && tokenVal <= TokenValue.Tvtimezone;
	}

	public bool isValueProperty( )
	{
	    return (tokenVal >= TokenValue.Tdtstart && tokenVal <= TokenValue.Texdate) || tokenVal == TokenValue.Ttrigger;
	}

	public TokenValue TokenVal 
	{
	    get { return tokenVal; }
	}

	public string TokenText
	{
	    get { return tokenText; }
	}

	public string Error
	{
	    get{ return errorMessage; }
	}

	public void FormatDateTime( )
	{
	    tokenText = ParseDateTime( tokenText );
	}
    }

    #region csUnit Tests
#if DEBUG
    namespace Test
    {
	using csUnit;

	public class TokenTest
	{
	    public TokenTest( )
	    {
	    }

	    public void testToken( )
	    {
		Token t1 = new Token( "testing" );
		Token t2 = new Token( TokenValue.Hyphen );
		Token t3 = new Token( "x-vendor-specific-tag", ScannerState.ParseID );
		Assert.True( t1.TokenText == "testing" && t1.TokenVal == TokenValue.Value && !t1.isError());
		Assert.True( t2.TokenText == null && t2.TokenVal == TokenValue.Hyphen && !t2.isError());
		Assert.True( t3.TokenText == "x:vendorSpecificTag" && t3.TokenVal == TokenValue.Xtension && !t3.isError() );
	    }

	    public void testParseID( )
	    {
		Token t1 = new Token( "BeGin", ScannerState.ParseID );
		Token t2 = new Token( "a123-009", ScannerState.ParseID );
		Token t3 = new Token( "x123", ScannerState.ParseID );
		Token t4 = new Token( "123", ScannerState.ParseID );
		Assert.True( t1.TokenVal == TokenValue.Tbegin && !t1.isError());
		Assert.True( t2.TokenText == "a123009" && t2.TokenVal == TokenValue.ID && !t2.isError());
		Assert.True( t3.TokenText == "x123" && t3.TokenVal == TokenValue.ID && !t3.isError());
		Assert.True( t4.isError() );
	    }

	    public void testReservedWords( )
	    {
		Token t1 = new Token( "class", ScannerState.ParseID );
		Token t2 = new Token( "AttendeE", ScannerState.ParseID );
		Token t3 = new Token( "daylight", ScannerState.ParseID );
		Token t4 = new Token( "vtodO", ScannerState.ParseID );
		Assert.True( t1.TokenVal == TokenValue.Tclass && t1.isSymbolicProperty() && !t1.isError());
		Assert.True( t2.TokenVal == TokenValue.Tattendee && t2.isMailtoProperty() && !t2.isError());
		Assert.True( t3.TokenVal == TokenValue.Tdaylight && t3.isResourceProperty() && !t3.isError());
		Assert.True( t4.TokenVal == TokenValue.Tvtodo && t4.isBeginEndValue() && !t4.isError());
	    }

	    public void testParseParms( )
	    {
		Token t1 = new Token( "\"jklsdfjkldfs\"", ScannerState.ParseParms, true );
		Token t2 = new Token( "a123-009", ScannerState.ParseParms );
		Assert.True( t1.TokenVal == TokenValue.QuotedString && !t1.isError());
		Assert.True( t2.TokenText == "a123-009" && t2.TokenVal == TokenValue.Parm && !t2.isError());
	    }

	    public void testParseValue( )
	    {
		Token t2 = new Token( "a123-009", ScannerState.ParseValue );
		Assert.True( t2.TokenText == "a123-009" && t2.TokenVal == TokenValue.Value && !t2.isError());
	    }

	    public void testCamelCase( )
	    {
		string val = Token.CamelCase( "stuff" );
		Assert.True( val == "stuff" );
		val = Token.CamelCase( "1-23" );
		Assert.True( val == "123" );
		val = Token.CamelCase( "x-attack" );
		Assert.True( val == "xAttack" );
		val = Token.CamelCase( "x-gorilla-attack" );
		Assert.True( val == "xGorillaAttack" );
		val = Token.CamelCase( "x-" );
		Assert.True( val == "x" );
		val = Token.CapsCamelCase( "x-" );
		Assert.True( val == "X" );
		val = Token.CapsCamelCase( "Valarm" );
		Assert.True( val == "Valarm" );
		val = Token.CapsCamelCase( "valarm" );
		Assert.True( val == "Valarm" );
	    }

	    public void testHtmlEncoding( )
	    {
		Token t1 = new Token( "this is, a value", ScannerState.ParseValue );
		Assert.True( t1.TokenVal == TokenValue.Value );
		Assert.True( t1.TokenText == "this is, a value" );
		t1 = new Token( "&this is, <a value>", ScannerState.ParseValue );
		Assert.True( t1.TokenVal == TokenValue.Value );
		Assert.True( t1.TokenText == "&amp;this is, &lt;a value&gt;" );
	    }
	}
    }
#endif
    #endregion
}
