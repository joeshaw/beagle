//
// QueryBody.cs
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
using System.IO;
using System.Collections;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class QueryBody {

		// FIXME: This is a good default when on an airplane.
		Beagle.QueryDomain domainFlags = Beagle.QueryDomain.Local; 

		ArrayList text = new ArrayList ();
		ArrayList mimeTypes = new ArrayList ();
		ArrayList hitTypes = new ArrayList ();
		ArrayList searchSources = new ArrayList ();

		public QueryBody ()
		{

		}

		public QueryBody (string str) : this ()
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

		public void AddHitType (string str)
		{
			hitTypes.Add (str);
		}

		public bool AllowsHitType (string str)
		{
			if (hitTypes.Count == 0)
				return true;
			foreach (string ht in hitTypes)
				if (str == ht)
					return true;
			return false;
		}

		public IList HitTypes {
			get { return hitTypes; }
		}

		public bool HasHitTypes {
			get { return hitTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddSource (string str)
		{
			searchSources.Add (str);
		}
		

		public bool AllowsSource (string str)
		{
			if (searchSources.Count == 0)
				return true;
			foreach (string ss in searchSources)
				if (str.ToUpper () == ss.ToUpper ())
					return true;
			return false;
		}

		public IList Sources {
			get { return searchSources; }
		}

		public bool HasSources {
			get { return searchSources.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public void AddDomain (Beagle.QueryDomain d)
		{
			domainFlags |= d;
		}

		public void RemoveDomain (Beagle.QueryDomain d)
		{
			domainFlags &= ~d;
		}
		
		public bool AllowsDomain (Beagle.QueryDomain d)
		{
			return (domainFlags & d) != 0;
		}

		///////////////////////////////////////////////////////////////

		public bool IsEmpty {
			get { return text.Count == 0
				      && mimeTypes.Count == 0
				      && searchSources.Count == 0; }
		}

		public void WriteAsBinary (BinaryWriter writer)
                {
			writer.Write (text.Count);
			foreach (string str in text) {
				writer.Write (str);
			}

			writer.Write (mimeTypes.Count);
			foreach (string mimeType in mimeTypes) {
				writer.Write (mimeType);
			}

			writer.Write (searchSources.Count);
			foreach (string searchSource in searchSources) {
				writer.Write (searchSource);
			}
		}

		public static QueryBody ReadAsBinary (BinaryReader reader)
                {
			QueryBody query = new QueryBody();

			int numTexts = reader.ReadInt32 ();
                        for (int i = 0; i < numTexts; i++) {
                                query.AddText (reader.ReadString ());
                        }

			int numMimeTypes = reader.ReadInt32 ();
                        for (int i = 0; i < numMimeTypes; i++) {
                                query.AddMimeType (reader.ReadString ());
                        }

			int numSearchSources = reader.ReadInt32 ();
                        for (int i = 0; i < numSearchSources; i++) {
                                query.AddSource (reader.ReadString ());
                        }

			return query;
		}
	}
}
