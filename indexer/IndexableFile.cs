//
// IndexableFile.cs
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
using System.Collections;
using System.IO;

using Beagle.Filters;
using BU = Beagle.Util;

namespace Beagle {

	public class IndexableFile : Indexable {

		bool isDirectory = false;
		Flavor flavor;
		Filter filter;
		String path;

		public IndexableFile (String _path)
		{
			path = Path.GetFullPath (_path);
			
			System.Uri uri = new System.Uri (path);
			Uri = uri.AbsoluteUri;

			Type = "File";

			if (Directory.Exists (path)) {
				isDirectory = true;
				MimeType = "inode/directory";
				Timestamp = Directory.GetLastWriteTime (path);
			} else if (File.Exists (path)) {
				flavor = Flavor.FromPath (path);
				filter = Filter.FromFlavor (flavor);
				MimeType = flavor.MimeType;		
				Timestamp = File.GetLastWriteTime (path);
			} else {
				throw new Exception ("No such file: " + path);
			}
		}

		static string[] longExtensions = {".html" };

		override protected void DoBuild ()
		{
			if (filter != null) {
				Stream stream;
				stream = new FileStream (path, FileMode.Open, FileAccess.Read);
				filter.Open (stream);
				ContentReader = filter.Content;
				HotContentReader = filter.HotContent;
				foreach (String key in filter.Keys)
					this [key] = filter [key];
				//stream.Close ();
			}

			if (isDirectory) {
				DirectoryInfo info = new DirectoryInfo (path);
				info = info.Parent;
				if (info != null) {
					this ["_Directory"] = info.FullName;
					this ["ParentSplitName"] = String.Join (" ", BU.StringFu.FuzzySplit (info.Name));
				}
			} else {
				FileInfo info = new FileInfo (path);
				this ["_Directory"] = info.DirectoryName;
				this ["ParentSplitName"] = String.Join (" ", BU.StringFu.FuzzySplit (Path.GetFileName (info.DirectoryName)));
			}

			// Try to strip off the extension in a semi-intelligent way,
			// and then fuzzy-split the file name and store it in a property.
			string name;
			bool ignoreExtension = false;
			if (path.EndsWith (".tar.gz")) {
				path = path.Substring (0, path.Length - ".tar.gz".Length);
				ignoreExtension = false;
			} else if (Path.HasExtension (path)) {
				string ext = Path.GetExtension (path);
				if (ext.Length <= 4)
					ignoreExtension = true;
				else {
					ext = ext.ToLower ();
					foreach (string str in longExtensions) {
						if (str == ext) {
							ignoreExtension = true;
							break;
						}
					}
				}
			}
			if (ignoreExtension)
				name = Path.GetFileNameWithoutExtension (path);
			else
				name = Path.GetFileName (path);
			this ["SplitName"] = String.Join (" ", BU.StringFu.FuzzySplit (name));

			this ["_NautilusEmblem"] = BU.NautilusTools.GetEmblem (path);
			this ["NautilusNotes"] = BU.NautilusTools.GetNotes (path);
		}
	}

}
