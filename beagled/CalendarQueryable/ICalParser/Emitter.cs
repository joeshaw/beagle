using System;
using System.Text;
using System.Collections;
using System.IO;

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
    /// Defines all of the basic operations that must be implemented by parser emitters.
    /// </summary>
    /// <remarks>
    /// 
    /// </remarks>
    public interface IEmitter
    {
	void doIntro();
	void doOutro();
	void doEnd( Token t);
	void doResourceBegin( Token t );
	void doBegin( Token t );
	void doComponentBegin( Token t );
	void doComponent( );
	void doEndComponent( );
	void doID( Token t );
	void doSymbolic( Token t );
	void doResource( Token t );
	void doURIResource( Token t );
	void doMailto( Token t );
	void doValueProperty( Token t, Token iprop );
	void doIprop( Token t, Token iprop );
	void doRest( Token t, Token id );
	void doAttribute( Token t1, Token t2 );
	Parser VParser{ get; set; }
	void emit( string val );
    }


    /// <summary>
    /// This emits iCalendar/RDF as a string.  This emitter is the one that most people will use for
    /// creating RDF from iCalendar files.  
    /// </summary>
    /// <remarks>
    /// This emitter is partially based on and inspired by the the ical2rdf.pl converter developed
    /// by Dan Connolly and Libby Miller and is probably a little out of date with respect to the
    /// current schema.
    /// (see <see cref="http://www.w3.org/2002/12/cal/"/> for more info on the W3C working group 
    /// responsible for the iCalendar RDF schema development)
    /// </remarks>
    public class RDFEmitter : IEmitter
    {
	private static string ical = "http://www.w3.org/2002/12/cal/ical#";

	StringBuilder accumulator;
	Parser parser;

	public RDFEmitter( )
	{
	    accumulator = new StringBuilder();
	}

	public Parser VParser
	{
	    get { return parser; }
	    set { parser = value; }
	}

	public string Rdf
	{
	    get { return accumulator.ToString(); }
	}

	public void doIntro()
	{
	    accumulator.Append( @"<rdf:RDF
  xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
  xmlns='" + ical + @"'
  xmlns:x='http://www.w3.org/2002/12/cal/prod_apple#'
  xmlns:ical='" + ical + @"'>
");
	}

	public void doOutro()
	{
	    accumulator.Append( "</rdf:RDF>" );
	}

	public void doEnd( Token t)
	{
	    accumulator.Append( "</" + t.TokenText + ">\n" );
	}

	public void doResourceBegin( Token t )
	{
	    accumulator.Append( "<" + t.TokenText + " rdf:parseType='Resource'>\n" );
	}

	public void doBegin( Token t )
	{
	    accumulator.Append( "<" + t.TokenText + ">\n" );
	}

	public void doComponentBegin( Token t )
	{
	    accumulator.Append( "<" + t.TokenText + ">\n" );
	}

	public void doComponent( )
	{
	    accumulator.Append( "<component>\n" );
	}

	public void doEndComponent( )
	{
	    accumulator.Append( "</component>\n" );
	}

	public void doID( Token t )
	{
	    accumulator.Append( "<" + t.TokenText );
	}

	public void doSymbolic( Token t )
	{
	    //TODO: url encoding
	    accumulator.Append( " rdf:resource='" + ical + Token.CamelCase( t.TokenText.ToLower() ) + "'/>\n" );
	}

	public void doResource( Token t )
	{
	    accumulator.Append( " rdf:resource='" + t.TokenText + "'/>\n" );
	}

	public void doURIResource( Token t )
	{
	    accumulator.Append( " rdf:resource='uri:" + t.TokenText + "'/>\n" );
	}

	public void doMailto( Token t )
	{
	    accumulator.Append( " rdf:parseType='Resource'>\n<calAddress rdf:resource='" + t.TokenText + "'/>\n" );
	}

	public void doValueProperty( Token t, Token iprop )
	{
	    accumulator.Append( " rdf:parseType='Resource'>\n" );
	    if( iprop != null )
	    {
		string lprop = Token.CamelCase( iprop.TokenText.ToLower());
		string lobj;
		if( lprop == "date" )
		{
		    lobj = Token.ParseDate( t.TokenText );
		}
		else
		{
		    lobj = Token.CamelCase( t.TokenText.ToLower());
		}
		accumulator.Append( "<" + lprop + ">" + lobj + "</" + lprop + ">\n" );
	    }
	    else
	    {
		accumulator.Append( "<dateTime>" + Token.ParseDateTime( t.TokenText ) + "</dateTime>\n" );
	    }
	}

	public void doIprop( Token t, Token iprop )
	{
	    accumulator.Append( " ical:" + iprop.TokenText + "='" + t.TokenText + "'/>\n" );
	}

	public void doRest( Token t, Token id )
	{
	    accumulator.Append( ">" + t.TokenText + "</" + id.TokenText + ">\n" );
	}

	public void doAttribute( Token key, Token val )
	{
	    string actual;
	    if( key.TokenText == "until" )
	    {
		actual = Token.ParseDateTime( val.TokenText );
	    }
	    else
	    {
		actual = val.TokenText;
	    }
	    accumulator.Append( "<" + key.TokenText + ">" + actual + "</" + key.TokenText + ">\n" );
	}

	public void emit( string val )
	{
	    accumulator.Append( val );
	}
    }

    /// <summary>
    /// This emmits RDF to a Stream
    /// </summary>
    public class RDFStreamEmitter : IEmitter
    {
	private static string ical = "http://www.w3.org/2002/12/cal/ical#";

	TextWriter accumulator;
	Parser parser;

	public RDFStreamEmitter( TextWriter _accumulator )
	{
	    accumulator = _accumulator;
	}

	public Parser VParser
	{
	    get { return parser; }
	    set { parser = value; }
	}

	public void doIntro()
	{
	    accumulator.Write( @"<rdf:RDF
  xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#'
  xmlns='" + ical + @"'
  xmlns:x='http://www.w3.org/2002/12/cal/prod_apple#'
  xmlns:ical='" + ical + @"'>
");
	}

	public void doOutro()
	{
	    accumulator.Write( "</rdf:RDF>" );
	}

	public void doEnd( Token t)
	{
	    accumulator.Write( "</" + t.TokenText + ">\n" );
	}

	public void doResourceBegin( Token t )
	{
	    accumulator.Write( "<" + t.TokenText + " rdf:parseType='Resource'>\n" );
	}

	public void doBegin( Token t )
	{
	    accumulator.Write( "<" + t.TokenText + ">\n" );
	}

	public void doComponentBegin( Token t )
	{
	    accumulator.Write( "<" + t.TokenText + ">\n" );
	}

	public void doComponent( )
	{
	    accumulator.Write( "<component>\n" );
	}

	public void doEndComponent( )
	{
	    accumulator.Write( "</component>\n" );
	}

	public void doID( Token t )
	{
	    accumulator.Write( "<" + t.TokenText );
	}

	public void doSymbolic( Token t )
	{
	    //TODO: url encoding
	    accumulator.Write( " rdf:resource='" + ical + Token.CamelCase( t.TokenText.ToLower() ) + "'/>\n" );
	}

	public void doResource( Token t )
	{
	    accumulator.Write( " rdf:resource='" + t.TokenText + "'/>\n" );
	}

	public void doURIResource( Token t )
	{
	    accumulator.Write( " rdf:resource='uri:" + t.TokenText + "'/>\n" );
	}

	public void doMailto( Token t )
	{
	    accumulator.Write( " rdf:parseType='Resource'>\n<calAddress rdf:resource='" + t.TokenText + "'/>\n" );
	}

	public void doValueProperty( Token t, Token iprop )
	{
	    accumulator.Write( " rdf:parseType='Resource'>\n" );
	    if( iprop != null )
	    {
		string lprop = Token.CamelCase( iprop.TokenText.ToLower());
		string lobj;
		if( lprop == "date" )
		{
		    lobj = Token.ParseDate( t.TokenText );
		}
		else
		{
		    lobj = Token.CamelCase( t.TokenText.ToLower());
		}
		accumulator.Write( "<" + lprop + ">" + lobj + "</" + lprop + ">\n" );
	    }
	    else
	    {
		accumulator.Write( "<dateTime>" + Token.ParseDateTime( t.TokenText ) + "</dateTime>\n" );
	    }
	}

	public void doIprop( Token t, Token iprop )
	{
	    accumulator.Write( " ical:" + iprop.TokenText + "='" + t.TokenText + "'/>\n" );
	}

	public void doRest( Token t, Token id )
	{
	    accumulator.Write( ">" + t.TokenText + "</" + id.TokenText + ">\n" );
	}

	public void doAttribute( Token key, Token val )
	{
	    string actual;
	    if( key.TokenText == "until" )
	    {
		actual = Token.ParseDateTime( val.TokenText );
	    }
	    else
	    {
		actual = val.TokenText;
	    }
	    accumulator.Write( "<" + key.TokenText + ">" + actual + "</" + key.TokenText + ">\n" );
	}

	public void emit( string val )
	{
	    accumulator.Write( val );
	}
    }


    // this is a delegate for periodically saving the triples out to the database (for RQLEmitter)
    public delegate void Persister( string rql );

    /// <summary>
    /// This emmits the RQL insert statements
    /// </summary>
    public class RQLEmitter : IEmitter
    {

	private class StackElem
	{
	    public Token id;
	    public string guid;
	    public bool predFlag;

	    public StackElem( Token _id, string _guid, bool _predFlag )
	    {
		id = _id;
		guid = _guid;
		predFlag = _predFlag;
	    }
	}

	private const string ical = "http://www.w3.org/2002/12/cal/ical#";
	private const string defaultContext = "http://www.intellidimension.com/rdfserver#null"; 
	private const int maxTriples = 150;
	private const string defaultTable = "icaltest";

	StringBuilder accumulator;
	Parser parser;
	Stack guids;
	private string context = defaultContext;
	private Persister persistCallback = null;
	private int numTriples = 0;
	private string table = defaultTable;

	public RQLEmitter( )
	{
	    accumulator = new StringBuilder();
	    guids = new Stack();
	}

	public RQLEmitter( string _context ) : this( )
	{
	    context = _context;
	}

	public RQLEmitter( string _context, string _table, Persister _persistCallback ) : this( _context )
	{
	    persistCallback = _persistCallback;
	    table = _table;
	}

	public RQLEmitter( Persister _persistCallback ) : this( )
	{
	    persistCallback = _persistCallback;
	}

	private static string NewGuid()
	{
	    return "guid:" + Guid.NewGuid().ToString();
	}

	public Parser VParser
	{
	    get { return parser; }
	    set { parser = value; }
	}

	public string Rql
	{
	    get { return realIntro() + accumulator.ToString() + realOutro(); }
	}

	private void Inc( )
	{
	    // this should be called everytime a triple is added in the emitter
	    if( ++numTriples > maxTriples )
	    {
		Dump( );
	    }
	}

	private void Dump( )
	{
	    if( persistCallback != null )
	    {
		string theRql = String.Format( Rql, table ); 
		persistCallback( theRql );
		numTriples = 0;
		accumulator = new StringBuilder();
	    }
	}

	// convenience methods for the internal stack used for emmiting code
	private StackElem Peek()
	{
	    return (StackElem) guids.Peek();
	}

	private StackElem Pop()
	{
	    return (StackElem) guids.Pop();
	}

	private void Push( StackElem elem )
	{
	    guids.Push( elem );
	}

	private int Count
	{
	    get { return guids.Count; }
	}

	private string NS( string tag )
	{
	    //TODO: implement a default namespacing scheme - ie. right now, all tags without a namespace are assumed to be 'ical:'
	    if( tag.IndexOf( ':' ) == -1 )
	    {
		return "ical:" + tag;
	    }
	    else
	    {
		return tag;
	    }
	}

	public void doIntro()
	{
	}

	// indicates EOF - flush remaining triples
	public void doOutro()
	{
	    Dump( );
	}

	private string realIntro()
	{
	    return ( 
		"Session.namespaces[\"rdf\"] = \"http://www.w3.org/1999/02/22-rdf-syntax-ns#\";\n" +
		"Session.namespaces[\"ical\"] = \"" + ical + "\";\n" +
		"Session.namespaces[\"x\"] = \"http://www.w3.org/2002/12/cal/prod_apple#\";\n" +
		"INSERT \n" );
	}

	private string realOutro()
	{
	    return( " INTO {0};\n" );
	}

	public void doEnd( Token t )
	{
	    //accumulator.Append( "</" + t.TokenText + ">\n" );
	    StackElem pred = Pop();
	    if( pred.predFlag )
	    {
		if( Count > 0 )
		{
		    accumulator.Append( "{{[" + context + "] [" + NS( pred.id.TokenText ) + "] [" + Peek().guid + "] [" + pred.guid + "]}}\n" );
		    Inc( );
		}
		else
		{
		    throw new Exception( "Problem parsing: expecting overriding resource for: " + t.TokenText );
		}
	    }
	}

	public void doResourceBegin( Token t )
	{
	    //accumulator.Append( "<" + t.TokenText + " rdf:parseType='Resource'>\n" );
	    Push( new StackElem( t, NewGuid(), true ));
	}

	public void doComponentBegin( Token t )
	{
	    //accumulator.Append( "<" + t.TokenText + ">\n" );
	    // beginning of a rdfType for this stmt - ie. it's a new resource
	    string guid = NewGuid();
	    StackElem elem = new StackElem( t, guid , false );

	    if( Count > 0 && Peek().predFlag )
	    {
		StackElem top = Peek();
		top.guid = guid;
	    }
	    Push( elem );

	    accumulator.Append( "{{[" + context + "] [rdf:type] [" + guid + "] [" + NS( t.TokenText ) + "]}}\n" );
	    Inc( );
	}

	public void doBegin( Token t )
	{
	    // this indicates the beginning of a property describing the resource on the top of the stack
	    // accumulator.Append( "<" + t.TokenText + ">\n" );
	    Push( new StackElem( t, null, true ));
	}

	public void doComponent( )
	{
	    doBegin( new Token( "component" ));
	}

	public void doEndComponent( )
	{
	    doEnd( new Token( "component" ));
	}

	public void doID( Token t )
	{
	    //accumulator.Append( "<" + t.TokenText );
	    StackElem elem = new StackElem( t, null, false );  // the rest of the methods will fill in this properly
	    Push( elem );
	}

	public void doSymbolic( Token t )
	{
	    //TODO: url encoding
	    //accumulator.Append( " rdf:resource='" + ical + Token.CamelCase( t.TokenText.ToLower() ) + "'/>\n" );
	    StackElem elem = Pop();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + NS( Token.CamelCase( t.TokenText.ToLower() )) + "]}}\n" );
	    Inc( );
	}

	public void doResource( Token t )
	{
	    //accumulator.Append( " rdf:resource='" + t.TokenText + "'/>\n" );
	    StackElem elem = Pop();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + NS( t.TokenText ) + "]}}\n" );
	    Inc( );
	}

	public void doURIResource( Token t )
	{
	    //accumulator.Append( " rdf:resource='" + t.TokenText + "'/>\n" );
	    StackElem elem = Pop();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + NS( t.TokenText ) + "]}}\n" );
	    Inc( );
	}

	public void doMailto( Token t )
	{
	    //accumulator.Append( " rdf:parseType='Resource'>\n<calAddress rdf:resource='" + t.TokenText + "'/>\n" );
	    StackElem elem = Pop();
	    elem.guid = NewGuid();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + elem.guid + "]}}\n" );
	    accumulator.Append( "{{[" + context + "] [ical:calAddress] [" + elem.guid + "] [" + NS( t.TokenText ) + "]}}\n" );
	    Push( elem );
	    Inc( );
	}

	public void doValueProperty( Token t, Token iprop )
	{
	    //accumulator.Append( " rdf:parseType='Resource'>\n" );
	    StackElem elem = Pop();
	    elem.guid = NewGuid();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + elem.guid + "]}}\n" );

	    if( iprop != null )
	    {
		//accumulator.Append( "<" + Token.CamelCase( iprop.TokenText.ToLower()) + ">" + t.TokenText + "</" + Token.CamelCase( iprop.TokenText.ToLower()) + ">\n" );
		string tag = NS( Token.CamelCase( iprop.TokenText.ToLower()));
		string lprop = Token.CamelCase( iprop.TokenText.ToLower());
		string lobj;
		if( lprop == "date" )
		{
		    lobj = Token.ParseDate( t.TokenText );
		}
		else
		{
		    lobj = Token.CamelCase( t.TokenText.ToLower());
		}
		accumulator.Append( "{{[" + context + "] [" + tag + "] [" + elem.guid + "] '" + lobj + "'}}\n" );
	    }
	    else
	    {
		//accumulator.Append( "<dateTime>" + Token.ParseDate( t.TokenText ) + "</dateTime>\n" );
		accumulator.Append( "{{[" + context + "] [ical:dateTime] [" + elem.guid + "] '" + Token.ParseDateTime( t.TokenText ) + "'}}\n" );
	    }
	    Push( elem );
	    Inc( );
	}

	public void doIprop( Token t, Token iprop )
	{
	    //accumulator.Append( " ical:" + iprop.TokenText + "='" + t.TokenText + "'/>\n" );
	    StackElem elem = Pop();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] [" + NS( iprop.TokenText ) + "]}}\n" );
	    Inc( );
	}

	public void doRest( Token t, Token id )
	{
	    //accumulator.Append( ">" + t.TokenText + "</" + id.TokenText + ">\n" );
	    StackElem elem = Pop();
	    accumulator.Append( "{{[" + context + "] [" + NS( elem.id.TokenText ) + "] [" + Peek().guid + "] '" + t.TokenText + "'}}\n" );
	    Inc( );
	}

	public void doAttribute( Token key, Token val )
	{
	    //accumulator.Append( "<" + key.TokenText + ">" + val.TokenText + "</" + key.TokenText + ">\n" );
	    // special case - convert 'until' token values from icalendar DateTimes to new DateTimes
	    string actual;
	    if( key.TokenText == "until" )
	    {
		actual = Token.ParseDateTime( val.TokenText );
	    }
	    else
	    {
		actual = val.TokenText;
	    }
	    accumulator.Append( "{{[" + context + "] [" + NS( key.TokenText ) + "] [" + Peek().guid + "] '" + actual + "'}}\n" );
	    Inc( );
	}

	public void emit( string val )
	{
	    accumulator.Append( val );
	    Inc( );
	}
    }
 }
