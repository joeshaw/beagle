//
// QueryStringParser.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
//

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

using System;
using System.Collections;
using System.Text.RegularExpressions;

using Beagle.Util;
using FSQ=Beagle.Daemon.FileSystemQueryable;

namespace Beagle.Daemon {

	public class QueryStringParser {
		
		private QueryStringParser () { } // a static class

		// Returns an ICollection of QueryPart objects.
		static public ICollection Parse (string query_string) 
		{
			
			/* This is our regular Expression Pattern:
			 * we expect something like this:
			 * -key:"Value String"
			 * key:Value
			 * or
			 * Value
		 		([+-]?)		# Required or Prohibited (optional)
				(\w+:)?		# Key  (optional)
				(		# Query Text
				 (\"([^\"]*)\"?)#  quoted
				 |		#  or
				 ([^\s\"]+)	#  unquoted
				)
				";
			 */
			string Pattern = "(?<pm>[+-]?) ( (?<key>\\w+) :)? ( (\"(?<quote>[^\"]*)\"?) | (?<unquote>[^\\s\"]+) )";
	
			Regex r = new Regex (Pattern, RegexOptions.IgnorePatternWhitespace);

			Match m = r.Match (query_string);
		
			ArrayList parts;
			parts = new ArrayList ();

			ArrayList or_list = null;

			while (m.Success) {

				QueryPart query_part = MatchToQueryPart (m);

				if (or_list != null) {
					or_list.Add (query_part);
					query_part = null;
				}

				Match next = m.NextMatch ();

				// Trap the OR operator
				// If the next match is an or, start an or_list
				// (if we don't have one already) and skip 
				// ahead to the next part.
				if ( next.Success
				    && (next.Groups ["key"].ToString () == "")
				    && (next.Groups ["unquote"].ToString ().ToUpper () == "OR") ) {

					if (or_list == null) {
						or_list = new ArrayList ();
						or_list.Add (query_part);
					}

					m = next.NextMatch();
					continue;
				}

				// If we have a non-empty or-list going, 
				// Create the appropriate QueryPart and add it
				// to the list.
				if (or_list != null) {

					QueryPart_Or or_part = new QueryPart_Or ();
					or_part.Logic = QueryPartLogic.Required;

					foreach (QueryPart sub_part in or_list)
						or_part.Add (sub_part);

					parts.Add (or_part);
					or_list = null;
				}

				// Add the next text part
				if (query_part != null)
					parts.Add (query_part);

				m=next;
			}

			// If we ended with an or_parts list, do the right thing.
			if (or_list != null) {

				QueryPart_Or or_part = new QueryPart_Or ();
				or_part.Logic = QueryPartLogic.Required;

				foreach (QueryPart sub_part in or_list)
					or_part.Add (sub_part);
			}

			return parts;
		}

		static private QueryPart StringToQueryPart (string text, bool is_prohibited)
		{
			QueryPart part;

			if (text.IndexOf ('*') != -1) {
				part = new QueryPart_Wildcard ();
				((QueryPart_Wildcard) part).QueryString = text;
			} else {
				part = new QueryPart_Text ();
				((QueryPart_Text) part).Text = text;
			}

			part.Logic = (is_prohibited ? QueryPartLogic.Prohibited : QueryPartLogic.Required);
			return part;
		}

		static private QueryPart MatchToQueryPart (Match m) 
		{
			// Looping over all Matches we have got:
			// m.Groups["pm"]	plus or minus sign 
			// m.Groups["key"]	keyname
			// m.Groups["quote"]	quoted string
			// m.Groups["unquote"]	unquoted string
			
			string query = m.ToString ();
			string unquote = m.Groups["unquote"].ToString ();
			string text = m.Groups ["quote"].ToString () + m.Groups ["unquote"].ToString ();	// only one of both is set.
			string key = m.Groups ["key"].ToString ();
			
			bool IsProhibited = (m.Groups ["pm"].ToString () == "-");
				

			// check for file extensions
			// if match starts with *. or . and only contains letters we assume it's a file extension
			Regex extension_re = new Regex (@"^\**\.\w*$");

			if (extension_re.Match (text).Success || key.ToLower () == "ext" || key.ToLower () == "extension") {
				
				QueryPart_Property query_part = new QueryPart_Property ();

				query_part.Key = Property.FilenameExtensionPropKey;

				if (text.StartsWith ("*."))
					query_part.Value = text.Substring (1).ToLower ();
				else if (text.StartsWith ("."))
					query_part.Value = text.ToLower ();
				else
					query_part.Value = "." + text.ToLower ();

				query_part.Type = PropertyType.Keyword;
				query_part.Logic = (IsProhibited ? QueryPartLogic.Prohibited : QueryPartLogic.Required);
				
				Logger.Log.Debug ("Extension query: {0}", query_part.Value);
				
				return query_part; 
			}

			if (key == "") {

				Logger.Log.Debug ("Parsed query '{0}' as text_query", text);

				return StringToQueryPart (text, IsProhibited);
			}

			string prop_string = null;
			bool is_present;
			PropertyType prop_type;

			is_present = PropertyKeywordFu.GetPropertyDetails (key, out prop_string, out prop_type);
			// if key is not present in the mapping, assume the query is a text query
			// i.e. if token is foo:bar and there is no mappable property named foo,
			// assume "foo:bar" as text query
			// FIXME the analyzer changes the text query "foo:bar" to "foo bar"
			// which might not be the right thing to do

			if (!is_present) {

				Logger.Log.Debug ("Could not find property, parsed query '{0}' as text_query", query);

				return StringToQueryPart (query, IsProhibited);
			}

			QueryPart_Property query_part_prop = new QueryPart_Property ();
			query_part_prop.Key = prop_string;
			query_part_prop.Value = text;
			query_part_prop.Type = prop_type;
			query_part_prop.Logic = (IsProhibited ? QueryPartLogic.Prohibited : QueryPartLogic.Required);

			Logger.Log.Debug ("Parsed query '"	    + query + 
					  "' as prop query:key="    + query_part_prop.Key +
					  ", value="		    + query_part_prop.Value +
					  " and property type="	    + query_part_prop.Type);

			return query_part_prop;
		}
	}
}

