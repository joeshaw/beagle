//
// LuceneNameResolver.cs
//
// Copyright (C) 2005-2007 Novell, Inc.
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
using System.Collections.Generic;
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
					
				case Property.ExactFilenamePropKey:
					info.Name = prop.Value;
					have_name = true;
					break;
					
				case Property.ParentDirUriPropKey:
					info.ParentId = GuidFu.FromUriString (prop.Value);
					have_parent_id = true;
					break;

				case Property.IsDirectoryPropKey:
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

		public NameInfo GetNameInfoById (Guid id)
		{
			Uri uri;
			uri = GuidFu.ToUri (id);

			IndexReader reader;
			reader = LuceneCommon.GetReader (SecondaryStore);

			TermDocs term_docs;
			term_docs = reader.TermDocs ();

			Term term = new Term ("Uri", UriFu.UriToEscapedString (uri));
			term_docs.Seek (term);

			int match_id = -1;
			if (term_docs.Next ())
				match_id = term_docs.Doc ();

			term_docs.Close ();

			NameInfo info = null;

			if (match_id != -1) {
				Document doc;
				doc = reader.Document (match_id);
				info = DocumentToNameInfo (doc);
			}

			LuceneCommon.ReleaseReader (reader);
			
			return info;
		}

		////////////////////////////////////////////////////////////////

		public Guid GetIdByNameAndParentId (string name, Guid parent_id)
		{
			string parent_uri_str;
			parent_uri_str = GuidFu.ToUriString (parent_id);

			string key1, key2;

			key1 = PropertyToFieldName (PropertyType.Keyword, Property.ParentDirUriPropKey);
			key2 = PropertyToFieldName (PropertyType.Keyword, Property.ExactFilenamePropKey);

			Term term1, term2;

			term1 = new Term (key1, parent_uri_str);
			term2 = new Term (key2, name.ToLower ());

			// Lets walk the exact file name terms first (term2)
			// since there are probably fewer than parent directory
			// Uri terms.
			List <int> term2_doc_ids = new List <int> ();

			IndexReader reader = LuceneCommon.GetReader (SecondaryStore);
			TermDocs term_docs = reader.TermDocs ();

			term_docs.Seek (term2);
			while (term_docs.Next ())
				term2_doc_ids.Add (term_docs.Doc ());

			Log.Debug ("Found {0} docs for term {1}", term2_doc_ids.Count, name.ToLower ());

			term_docs.Seek (term1);
			
			int match_id = -1;

			while (term_docs.Next ()) {
				int doc_id = term_docs.Doc ();

				if (term2_doc_ids.BinarySearch (doc_id) >= 0) {
					match_id = doc_id;
					break;
				}
			}

			term_docs.Close ();

			Guid id;
			if (match_id != -1) {
				Document doc;
				doc = reader.Document (match_id);
				id = GuidFu.FromUriString (doc.Get ("Uri"));
			} else 
				id = Guid.Empty;

			LuceneCommon.ReleaseReader (reader);

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
							  Property.IsDirectoryPropKey);
			
			LNS.Query query;
			query = new LNS.TermQuery (new Term (field_name, "true"));

			// Then we actually run the query
			LNS.IndexSearcher searcher;
			//searcher = new LNS.IndexSearcher (SecondaryStore);
			searcher = LuceneCommon.GetSearcher (SecondaryStore);

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

			LuceneCommon.ReleaseSearcher (searcher);

			return match_list;
		}
	}
}
