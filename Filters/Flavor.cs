//
// Flavor.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.IO;

namespace Beagle.Filters {

	public class Flavor : IComparable {

		static readonly public String Wildcard = "*";
		
		String mimeType;
		String extension;

		public Flavor (String _mimeType, String _extension)
		{
			if (_mimeType == null)
				_mimeType = "";
			if (_extension == null)
				_extension = "";

			mimeType = _mimeType;
			extension = _extension;
		}

		static public Flavor FromMimeType (String mimeType)
		{
			return new Flavor (mimeType, "");
		}

		static public Flavor FromExtension (String extension)
		{
			return new Flavor ("", extension);
		}

		static public Flavor FromPath (String path)
		{
			String mimeType = Beagle.Util.VFS.Mime.GetMimeType (path);
			String extension = Path.GetExtension (path);
			return new Flavor (mimeType, extension);
		}

		public String MimeType {
			get { return mimeType; }
		}

		public String Extension {
			get { return Extension; }
		}
		
		int PatternCount {
			get {
				int count = 0;
				if (mimeType == Wildcard)
					++count;
				if (extension == Wildcard)
					++count;
				return count;
			}
		}

		public bool IsPattern {
			get { return PatternCount > 0; }
		}

		public bool IsMatch (Flavor other)
		{
			return (mimeType == Wildcard || mimeType == other.MimeType)
				&& (extension == Wildcard || extension == other.Extension);
		}
		
		override public int GetHashCode ()
		{
			return mimeType.GetHashCode () ^ extension.GetHashCode ();
		}
		
		override public bool Equals (object rhs)
		{
			Flavor other = rhs as Flavor;
			return other != null
				&& mimeType == other.mimeType
				&& extension == other.extension;
		}

		public int CompareTo (object rhs)
		{
			if (rhs == null)
				return 1;

			if (rhs.GetType () != this.GetType ())
				throw new ArgumentException ();

			Flavor other = rhs as Flavor;

			int cmp = PatternCount.CompareTo (other.PatternCount);
			
			if (cmp == 0)
				cmp = mimeType.CompareTo (other.mimeType);
			if (cmp == 0)
				cmp = extension.CompareTo (other.extension);
			
			return cmp;
		}

		override public String ToString ()
		{
			String str = "[";
			if (mimeType != "")
				str += "mime=" + mimeType;
			if (mimeType != "" && extension != "")
				str += ", ";
			if (extension != "")
				str += "ext=" + extension;
			str += "]";
			return str;
		}

	}
}
