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

			if (key == String.Empty) {

				Logger.Log.Debug ("Parsed query '{0}' as text_query", text);

				return StringToQueryPart (text, IsProhibited);
			}

			// FIXME: i18n-izing "date"
			if (key == "date") {
				try {
					QueryPart part = DateQueryToQueryPart (text);
					part.Logic = (IsProhibited ? QueryPartLogic.Prohibited : QueryPartLogic.Required);
					return part;
				} catch (FormatException) {
					Log.Debug ("Could not parse [{0}] as date query. Assuming text.", text);
					return StringToQueryPart (text, IsProhibited);
				}
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

		private static QueryPart DateQueryToQueryPart (string query)
		{
			// Format
			// query := date[-date]
			// date := empty | year | year.month | year.month.date 
			// FIXME: Do we really want to query time too ? They are too long!

			if (query == String.Empty)
				throw new FormatException ();

			int next_date_index = query.IndexOf ('-');
			if (next_date_index == -1)
				return DateToQueryPart (query);

			// We have a range query - get the ranges
			DateTime start_date, end_date;

			try {
				int y, m, d;
				ParseDateQuery (query.Substring (0, next_date_index), out y, out m, out d);
				start_date = CreateDateTime (y, m, d, true);
			} catch (FormatException) {
				start_date = DateTime.MinValue;
			}
				
			try {
				int y, m, d;
				ParseDateQuery (query.Substring (next_date_index + 1), out y, out m, out d);
				end_date = CreateDateTime (y, m, d, false);
			} catch (FormatException) {
				// FIXME: Should the default end_date be DateTime.Now ?
				end_date = DateTime.MinValue;
			}

			if (start_date == DateTime.MinValue && end_date == DateTime.MinValue)
				throw new FormatException ();

			QueryPart_DateRange range_query = new QueryPart_DateRange ();
			range_query.Key = QueryPart_DateRange.TimestampKey;
			range_query.StartDate = start_date;
			range_query.EndDate = end_date;
			Log.Debug ("Parsed date range query [{0}] as {1}", query, range_query);

			return range_query;
		}

		private static void ParseDateQuery (string dt_string, out int year, out int month, out int date)
		{
			year = month = date = -1;

			if (dt_string.Length >= 4)
				year = Convert.ToInt32 (dt_string.Substring (0, 4));

			if (dt_string.Length >= 6)
				month = Convert.ToInt32 (dt_string.Substring (4, 2));

			if (dt_string.Length == 8)
				date = Convert.ToInt32 (dt_string.Substring (6, 2));

			if (dt_string.Length > 8 || year == -1)
				throw new FormatException ();
		}

		private static DateTime CreateDateTime (int y, int m, int d, bool start_date)
		{
			if (m == -1)
				m = (start_date ? 1 : 12);
			if (d == -1)
				d = (start_date ? 1 : DateTime.DaysInMonth (y, m));

			int hour = (start_date ? 0 : 23);
			int min  = (start_date ? 0 : 59);
			int sec  = (start_date ? 0 : 59);

			// Create the date in local time
			DateTime dt = new DateTime (y, m, d, hour, min, sec, DateTimeKind.Local);

			// Dates could be in local or UTC
			// Convert them to UTC
			return dt.ToUniversalTime ();
		}

		private static QueryPart_DateRange DateToQueryPart (string dt_string)
		{
			int y, m, d;
			ParseDateQuery (dt_string, out y, out m, out d);

			QueryPart_DateRange dt_query = new QueryPart_DateRange ();
			dt_query.Key = QueryPart_DateRange.TimestampKey;
			dt_query.StartDate = CreateDateTime (y, m, d, true);
			dt_query.EndDate =  CreateDateTime (y, m, d, false);
			Log.Debug ("Parsed date query [{0}] as {1}", dt_string, dt_query);

			return dt_query;
		}
	}
}

