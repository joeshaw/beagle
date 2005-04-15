//
// FilterFactory.cs
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
using System.Reflection;

using Beagle.Util;

namespace Beagle.Daemon {

	public class FilterFactory {

		static FilterFactory ()
		{
			string path = Environment.GetEnvironmentVariable ("BEAGLE_FILTER_PATH");
			
			if (path == null || path == "")
				path = PathFinder.FilterDir;
			else if (path [path.Length-1] == ':')
				path += PathFinder.FilterDir;

			Hashtable seen = new Hashtable ();

			foreach (string dir in path.Split (':')) {
				if (! seen.Contains (dir))
					ScanDirectoryForAssemblies (dir);
				seen [dir] = true;
			}
		}

		/////////////////////////////////////////////////////////////////////////
		
		static Hashtable mime_type_table = new Hashtable ();
		static Hashtable extension_table = new Hashtable ();

		static private void RegisterMimeType (string mime_type, Type filter_type)
		{
			// FIXME: Should mime types be case-sensitive?
			mime_type = mime_type.ToLower ();

			if (mime_type_table.Contains (mime_type)) {
				Type current_type = (Type) mime_type_table [mime_type];
				if (current_type != filter_type)
					Logger.Log.Error ("Filter collision: mime type={0} ({1} vs {2})",
							  mime_type, current_type, filter_type);

				// If we try to re-register the same filter for the same mime type,
				// just silently return.
				return;
			}

			mime_type_table [mime_type] = filter_type;
		}

		static private void RegisterExtension (string extension, Type filter_type)
		{
			// FIXME: Should mime types be case-sensitive?
			extension = extension.ToLower ();

			if (extension [0] != '.')
				extension = "." + extension;

			if (extension_table.Contains (extension)) {
				Type current_type = (Type) extension_table [extension];
				if (current_type != filter_type)
					Logger.Log.Error ("Filter collision: extension={0} ({1} vs {2})",
							  extension, current_type, filter_type);

				// If we try to re-register the same filter for the same extension,
				// just silently return.
				return;
			}

			extension_table [extension] = filter_type;
		}

		static private Filter CreateFilter (string mime_type, string extension)
		{
			Type filter_type = null;
			string this_mime_type = null;
			string this_extension = null;

			// First, try looking it up by extension.
			// For example, a ".js" file is identified as "text/plain" by gnome-vfs,
			// which will improperly create an instance of Text filter.
			if (extension != null && extension.Length > 0) {
				extension = extension.ToLower ();
				if (extension [0] != '.')
					extension = "." + extension;
				filter_type = (Type) extension_table [extension];
				if (filter_type != null)
					this_extension = extension;
			}
			
			// If that didn't work, look it up by the mime-type.
			if (filter_type == null && mime_type != null && mime_type.Length > 0) {
				mime_type = mime_type.ToLower ();
				filter_type = (Type) mime_type_table [mime_type];
				if (filter_type != null)
					this_mime_type = mime_type;
			}

			Filter filter = null;
			if (filter_type != null) {
				filter = (Filter) Activator.CreateInstance (filter_type);
				if (this_mime_type != null)
					filter.MimeType = this_mime_type;
				if (this_extension != null)
					filter.Extension = this_extension;
			}

			return filter;
		}

		static public Filter CreateFilterFromMimeType (string mime_type)
		{
			return CreateFilter (mime_type, null);
		}

		static public Filter CreateFilterFromExtension (string extension)
		{
			return CreateFilter (null, extension);
		}

		static public Filter CreateFilterFromPath (string path)
		{
			string guessed_mime_type = Beagle.Util.VFS.Mime.GetMimeType (path);
			string extension = Path.GetExtension (path);
			return CreateFilter (guessed_mime_type, extension);
		}

		/////////////////////////////////////////////////////////////////////////

		static private int ScanAssemblyForFilters (Assembly assembly)
		{
			int count = 0;

			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (Filter)) && ! t.IsAbstract) {
					Filter filter = null;
					try {
						filter = (Filter) Activator.CreateInstance (t);
					} catch (Exception ex) {
						Logger.Log.Error ("Caught exception while instantiating {0}", t);
						Logger.Log.Error (ex);
					}

					if (filter == null)
						continue;

					++count;

					foreach (string mime_type in filter.SupportedMimeTypes)
						RegisterMimeType (mime_type, t);

					foreach (string extension in filter.SupportedExtensions)
						RegisterExtension (extension, t);
				}
			}

			return count;
		}

		static private void ScanDirectoryForAssemblies (string dir)
		{
			if (dir == null || dir == "")
				return;

			if (! Directory.Exists (dir)) {
				Logger.Log.Debug ("'{0}' is not a directory: No filters loaded", dir);
				return;
			}
			
			DirectoryInfo dir_info = new DirectoryInfo (dir);
			foreach (FileInfo file_info in dir_info.GetFiles ()) {
				if (file_info.Extension == ".dll") {
					Assembly a = Assembly.LoadFrom (file_info.FullName);
					int n = ScanAssemblyForFilters (a);
					Logger.Log.Debug ("Loaded {0} filter{1} from {2}",
							  n, n == 1 ? "" : "s", file_info.FullName);
				}
			}
		}
	}
}
