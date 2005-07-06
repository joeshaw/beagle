//
// FilterDirectory.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.IO;
using System.Text;
using System.Diagnostics;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {

	public class FilterDirectory : FilterDesktop {

		public FilterDirectory ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("x-directory/normal"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("inode/directory"));
		}

		override protected void DoOpen (DirectoryInfo dir)
		{
			FileInfo file = new FileInfo (Path.Combine (dir.FullName, ".directory"));

			if (!file.Exists) {
				Logger.Log.Debug ("No directory meta-data file found for directory: {0}", dir.FullName);
				Finished ();
				return;
			}
				
			try {
				reader = new StreamReader (file.FullName);
			} catch (Exception e) {
				Logger.Log.Debug ("Could not open directory meta-data file, not filtering: {0}", dir.FullName);
				Error ();
				return;
			}
		}
	}
}
