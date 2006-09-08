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

using System.Xml;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	public class FilterFactory {

		static private bool Debug = false;

		static FilterFactory ()
		{
			ReflectionFu.ScanEnvironmentForAssemblies ("BEAGLE_FILTER_PATH", PathFinder.FilterDir,
								   delegate (Assembly a) {
									   int n = ScanAssemblyForFilters (a);
									   Logger.Log.Debug ("Loaded {0} filter{1} from {2}",
											     n, n == 1 ? "" : "s", a.Location);
								   });
		}

		/////////////////////////////////////////////////////////////////////////
		

		static private ICollection CreateFilters (Uri uri, string extension, string mime_type)
		{
			Hashtable matched_filters_by_flavor = FilterFlavor.NewHashtable ();
			
			foreach (FilterFlavor flavor in filter_types_by_flavor.Keys) {
				if (flavor.IsMatch (uri, extension, mime_type)) {
					Filter matched_filter = null;

					try {
						matched_filter = (Filter) Activator.CreateInstance ((Type) filter_types_by_flavor [flavor]);

						if (flavor.MimeType != null)
							matched_filter.MimeType = flavor.MimeType;
						if (flavor.Extension != null)
							matched_filter.Extension = flavor.Extension;

					} catch (Exception e) {
						continue;
					}
					matched_filters_by_flavor [flavor] = matched_filter;
				}
			}

			foreach (DictionaryEntry entry in matched_filters_by_flavor) {
				FilterFlavor flav = (FilterFlavor) entry.Key;
				Filter filter = (Filter) entry.Value;
				
				if (Debug)
					Logger.Log.Debug ("Found matching filter: {0}, Weight: {1}", filter, flav.Weight);
			}

			return matched_filters_by_flavor.Values;
		}

		static public int GetFilterVersion (string filter_name) 
		{
			if (filter_versions_by_name.Contains (filter_name)) {
				return (int) filter_versions_by_name [filter_name];
			} else {
				return -1;
			}
		}

		/////////////////////////////////////////////////////////////////////////

		static public ICollection CreateFiltersFromMimeType (string mime_type)
		{
			return CreateFilters (null, null, mime_type);
		}

		static public ICollection CreateFilterFromExtension (string extension)
		{
			return CreateFilters (null, extension, null);
		}

		static public ICollection CreateFiltersFromPath (string path)
		{
			string guessed_mime_type = XdgMime.GetMimeType (path);
			string extension = Path.GetExtension (path);
			return CreateFilters (UriFu.PathToFileUri (path), extension, guessed_mime_type);
		}

		static public ICollection CreateFiltersFromUri (Uri uri)
		{
			if (uri.IsFile)
				return CreateFiltersFromPath (uri.LocalPath);
			else
				return CreateFilters (uri, null, null);
		}

		static public ICollection CreateFiltersFromIndexable (Indexable indexable)
		{
			string path = indexable.ContentUri.LocalPath;
			string extension = Path.GetExtension (path);
			string mime_type = indexable.MimeType;
			return CreateFilters (UriFu.PathToFileUri (path), extension, mime_type);
		}

		/////////////////////////////////////////////////////////////////////////

		static private bool ShouldWeFilterThis (Indexable indexable)
		{
			if (indexable.Filtering == IndexableFiltering.Never
			    || indexable.NoContent)
				return false;

			if (indexable.Filtering == IndexableFiltering.Always)
				return true;

			// Our default behavior is to try to filter non-transient file
			// indexable and indexables with a specific mime type attached.
			if (indexable.IsNonTransient || indexable.MimeType != null)
				return true;
			
			return false;
		}

		static public bool FilterIndexable (Indexable indexable, TextCache text_cache, out Filter filter)
		{
			filter = null;
			ICollection filters = null;

			if (indexable.Filtering == IndexableFiltering.AlreadyFiltered)
				return false;

			if (! ShouldWeFilterThis (indexable)) {
				indexable.NoContent = true;
				return false;
			}

			string path = null;

			// First, figure out which filter we should use to deal with
			// the indexable.

			// If a specific mime type is specified, try to index as that type.
			if (indexable.MimeType != null)
				filters = CreateFiltersFromMimeType (indexable.MimeType);

			if (indexable.ContentUri.IsFile) {
				path = indexable.ContentUri.LocalPath;

				// Otherwise sniff the mime-type from the file
				if (indexable.MimeType == null)
					indexable.MimeType = XdgMime.GetMimeType (path);

				if (filters == null || filters.Count == 0) {
					filters = CreateFiltersFromIndexable (indexable);
				}

				if (Directory.Exists (path)) {
					indexable.MimeType = "inode/directory";
					indexable.NoContent = true;
					if (! indexable.ValidTimestamp)
						indexable.Timestamp = Directory.GetLastWriteTimeUtc (path);
				} else if (File.Exists (path)) {
					// Set the timestamp to the best possible estimate (if no timestamp was set by the backend)
					if (! indexable.ValidTimestamp)
						indexable.Timestamp = File.GetLastWriteTimeUtc (path);
				} else {
					Logger.Log.Warn ("No such file: {0}", path);
					return false;
				}
			}

			// We don't know how to filter this, so there is nothing else to do.
			if (filters.Count == 0) {
				if (! indexable.NoContent) {
					indexable.NoContent = true;

					Logger.Log.Debug ("No filter for {0} ({1})", path != null ? path : indexable.Uri.ToString (), indexable.MimeType);
					return false;
				}
				
				return true;
			}

			foreach (Filter candidate_filter in filters) {
				if (Debug)
					Logger.Log.Debug ("Testing filter: {0}", candidate_filter);
				
				// Hook up the snippet writer.
				if (candidate_filter.SnippetMode && text_cache != null) {
					if (candidate_filter.OriginalIsText && indexable.IsNonTransient) {
						text_cache.MarkAsSelfCached (indexable.Uri);
					} else if (indexable.CacheContent) {
						TextWriter writer = text_cache.GetWriter (indexable.Uri);
						candidate_filter.AttachSnippetWriter (writer);
					}
				}

				if (indexable.Crawled)
					candidate_filter.EnableCrawlMode ();
				
				// Set the filter's URI
				candidate_filter.Uri = indexable.Uri;

				// allow the filter access to the indexable's properties
				candidate_filter.IndexableProperties = indexable.Properties;
				
				// Open the filter, copy the file's properties to the indexable,
				// and hook up the TextReaders.

				bool succesful_open = false;
				TextReader text_reader;
				Stream binary_stream;

				if (path != null)
					succesful_open = candidate_filter.Open (path);
				else if ((text_reader = indexable.GetTextReader ()) != null)
					succesful_open = candidate_filter.Open (text_reader);
				else if ((binary_stream = indexable.GetBinaryStream ()) != null)
					succesful_open = candidate_filter.Open (binary_stream);
					
				if (succesful_open) {
					foreach (Property prop in candidate_filter.Properties)
						indexable.AddProperty (prop);
					indexable.SetTextReader (candidate_filter.GetTextReader ());
					indexable.SetHotTextReader (candidate_filter.GetHotTextReader ());

					if (Debug)
						Logger.Log.Debug ("Successfully filtered {0} with {1}", path, candidate_filter);

					filter = candidate_filter;
					return true;
				} else if (Debug) {
					Logger.Log.Debug ("Unsuccessfully filtered {0} with {1}, falling back", path, candidate_filter);
					candidate_filter.Cleanup ();
				}
			}

			if (Debug)
				Logger.Log.Debug ("None of the matching filters could process the file: {0}", path);
			
			return false;
		}

		static public bool FilterIndexable (Indexable indexable, out Filter filter)
		{
			return FilterIndexable (indexable, null, out filter);
		}

		static public bool FilterIndexable (Indexable indexable)
		{
			Filter filter = null;

			return FilterIndexable (indexable, null, out filter);
		}

		/////////////////////////////////////////////////////////////////////////

		private static Hashtable filter_types_by_flavor = new Hashtable ();
		private static Hashtable filter_versions_by_name = new Hashtable ();

		static private int ScanAssemblyForFilters (Assembly assembly)
		{
			int count = 0;

			foreach (Type t in ReflectionFu.ScanAssemblyForClass (assembly, typeof (Filter))) {
				Filter filter = null;

				try {
					filter = (Filter) Activator.CreateInstance (t);
				} catch (Exception ex) {
					Logger.Log.Error (ex, "Caught exception while instantiating {0}", t);
				}

				if (filter == null)
					continue;

				filter_versions_by_name [t.ToString ()] = filter.Version;

				foreach (FilterFlavor flavor in filter.SupportedFlavors) {
					filter_types_by_flavor [flavor] = t;
					FilterFlavor.Flavors.Add (flavor);
				}

				++count;
			}

			return count;
		}
		
	}

	/////////////////////////////////////////////////////////////////////////

	public class FilteredStatus 
	{
		private Uri uri;
		private string filter_name;
		private int filter_version;

		[XmlIgnore]
		public Uri Uri {
			get { return uri; }
			set { uri = value; }
		}

		[XmlAttribute ("Uri")]
		public string UriAsString {
			get {
				return UriFu.UriToEscapedString (uri);
			}

			set {
				uri = UriFu.EscapedStringToUri (value);
			}
		}

		public string FilterName {
			get { return filter_name; }
			set { filter_name = value; }
		}

		public int FilterVersion {
			get { return filter_version; }
			set { filter_version = value; }
		}

		public static FilteredStatus New (Indexable indexable, Filter filter)
		{
			FilteredStatus status = new FilteredStatus ();

			status.Uri = indexable.Uri;
			status.FilterName = filter.GetType ().ToString ();
			status.FilterVersion = filter.Version;

			return status;
		}
	}
}
