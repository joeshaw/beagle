using System;

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
    /// Represents the subject predicate object triple
    /// </summary>
    public class Triple : ITriple
    {
	string subj, obj, pred;
	bool objIsResource;

	public Triple( string _pred, string _subj, string _obj, bool _objIsResource )
	{
	    pred = _pred;
	    subj = _subj;
	    obj = _obj;
	    objIsResource = _objIsResource;
	}

	public string GetSubject( )
	{
	    return subj;
	}

	public string GetObject( )
	{
	    return obj;
	}

	public string GetPredicate( )
	{
	    return pred;
	}

	public bool IsResource( )
	{
	    return objIsResource;
	}

	public override string ToString()
	{
	    string rval = "[" + GetPredicate() + "] [" + GetSubject() + "] ";
	    if( IsResource() )
	    {
		rval += "[" + GetObject() + "]";
	    }
	    else
	    {
		rval += "'" + GetObject() + "'";
	    }
	    return rval;
	}

    }

    public interface ITriple
    {
	string GetSubject();
	string GetObject();
	string GetPredicate();
	bool IsResource();
    }
}
