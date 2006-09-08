//
// LuceneBitArray.cs
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

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public class LuceneBitArray : BetterBitArray {

		private class BitArrayHitCollector : LNS.HitCollector {

			public BetterBitArray Array;

			public override void Collect (int id, float score)
			{
				this.Array.Set (id, true);
			}
		}

		private static bool Debug = false;

		LNS.IndexSearcher searcher;
		BitArrayHitCollector collector;
		BetterBitArray scratch;

		LNS.BooleanQuery pending_uri_query = null;
		int pending_clause_count = 0;
		static int max_clause_count;

		static LuceneBitArray ()
		{
			max_clause_count = LNS.BooleanQuery.GetMaxClauseCount ();
		}

		public LuceneBitArray (LNS.IndexSearcher searcher) : base (searcher.MaxDoc ())
		{
			this.searcher = searcher;
			this.collector = new BitArrayHitCollector ();
			this.scratch = null;
		}

		public LuceneBitArray (LNS.IndexSearcher searcher,
				       LNS.Query query) : this (searcher)
		{
			this.Or (query);
		}

		private void UseScratch ()
		{
			if (scratch == null)
				scratch = new BetterBitArray (searcher.MaxDoc ());
			else
				scratch.SetAll (false);
			collector.Array = scratch;
		}

		public LuceneBitArray Search (LNS.Query query)
		{
			this.SetAll (false);
			this.Or (query);
			return this;
		}

		public LuceneBitArray And (LNS.Query query)
		{
			UseScratch ();
			searcher.Search (query, null, collector);
			if (Debug)
				Explain (query);
			this.And (scratch);
			return this;
		}

		public LuceneBitArray AndNot (LNS.Query query)
		{
			UseScratch ();
			searcher.Search (query, null, collector);
			if (Debug)
				Explain (query);
			this.AndNot (scratch);
			return this;
		}

		public LuceneBitArray Or (LNS.Query query)
		{
			collector.Array = this;
			searcher.Search (query, null, collector);
			if (Debug)
				Explain (query);
			return this;
		}
		
		public LuceneBitArray Xor (LNS.Query query)
		{
			UseScratch ();
			searcher.Search (query, null, collector);
			if (Debug)
				Explain (query);
			this.Xor (scratch);
			return this;
		}

		private void Explain (LNS.Query query)
		{
			int j = 0;
			while (j < collector.Array.Count) {
				int i;
				i = collector.Array.GetNextTrueIndex (j);
				if (i >= collector.Array.Count)
					break;
				j = i + 1;

				Document doc = searcher.Doc (i);
				LNS.Explanation exp = searcher.Explain (query, i);

				Log.Debug ("Query: [{0}]", query);
				Log.Debug ("Matching URI: {0}", doc.Get ("Uri"));
				Log.Debug ("Explanation: {0}", exp);
			}
		}

		////////////////////////////////////////////////////////////

		public void AddUri (Uri uri)
		{
			AddUri (UriFu.UriToEscapedString (uri));
		}

		public void AddUri (string str)
		{
			Term term;
			term = new Term ("Uri", str);

			LNS.TermQuery q;
			q = new LNS.TermQuery (term);

			if (pending_uri_query == null)
				pending_uri_query = new LNS.BooleanQuery ();
			pending_uri_query.Add (q, false, false);
			++pending_clause_count;

			if (pending_clause_count == max_clause_count)
				FlushUris ();
		}

		public void FlushUris ()
		{
			if (pending_uri_query != null) {
				this.Or (pending_uri_query);
				pending_uri_query = null;
				pending_clause_count = 0;
			}
		}

		////////////////////////////////////////////////////////////

		public void ProjectOnto (LuceneBitArray other)
		{
			int j = 0;
			while (j < this.Count) {
				int i;
				i = this.GetNextTrueIndex (j);
				if (i >= this.Count)
					break;
				j = i+1;

				Document doc;
				doc = searcher.Doc (i);

				other.AddUri (doc.Get ("Uri"));
			}

			other.FlushUris ();
		}

		public void Join (LuceneBitArray other)
		{
			LuceneBitArray image;
			image = new LuceneBitArray (other.searcher);
			this.ProjectOnto (image);

			other.Or (image);

			// We only need to project back items in the other
			// bit array that are not in the image of the
			// first projection.
			image.Not ();
			image.And (other);
			image.ProjectOnto (this);
		}
	}
}
