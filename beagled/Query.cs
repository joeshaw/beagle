//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
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

using BU = Beagle.Util;

namespace Beagle.Daemon {

	[Flags]
	public enum QueryDomain {
		Local = 1,
		Neighorhood = 2,
		Global = 4
	}

	public class Query {

		// FIXME: This is not a good default
		//QueryDomain domainFlags = QueryDomain.Local | QueryDomain.Neighorhood | QueryDomain.Global;
		// FIXME: This is a good default when on an airplane.
		QueryDomain domainFlags = QueryDomain.Local; 

		ArrayList text = new ArrayList ();
		ArrayList mimeTypes = new ArrayList ();

		public Query ()
		{

		}

		public Query (string str) : this ()
		{
			AddText (str);
		}

		///////////////////////////////////////////////////////////////

		public void AddTextRaw (string str)
		{
			text.Add (str);
		}

		public void AddText (string str)
		{
			foreach (string textPart in BU.StringFu.SplitQuoted (str))
				AddTextRaw (textPart);
		}

		// FIXME: Since it is possible to introduce quotes via the AddTextRaw
		// method, this function should replace " with \" when appropriate.
		public string QuotedText {
			get { 
				string[] parts = new string [text.Count];
				for (int i = 0; i < text.Count; ++i) {
					string t = (string) text [i];
					if (BU.StringFu.ContainsWhiteSpace (t))
						parts [i] = "\"" + t + "\"";
					else
						parts [i] = t;
				}
				return String.Join (" ", parts);
			}
		}

		public IList Text {
			get { return text; }
		}

		public bool HasText {
			get { return text.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddMimeType (string str)
		{
			mimeTypes.Add (str);
		}
		
		public bool AllowsMimeType (string str)
		{
			if (mimeTypes.Count == 0)
				return true;
			foreach (string mt in mimeTypes)
				if (str == mt)
					return true;
			return false;
		}

		public IList MimeTypes {
			get { return mimeTypes; }
		}

		public bool HasMimeTypes {
			get { return mimeTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddDomain (QueryDomain d)
		{
			domainFlags |= d;
		}

		public void RemoveDomain (QueryDomain d)
		{
			domainFlags &= ~d;
		}
		
		public bool AllowsDomain (QueryDomain d)
		{
			return (domainFlags & d) != 0;
		}

		///////////////////////////////////////////////////////////////

		public bool IsEmpty {
			get { return text.Count == 0
				      && mimeTypes.Count == 0; }
		}
	}
}
