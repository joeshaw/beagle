//
// StaticQueryable.cs
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
using System.IO;
using System.Collections;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;
	
using Beagle.Util;

namespace Beagle.Daemon {

	public class StaticQueryable : LuceneQueryable 	{

		protected TextCache text_cache = null;

		public StaticQueryable (string index_name, string index_path, bool read_only_mode) : base (index_path, read_only_mode)
		{
			Logger.Log.Debug ("Initializing static queryable: {0}", index_path);

			if (Directory.Exists (Path.Combine (index_path, "TextCache"))) {
				try {
					text_cache = new TextCache (index_path, true);
				} catch (UnauthorizedAccessException) {
					Logger.Log.Warn ("Unable to purge static queryable text cache in {0}.  Will run without it.", index_path);
				}
			}
		}

		// FIXME: Move these to LuceneCommon if and when we decide to
		// support adding/removing arbitrary backends at runtime
		internal void Close ()
		{
			Log.Debug ("Removing static queryable {0}", IndexName);
			if (text_cache != null)
				text_cache.Dispose ();
			Driver.PrimaryStore.Close ();
			Driver.SecondaryStore.Close ();
			FileAttributesStore.Dispose ();
		}

#if ENABLE_RDF_ADAPTER
		protected override TextCache TextCache {
			get { return text_cache; }
		}
#endif

		override public ISnippetReader GetSnippet (string[] query_terms, Hit hit, bool full_text, int ctx_length, int snp_length) 
		{
			if (text_cache == null)
				return null;

			// Look up the hit in our local text cache.
			TextReader reader = text_cache.GetReader (hit.Uri);
			if (reader == null)
				return null;
			
			return SnippetFu.GetSnippet (query_terms, reader, full_text, ctx_length, snp_length);
		}

		override protected bool HitIsValid (Uri uri)
		{
			// We can't check anything else than file uris
			if (! uri.IsFile)
				return true;
			
			// FIXME: This is a hack, we need to support parent Uri's in some sane way
			try {
				int j = uri.LocalPath.LastIndexOf ('#');
				string actual_path = ((j == -1) ? uri.LocalPath : uri.LocalPath.Substring (0, j));
				return File.Exists (actual_path) || Directory.Exists (actual_path);
			} catch (Exception e) {
				Logger.Log.Warn ("Exception executing HitIsValid on {0}", uri.LocalPath);
				return false;
			}
		}
	}
}
