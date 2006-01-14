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

using Beagle.Util;
using FSQ=Beagle.Daemon.FileSystemQueryable;

namespace Beagle.Daemon {

	public class QueryStringParser {
		
		private QueryStringParser () { } // a static class

		// Returns an ICollection of QueryPart objects.
		static public ICollection Parse (string query_string)
		{
			ArrayList token_list;
			token_list = new ArrayList ();

			while (true) {
				Token token = ExtractToken (ref query_string);
				if (token == null)
					break;
				token_list.Add (token);
			}

			return TokensToQueryParts (token_list);
		}

		/////////////////////////////////////////////////////////

		private enum TokenType {
			Unknown,
			StandAlone,
			Plus,
			Minus,
			Operator,
			Meta
		}

		private class Token {
			public TokenType Type = TokenType.Unknown;
			public string    Text;
			public bool      IsQuoted;
		}

		// Our tiny query language:
		// prefix terms by + or - to require or prohibit them
		// 

		static private Token ExtractToken (ref string in_string)
		{
			if (in_string == null || in_string.Length == 0)
				return null;

			// Find the first non-whitespace character.
			int first_pos = -1;
			char first = ' ';
			bool first_is_singleton = false;
			for (int i = 0; i < in_string.Length; ++i) {
				first = in_string [i];
				if (! Char.IsWhiteSpace (first)) {
					first_pos = i;
					if (i == in_string.Length - 1)
						first_is_singleton = true;
					break;
				}
			}

			// This is only whitespace, and thus doesn't
			// contain any tokens.
			if (first_pos == -1)
				return null;

			Token token;
			token = new Token ();

			// Based on the first character, decide what kind of a
			// token this is.  Advance first_pos as necessary to
			// skip special characters (like + and -)
			switch (first) {
				
			case '+':
				if (first_is_singleton) {
					token.Type = TokenType.Meta;
					token.Text = "DanglingPlus";
					in_string = null;
					return token;
				}

				token.Type = TokenType.Plus;
				++first_pos;
				break;

			case '-':
				if (first_is_singleton) {
					token.Type = TokenType.Meta;
					token.Text = "DanglingMinus";
					in_string = null;
					return token;
				}

				token.Type = TokenType.Minus;
				++first_pos;
				break;

			case '"':
				if (first_is_singleton) {
					token.Type = TokenType.Meta;
					token.Text = "DanglingQuote";
					in_string = null;
					return token;
				}

				token.Type = TokenType.StandAlone;
				token.IsQuoted = true;
				++first_pos;
				break;

			default:
				token.Type = TokenType.StandAlone;
				break;
			}

			char last;
			last = token.IsQuoted ? '"' : ' ';
			
			int last_pos;
			last_pos = in_string.IndexOf (last, first_pos);
			
			if (last_pos == -1) {
				// We don't worry about missing close-quotes.
				// FIXME: Maybe we should, or at least return a meta-token
				token.Text = in_string.Substring (first_pos);
				in_string = null;
			} else {
				token.Text = in_string.Substring (first_pos, last_pos - first_pos);
				if (last_pos == in_string.Length-1)
					in_string = null;
				else
					in_string = in_string.Substring (last_pos+1);
			}

			// Trap the OR operator
			if (token.Type == TokenType.StandAlone && token.Text == "OR") {
				token.Type = TokenType.Operator;
				token.Text = "Or";
			}

			// Ah, the dreaded "internal error".
			if (token.Type == TokenType.Unknown)
				throw new Exception ("Internal QueryStringParser.ExtractToken Error");
				
			return token;
		}

		// FIXME support Date queries
		static private QueryPart TokenToQueryPart (Token token) {
			string query_text = token.Text;
			
			// also support extension query of form
			// beagle .pdf
			// i.e. the extension is just given as a query word
			bool is_extension_query = ((query_text [0] == '.') && (query_text.Length != 1));
			if (is_extension_query) {
				QueryPart_Property query_part = new QueryPart_Property ();
				query_part.Key = FSQ.FileSystemQueryable.FilenameExtensionPropKey;
				query_part.Value = query_text.ToLower (); // the whole .abc part
				query_part.Type = PropertyType.Keyword;
				Logger.Log.Debug ("Extension query:" + query_text.ToLower ());
				return query_part;
			}

			int pos = query_text.IndexOf (":");
			if ( pos == -1) {
				QueryPart_Text query_part = new QueryPart_Text ();
				query_part.Text = query_text;
				Logger.Log.Debug ("Parsed query '" + query_text + "' as text_query");
				return query_part;
			}

			string prop_name = query_text.Substring (0, pos);
			string prop_string = null;
			bool is_present;
			PropertyType prop_type;
			is_present = PropertyKeywordFu.GetPropertyDetails (prop_name, out prop_string, out prop_type);
			// if prop_name is not present in the mapping, assume the query is a text query
			// i.e. if token is foo:bar and there is no mappable property named foo,
			// assume "foo:bar" as text query
			// FIXME the analyzer changes the text query "foo:bar" to "foo bar"
			// which might not be the right thing to do
			if (!is_present) {
				QueryPart_Text query_part = new QueryPart_Text ();
				query_part.Text = query_text;
				Logger.Log.Debug ("Could not find property, parsed query '" + query_text + "' as text_query");
				return query_part;
			}

			QueryPart_Property query_part_prop = new QueryPart_Property ();
			query_part_prop.Key = prop_string;
			query_part_prop.Value = query_text.Substring (pos + 1);
			// if query was of type ext:mp3 or extension:mp3
			// change value to .mp3
			// if query was of type ext:
			// change value to ""
			// Change extension query value to lowercase - thats how they are stored on disk
			if (query_part_prop.Key == FSQ.FileSystemQueryable.FilenameExtensionPropKey)
				if (query_part_prop.Value != String.Empty)
					query_part_prop.Value = "." + query_part_prop.Value.ToLower ();
			query_part_prop.Type = prop_type;
			Logger.Log.Debug ("Parsed query '"	    + query_text + 
					  "' as prop query:key="    + query_part_prop.Key +
					  ", value="		    + query_part_prop.Value +
					  " and property type="	    + query_part_prop.Type);
			return query_part_prop;
		}

		static private ICollection TokensToQueryParts (ArrayList token_list)
		{
			ArrayList parts;
			parts = new ArrayList ();

			int i = 0;
			ArrayList or_list = null;

			while (i < token_list.Count) {
				Token token;
				token = token_list [i] as Token;

				if (token.Type == TokenType.Meta) {
					++i;
					continue;
				}

				// Skip any extra operators
				if (token.Type == TokenType.Operator) {
					++i;
					continue;
				}

				// Assemble a part for this token.

				QueryPart query_part = TokenToQueryPart (token);
				if (token.Type == TokenType.Minus)
					query_part.Logic = QueryPartLogic.Prohibited;
				else
					query_part.Logic = QueryPartLogic.Required;

					
				if (or_list != null) {
					or_list.Add (query_part);
					query_part = null;
				}

				Token next_token = null;
				if (i < token_list.Count - 1)
					next_token = token_list [i+1] as Token;


				// If the next token is an or, start an or_list
				// (if we don't have one already) and skip 
				// ahead to the next part.
				if (next_token != null 
				    && next_token.Type == TokenType.Operator
				    && next_token.Text == "Or") {
					if (or_list == null) {
						or_list = new ArrayList ();
						or_list.Add (query_part);
					}
					i += 2;
					continue;
				}

				// If we have a non-empty or-list going, 
				// Create the appropriate QueryPart and add it
				// to the list.
				if (or_list != null) {
					QueryPart_Or or_part;
					or_part = new QueryPart_Or ();
					or_part.Logic = QueryPartLogic.Required;
					foreach (QueryPart sub_part in or_list)
						or_part.Add (sub_part);
					parts.Add (or_part);
					or_list = null;
				}

				// Add the next text part
				if (query_part != null)
					parts.Add (query_part);

				++i;
			}

			// If we ended with an or_parts list, do the right thing.
			if (or_list != null) {
				QueryPart_Or or_part;
				or_part = new QueryPart_Or ();
				or_part.Logic = QueryPartLogic.Required;
				foreach (QueryPart sub_part in or_list)
					or_part.Add (sub_part);
			}

			return parts;
		}
	}

}
