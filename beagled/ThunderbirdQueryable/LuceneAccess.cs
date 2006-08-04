//
// LuceneAccess.cs: Provides low level access to the underlying Lucene database
//
// Copyright (C) 2006 Pierre Östlund
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

using Lucene.Net.Documents;
using Lucene.Net.Index;
using LNS = Lucene.Net.Search;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.ThunderbirdQueryable {

	public class LuceneAccess : LuceneQueryingDriver {
		public class StoredInfo 
		{
			public DateTime LastIndex;
			public bool FullyIndexed;
			public Uri Uri;
		}
		
		public LuceneAccess (string index_name, int minor_version, bool read_only)
			: base (index_name, minor_version, read_only)
		{
		
		}
		
		public StoredInfo DocumentToStoredInfo (Document doc)
		{
			int count = 0;
			StoredInfo info = new StoredInfo ();

			info.Uri = GetUriFromDocument (doc);

			foreach (Field f in doc.Fields ()) {
				Property prop = GetPropertyFromDocument (f, doc, false);
				if (prop == null)
					continue;

				switch (prop.Key) {
				case "fixme:indexDateTime":
					info.LastIndex = StringFu.StringToDateTime (prop.Value);
					count++;
					break;
				case "fixme:fullyIndexed":
					info.FullyIndexed = Convert.ToBoolean (prop.Value);
					count++;
					break;
				}
				
				if (count == 2)
					break;
			}

			return info;
		}
		
		private class SingletonCollector : LNS.HitCollector
		{
			public int MatchId = -1;
			
			public override void Collect (int id, float score)
			{
				MatchId = id;
			}
		}
		
		public StoredInfo GetStoredInfo (Uri uri)
		{
			StoredInfo info = new StoredInfo ();

			LNS.Query query = UriQuery ("Uri", uri);
			SingletonCollector collector = new SingletonCollector ();
			
			LNS.IndexSearcher searcher = LuceneCommon.GetSearcher (PrimaryStore);
			searcher.Search (query, null, collector);
			
			if (collector.MatchId != -1) { 
				Document doc = searcher.Doc (collector.MatchId);
				info = DocumentToStoredInfo (doc);
			}
			
			LuceneCommon.ReleaseSearcher (searcher);
			
			return info;
		}
		
		public Hashtable GetStoredUriStrings (string server, string file)
		{
			Hashtable uris = new Hashtable ();

			Term term = new Term (PropertyToFieldName (PropertyType.Keyword, "fixme:file"), file);
			LNS.QueryFilter filter = new LNS.QueryFilter (new LNS.TermQuery (term));
			
			term = new Term (PropertyToFieldName (PropertyType.Keyword, "fixme:account"), server);
			LNS.TermQuery query = new LNS.TermQuery (term);
			
			LNS.IndexSearcher searcher = LuceneCommon.GetSearcher (PrimaryStore);
			LNS.Hits hits = searcher.Search (query, filter);
			
			for (int i = 0; i < hits.Length (); i++) {
				StoredInfo info = DocumentToStoredInfo (hits.Doc (i));	
				uris.Add (info.Uri.ToString (), info.FullyIndexed);
			}

			LuceneCommon.ReleaseSearcher (searcher);
			
			return uris;
		}
	}
}
