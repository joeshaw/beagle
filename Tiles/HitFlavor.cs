//
// HitFlavor.cs
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

using BU = Beagle.Util;

namespace Beagle.Tile {

	[AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
	public class HitFlavor : Attribute {

		public string Name;
		public string Emblem;
		public string Color;
		public int    Columns = 3;
		public int    Rank = 0;
		public Type   TileType;

		private string uri;
		private string type;
		private string mimeType;
		private string source;

		public string Uri { 
			get { return uri; }
			set { uri = IsWild (value) ? null : value; }
		}

		public string Type {
			get { return type; }
			set { type = IsWild (value) ? null : value; }
		}

		public string MimeType {
			get { return mimeType; }
			set { mimeType = IsWild (value) ? null : value; }
		}

		public string Source {
			get { return source; }
			set { source = IsWild (value) ? null : value; }
		}

		private bool IsWild (string str)
		{
			if (str == null)
				return true;
			if (str == "")
				return false;
			foreach (char c in str)
				if (c != '*')
					return false;
			return true;
		}

		public bool IsMatch (Hit hit)
		{
			return (Uri == null || BU.StringFu.GlobMatch (Uri, hit.Uri.ToString ()))
				&& (Type == null || BU.StringFu.GlobMatch (Type, hit.Type))
				&& (MimeType == null || BU.StringFu.GlobMatch (MimeType, hit.MimeType))
				&& (Source == null || BU.StringFu.GlobMatch (Source, hit.Source));
		}

		public int Weight {
			get {
			
				//KNV: Ensure Network Tile Flavor gets preference over all others:
				if (Name.Equals("Network")) {
					return 100;
				}		
						
				int weight = 0;
				if (Type != null)
					weight += 8;
				if (MimeType != null)
					weight += 4;
				if (Uri != null)
					weight += 2;
				if (Source != null)
					weight += 1;
				return weight;
			}
		}

		override public string ToString ()
		{
			string str = "";
			if (Uri != null)
				str += " Uri=" + Uri;
			if (Type != null)
				str += " Type=" + Type;
			if (MimeType != null)
				str += " MimeType=" + MimeType;
			if (Source != null)
				str += " Source=" + Source;
			return "HitFlavor (" + str.Trim () + ")";
		}
	}
}
