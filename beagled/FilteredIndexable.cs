//
// FilteredIndexable.cs
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
using System.IO;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class FilteredIndexable : Beagle.Indexable {

		Flavor flavor;
		Filter filter;

		public FilteredIndexable (string _uri) : base (_uri)
		{
			BuildFromFile ();
		}

		protected FilteredIndexable ()
		{
			// For instantiating from xml
		}

		public static new FilteredIndexable NewFromXml (string xml) 
		{
			FilteredIndexable indexable = new FilteredIndexable ();
			indexable.ReadFromXml (xml);
			indexable.BuildFromFile ();
			return indexable;
		}

		static string[] longExtensions = {".html" };

		private void BuildFromFile ()
		{
			Console.WriteLine ("Uri: {0}", Uri);
			if (!Uri.StartsWith ("file://")) {
				return;
			}

			bool isDirectory = false;

			string path = Uri.Substring ("file://".Length);

			if (Directory.Exists (path)) {
				isDirectory = true;
				
				if (MimeType == null)
					MimeType = "inode/directory";

				if (Timestamp == new DateTime (0))
					Timestamp = Directory.GetLastWriteTime (path);
			} else if (File.Exists (path)) {
				flavor = Flavor.FromPath (path);
				filter = Filter.FromFlavor (flavor);
				if (MimeType == null)
					MimeType = flavor.MimeType;
				if (Timestamp == new DateTime (0)) 
					File.GetLastWriteTime (path);
			} else {
				throw new Exception ("No such file: " + path);
			}

			FileInfo info = new FileInfo (path);
			if (filter != null) {
				filter.Open (info);
				foreach (Property prop in filter.Properties)
					AddProperty (prop);
			}

			string dirName = null, parentName = null;

			if (isDirectory) {
				DirectoryInfo dirInfo = new DirectoryInfo (path);
				dirInfo = dirInfo.Parent;
				if (dirInfo != null) {
					dirName = dirInfo.FullName;
					parentName = dirInfo.Name;
				}
			} else {
				dirName = info.DirectoryName;
				parentName = info.DirectoryName;
			}

			if (dirName != null)
				AddProperty (Property.NewKeyword ("fixme:directory", dirName));

			if (parentName != null) {
				string split = String.Join (" ", BU.StringFu.FuzzySplit (parentName));
				AddProperty (Property.New ("fixme:parentsplitname", split));
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
			AddProperty (Property.New ("fixme:splitname",
						      String.Join (" ", BU.StringFu.FuzzySplit (name))));


			// Attach Nautilus metadata to the file
			// FIXME: This should be in the metadata store, not attached
			// to the indexed document.

			string nautilusEmblem = BU.NautilusTools.GetEmblem (path);
			if (nautilusEmblem != null)
				AddProperty (Property.NewKeyword ("fixme:nautilus/emblem",
								     nautilusEmblem));
				
			string nautilusNotes = BU.NautilusTools.GetNotes (path);
			if (nautilusNotes != null)
				AddProperty (Property.New ("fixme:nautilus/notes", nautilusNotes));
						      

			// Check for FSpot metadata on images.
			// FIXME: This should also be in the metadata store.
			if (MimeType.StartsWith ("image/")) {
				BU.FSpotTools.Photo photo = BU.FSpotTools.GetPhoto (path);
				if (photo != null) {
					if (photo.Description != null)
						AddProperty (Property.New ("fixme:fspot/description",
									      photo.Description));

					// FIXME: This is a bit weird, since stemming is applied to
					// the list of tags. .. but I'm not sure if there is a clean way
					// to do it.
					string tagStr = "";
					foreach (BU.FSpotTools.Tag tag in photo.Tags)
						AddProperty (Property.NewKeyword ("fixme:fspot/tag", tag.Name));
				}
			}
		}

		override public TextReader GetTextReader ()
		{
			return filter != null ? filter.GetTextReader () : null;
		}

		override public TextReader GetHotTextReader ()
		{
			return filter != null ? filter.GetHotTextReader () : null;
		}

		public bool HaveFilter {
			get { return filter != null; }
		}

		public Flavor Flavor {
			get { return flavor; }
		}

		public FileInfo GetFileInfo () {
			if (Uri.StartsWith ("file://")) {
				string path = Uri.Substring ("file://".Length);
				return new FileInfo (path);
			} else {
				return null;
			}
		}
	}

}
