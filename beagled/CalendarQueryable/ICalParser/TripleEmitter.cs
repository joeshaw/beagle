using System;
using System.Text;
using System.Collections;

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
    /// This emmits the RQL insert statements
    /// </summary>
    public class TripleEmitter : IEmitter
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

	private static string ical  = "http://www.w3.org/2002/12/cal/ical#";
	private static string apple = "http://www.w3.org/2002/12/cal/prod_apple#";
	private static string rdf   = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";

	ArrayList accumulator;
	Parser parser;
	Stack guids;

	public TripleEmitter( )
	{
	    accumulator = new ArrayList();
	    guids = new Stack();
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

	public Triple[] Triples
	{
	    get
	    {
		Triple[] rval = new Triple[ accumulator.Count ];
		for( int i = 0; i < accumulator.Count; ++i )
		{
		    rval[i] = (Triple) accumulator[i];
		}
		return rval;
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
	    // implement a default namespacing scheme - ie. all tags without a namespace are assumed to be 'ical:'
	    if( tag.IndexOf( ':' ) == -1 )
	    {
		return ical + tag;
	    }
	    else if( tag.StartsWith( "x:" ) && tag.Length > 2 )
	    {
		return apple + tag.Substring( 2 );
	    }
	    else
	    {
		return tag;
	    }
	}

	private string RDF( string tag )
	{
	    // implement a default namespacing scheme - ie. all tags without a namespace are assumed to be 'ical:'
	    if( tag.IndexOf( ':' ) == -1 )
	    {
		return rdf + tag;
	    }
	    else if( tag.StartsWith( "x:" ) && tag.Length > 2 )
	    {
		return apple + tag.Substring( 2 );
	    }
	    else
	    {
		return tag;
	    }
	}

	public void doIntro()
	{
	}

	public void doOutro()
	{
	}

	public void doEnd( Token t )
	{
	    StackElem pred = Pop();
	    if( pred.predFlag )
	    {
		if( Count > 0 )
		{
		    accumulator.Add( new Triple( NS( pred.id.TokenText ), Peek().guid, pred.guid, true ));
		}
		else
		{
		    throw new Exception( "Problem parsing: expecting overriding resource for: " + t.TokenText );
		}
	    }
	}

	public void doResourceBegin( Token t )
	{
	    Push( new StackElem( t, NewGuid(), true ));
	}

	public void doComponentBegin( Token t )
	{
	    // beginning of a rdfType for this stmt - ie. it's a new resource
	    string guid = NewGuid();
	    StackElem elem = new StackElem( t, guid , false );

	    if( Count > 0 && Peek().predFlag )
	    {
		StackElem top = Peek();
		top.guid = guid;
	    }
	    Push( elem );

	    accumulator.Add( new Triple( RDF( "type" ), guid, NS( t.TokenText ), true));
	}

	public void doBegin( Token t )
	{
	    // this indicates the beginning of a property describing the resource on the top of the stack
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
	    StackElem elem = new StackElem( t, null, false );  // the rest of the methods will fill in this properly
	    Push( elem );
	}

	public void doSymbolic( Token t )
	{
	    //TODO: url encoding
	    StackElem elem = Pop();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, NS( Token.CamelCase( t.TokenText.ToLower())), true));
	}

	public void doResource( Token t )
	{
	    StackElem elem = Pop();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, NS( t.TokenText ), true));
	}

	public void doURIResource( Token t )
	{
	    StackElem elem = Pop();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, NS( t.TokenText ), true));
	}

	public void doMailto( Token t )
	{
	    StackElem elem = Pop();
	    elem.guid = NewGuid();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, elem.guid, true ));
	    accumulator.Add( new Triple( NS( "calAddress" ), elem.guid, NS( t.TokenText ), true));
	    Push( elem );
	}

	public void doValueProperty( Token t, Token iprop )
	{
	    StackElem elem = Pop();
	    elem.guid = NewGuid();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, elem.guid, true ));

	    if( iprop != null )
	    {
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
		accumulator.Add( new Triple( tag, elem.guid, lobj, false ));
	    }
	    else
	    {
		accumulator.Add( new Triple( NS( "dateTime" ), elem.guid, Token.ParseDateTime( t.TokenText ), false));
	    }
	    Push( elem );
	}

	public void doIprop( Token t, Token iprop )
	{
	    StackElem elem = Pop();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, NS( iprop.TokenText ), true));
	}

	public void doRest( Token t, Token id )
	{
	    StackElem elem = Pop();
	    accumulator.Add( new Triple( NS( elem.id.TokenText ), Peek().guid, t.TokenText, false ));
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
	    accumulator.Add( new Triple( NS( key.TokenText ), Peek().guid, actual, false ));
	}

	public void emit( string val )
	{
	}

    }
}
