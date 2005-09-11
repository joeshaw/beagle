//
// LuceneNameResolver.cs
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

using Lucene.Net.Documents;
using Lucene.Net.Index;
using LNS = Lucene.Net.Search;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	//
	// This is just a LuceneQueryingDriver with the ability to do the
	// special secondary-index-only queries we need to map internal uris
	// back to filenames.
	//

	public class LuceneNameResolver : LuceneQueryingDriver {

		public class NameInfo {
			public Guid   Id;
			public Guid   ParentId;
			public string Name;
			public bool   IsDirectory;
		}

		public LuceneNameResolver (string index_name, int minor_version, bool read_only)
			: base (index_name, minor_version, read_only)
		{

		}

		////////////////////////////////////////////////////////////////


		private NameInfo DocumentToNameInfo (Document doc)
		{
			NameInfo info;
			info = new NameInfo ();

			string str;
			str = doc.Get ("Uri");
			info.Id = GuidFu.FromUriString (str);

			bool have_name = false;
			bool have_parent_id = false;
			bool have_is_dir = false;

			foreach (Field f in doc.Fields ()) {
				Property prop;
				prop = GetPropertyFromDocument (f, doc, false);
				if (prop == null)
					continue;

				switch (prop.Key) {
					
				case FileSystemQueryable.ExactFilenamePropKey:
					info.Name = prop.Value;
					have_name = true;
					break;
					
				case FileSystemQueryable.ParentDirUriPropKey:
					info.ParentId = GuidFu.FromUriString (prop.Value);
					have_parent_id = true;
					break;

				case FileSystemQueryable.IsDirectoryPropKey:
					info.IsDirectory = (prop.Value == "true");
					have_is_dir = true;
					break;
				}

				if (have_name && have_parent_id && have_is_dir)
					break;
			}

			return info;
		}

		////////////////////////////////////////////////////////////////

		// Pull a single record out of the index

		private class SingletonCollector : LNS.HitCollector {
			
			public int MatchId = -1;

			public override void Collect (int id, float score)
			{
				if (MatchId != -1)
					Logger.Log.Error ("Duplicate name found: replacing MatchId {0} with {1}",
							  MatchId, id);

				MatchId = id;
			}
		}

		public NameInfo GetNameInfoById (Guid id)
		{
			Uri uri;
			uri = GuidFu.ToUri (id);
			
			LNS.Query query;
			query = UriQuery ("Uri", uri);
			
			SingletonCollector collector;
			collector = new SingletonCollector ();

			LNS.IndexSearcher searcher;
			searcher = new LNS.IndexSearcher (SecondaryStore);
			searcher.Search (query, null, collector);

			NameInfo info = null;

			if (collector.MatchId != -1) {
				Document doc;
				doc = searcher.Doc (collector.MatchId);
				info = DocumentToNameInfo (doc);
			}

			searcher.Close ();
			
			return info;
		}

		////////////////////////////////////////////////////////////////

		public Guid GetIdByNameAndParentId (string name, Guid parent_id)
		{
			string parent_uri_str;
			parent_uri_str = GuidFu.ToUriString (parent_id);

			string key1;
			key1 = PropertyToFieldName (PropertyType.Keyword, FileSystemQueryable.ParentDirUriPropKey);

			string key2;
			key2 = PropertyToFieldName (PropertyType.Keyword, FileSystemQueryable.ExactFilenamePropKey);
						    
			LNS.Query q1;
			q1 = new LNS.TermQuery (new Term (key1, parent_uri_str));
			
			LNS.Query q2;
			q2 = new LNS.TermQuery (new Term (key2, name));

			LNS.BooleanQuery query;
			query = new LNS.BooleanQuery ();
			query.Add (q1, true, false);
			query.Add (q2, true, false);

			SingletonCollector collector;
			collector = new SingletonCollector ();

			LNS.IndexSearcher searcher;
			searcher = new LNS.IndexSearcher (SecondaryStore);
			searcher.Search (query, null, collector);

			Guid id;
			if (collector.MatchId != -1) {
				Document doc;
				doc = searcher.Doc (collector.MatchId);
				id = GuidFu.FromUriString (doc.Get ("Uri"));
			} else 
				id = Guid.Empty;

			searcher.Close ();

			return id;
		}

		////////////////////////////////////////////////////////////////

		// Pull all of the directories out of the index and cache them

		// Not to be confused with LuceneQueryingDriver.BitArrayHitCollector
		private class BitArrayHitCollector : LNS.HitCollector {

			private BetterBitArray matches;
			
			public BitArrayHitCollector (BetterBitArray matches)
			{
				this.matches = matches;
			}

			public override void Collect (int id, float score)
			{
				matches [id] = true;
			}
		}

		public ICollection GetAllDirectoryNameInfo ()
		{
			// First we assemble a query to find all of the directories.
			string field_name;
			field_name = PropertyToFieldName (PropertyType.Keyword,
							  FileSystemQueryable.IsDirectoryPropKey);
			
			LNS.Query query;
			query = new LNS.TermQuery (new Term (field_name, "true"));

			// Then we actually run the query
			LNS.IndexSearcher searcher;
			searcher = new LNS.IndexSearcher (SecondaryStore);

			BetterBitArray matches;
			matches = new BetterBitArray (searcher.MaxDoc ());

			BitArrayHitCollector collector;
			collector = new BitArrayHitCollector (matches);

			searcher.Search (query, null, collector);
			
			// Finally we pull all of the matching documents,
			// convert them to NameInfo, and store them in a list.

			ArrayList match_list = new ArrayList ();
			int i = 0;
			while (i < matches.Count) {
				
				i = matches.GetNextTrueIndex (i);
				if (i >= matches.Count)
					break;

				Document doc;
				doc = searcher.Doc (i);

				NameInfo info;
				info = DocumentToNameInfo (doc);

				match_list.Add (info);

				++i;
			}

			return match_list;
		}
	}
}
