//
// NameIndexTool.cs
// Copyright (C) 2005 Novell, Inc.
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

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

class NameIndexTool {

		static void Main (string [] args)
		{
			NameIndex name_index;

			name_index = new NameIndex (Path.Combine (PathFinder.IndexDir, "FileSystemIndex"), null);

			if (args.Length == 0) {
				name_index.SpewIndex ();
			} else {
				Query query = new Query ();
				foreach (string arg in args)
					query.AddTextRaw (arg);
				foreach (Uri uri in name_index.Search (query, null))
					Console.WriteLine (GuidFu.ToShortString (GuidFu.FromUri (uri)));
			}
		}
}
