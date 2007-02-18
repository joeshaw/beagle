//
// FilterRPM.cs
//
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
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
using System.Runtime.InteropServices;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {
	public class FilterRPM : FilterPackage {
		
		// Adding something will require changing rpm-glue.c
		enum TagType {
			NAME = 1,
			VERSION,
			SUMMARY,
			DESCRIPTION,
			GROUP,
			LICENSE,
			PACKAGER,
			URL,
			SIZE
		};
		
		/* For string properties */
		private delegate void StringCallback (TagType type, IntPtr value);
		/* For integer properties */
		private delegate void IntCallback (TagType type, int value);
		/* For text data */
		private delegate void TextCallback (IntPtr value);

		[DllImport ("libbeagleglue")]
		private static extern int rpm_parse (string filename, StringCallback string_cb,
						     IntCallback int_cb, TextCallback text_cb);

		public FilterRPM ()
		{
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-rpm"));
		}

		protected override bool PullPackageProperties ()
		{
			int ret = rpm_parse (FileInfo.FullName, HandleStringProperty,
				HandleIntProperty, HandleText);
			if (ret == 0)
				return true;
			else
				return false;
		}

		private void HandleStringProperty (TagType tag, IntPtr value)
		{
			string val_str = Marshal.PtrToStringAnsi (value);
			switch (tag) {
			case TagType.NAME:
				PackageName = val_str;
				break;
			case TagType.VERSION:
				PackageVersion = val_str;
				break;
			case TagType.SUMMARY:
				Summary = val_str;
				break;
			case TagType.GROUP:
				Category = val_str;
				break;
			case TagType.LICENSE:
				License = val_str;
				break;
			case TagType.PACKAGER:
				Packager = val_str;
				break;
			case TagType.URL:
				Homepage = val_str;
				break;
			case TagType.DESCRIPTION:
				// Store the long description field as text (so will provide snippets)
				AppendText (val_str);
				AppendStructuralBreak ();
				break;
			}
		}

		private void HandleIntProperty (TagType tag, int value)
		{
			if (tag == TagType.SIZE)
				Size = value;
		}

		private void HandleText (IntPtr value)
		{
			string val_str = Marshal.PtrToStringAnsi (value);
			AppendWord (val_str);
		}
	}
}
