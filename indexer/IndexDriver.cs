
using System;
using System.Collections;
using System.IO;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers;

namespace Dewey {

    public class IndexDriver {
	
	public IndexDriver() { }

	private String IndexPath {
	    get {
		String homedir = Environment.GetEnvironmentVariable("HOME");
		return Path.Combine(homedir, ".dewey");
	    }
	}

	private Analyzer NewAnayzer() {
	    return new StandardAnalyzer();
	}

	private void DoDelete(IEnumerable list_of_ids) {
	    IndexReader reader = IndexReader.Open(IndexPath);
	    foreach (int id in list_of_ids) {
		reader.Delete(id);
	    }
	    reader.Close();
	}

	private void DoInsert(IEnumerable list_of_items) {
	    Analyzer analyzer = NewAnayzer();
	    IndexWriter writer = new IndexWriter(IndexPath, analyzer, false);
	    
	    foreach (IndexItemWithPayload item in list_of_items) {
		Console.WriteLine("Indexing " + item.URI);
		Document doc = item.ToDocument();
		writer.AddDocument(doc);
	    }

	    writer.Optimize();
	    writer.Close();
	}

	// Add a set of items to the index
	public void Add(IEnumerable items) {

	    if (! Directory.Exists(IndexPath)) {
		Directory.CreateDirectory(IndexPath);
		// Initialize the index
		IndexWriter writer = new IndexWriter(IndexPath, null, true);
		writer.Close();
	    }

	    ArrayList to_be_deleted = new ArrayList();
	    ArrayList to_be_inserted = new ArrayList();

	    Searcher searcher = new IndexSearcher(IndexPath);
	    
	    foreach (IndexItemWithPayload item in items) {

		Query uri_query = new TermQuery(new Term("URI", item.URI));
		Hits uri_hits = searcher.Search(uri_query);
		int nHits = uri_hits.Length();

		if (nHits > 1) {
		    // FIXME: This shouldn't happen, so do something sane.
		    Console.WriteLine("Something bad happened!");
		} else if (nHits == 1) {
		    IndexItem old_item = new IndexItem(uri_hits.Doc(0));
		    int old_id = uri_hits.Id(0);
		    
		    if (old_item.IsSupercededBy(item)) {
			to_be_deleted.Add(old_id);
			to_be_inserted.Add(item);
			Console.WriteLine("Re-scheduling " + item.URI);
		    } else {
			Console.WriteLine("Skipping " + item.URI);
		    }
		    
		} else {
		    to_be_inserted.Add(item);
		    Console.WriteLine("Scheduling " + item.URI);
		}
	    }

	    if (to_be_deleted.Count > 0)
		DoDelete(to_be_deleted);

	    if (to_be_inserted.Count > 0)
		DoInsert(to_be_inserted);
	}

	// Add a single item to the index
	public void Add(IndexItemWithPayload item) {
	    Add(new IndexItemWithPayload[] { item });
	}

	public IndexItem[] QueryBody(String query_str) {
	    Searcher searcher = new IndexSearcher(IndexPath);
	    Analyzer analyzer = NewAnayzer();
	    Query query = QueryParser.Parse(query_str, "Body", analyzer);
	    Hits hits = searcher.Search(query);

	    IndexItem[] item_hits = new IndexItem[hits.Length()];

	    for (int i = 0; i < hits.Length(); ++i) {
		item_hits[i] = new IndexItem(hits.Doc(i));
	    }

	    return item_hits;
	}

    }

}
