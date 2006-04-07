//
// DumpIndex.cs
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
using System.Net;

using Beagle;
using Beagle.Util;
using Beagle.Daemon;

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Documents;

class DumpIndexTool {

	public class HitByUriComparer : IComparer {

		public int Compare (object a, object b)
		{
			// All of this mapping to and from strings is dreadful.
			return String.Compare (((Hit) a).Uri.ToString (), ((Hit) b).Uri.ToString ());
		}
	}

	static string RemapUriToPath (Hashtable all_hits_by_uri, Hit hit)
	{
		string exact_name = hit.GetFirstProperty ("beagle:ExactFilename");
		string parent_uri_str = hit.GetFirstProperty ("_private:ParentDirUri");

		if (parent_uri_str == null)
			return exact_name;
		else
			return Path.Combine (RemapUriToPath (all_hits_by_uri, (Hit) all_hits_by_uri [parent_uri_str]),
					     exact_name);
	}

	static int DumpOneIndex_Metadata (string index_name, bool only_dump_the_urls)
	{
		Console.WriteLine (); // a visual cue that something has changed
		LuceneQueryingDriver driver;
		driver = new LuceneQueryingDriver (index_name, -1, true);
		
		Hashtable all_hits_by_uri;
		all_hits_by_uri = driver.GetAllHitsByUri ();

		ArrayList all_hits;
		all_hits = new ArrayList (all_hits_by_uri.Values);

		if (index_name == "FileSystemIndex") // A hard-wired hack
			foreach (Hit hit in all_hits)
				hit.Uri = UriFu.PathToFileUri (RemapUriToPath (all_hits_by_uri, hit));

		all_hits.Sort (new HitByUriComparer ());

		foreach (Hit hit in all_hits) {

			if (only_dump_the_urls) {
				Console.WriteLine ("{0}: {1}", index_name, hit.Uri);
				continue;
			}

			Console.WriteLine (" Index: {0}", index_name);
			Console.WriteLine ("   Uri: {0}", hit.Uri);
			if (hit.ParentUri != null)
				Console.WriteLine ("Parent: {0}", hit.ParentUri);
			Console.WriteLine (" MimeT: {0}", hit.MimeType);
			Console.WriteLine ("  Type: {0}", hit.Type);

			ArrayList props;
			props = new ArrayList (hit.Properties);
			props.Sort ();
			foreach (Property prop in props)
				if (! prop.Key.StartsWith ("_private:"))
				    Console.WriteLine ("  Prop: {0} = '{1}'", prop.Key, prop.Value);

			Console.WriteLine ();
		}

		return all_hits.Count;
	}

	static Term initial_enum_term;
	// Dump the term frequencies: we do this via direct Lucene access.
	static void DumpOneIndex_TermFrequencies (string index_name)
	{
		LuceneQueryingDriver driver;
		driver = new LuceneQueryingDriver (index_name, -1, true);
		
		IndexReader reader;
		reader = IndexReader.Open (driver.PrimaryStore);

		TermEnum term_enum;
		term_enum = reader.Terms (initial_enum_term);

		int distinct_term_count = 0;
		int term_count = 0;

		// from LuceneFAQ
		// Terms are sorted first by field, then by text
		// so all terms with a given field are adjacent in enumerations.
		if (term_enum.Term () != null) {
			while (term_enum.Term().Field() == "Text") {
				int freq;
				freq = term_enum.DocFreq ();

				Console.WriteLine ("{0} {1} {2}", index_name, term_enum.Term ().Text (), freq);

				// FIXME: spew these as a count
				++distinct_term_count;
				term_count += freq;

				if (!term_enum.Next ())
					break;
			}
		}

		term_enum.Close ();
		reader.Close ();

		Console.WriteLine ();
	}

	public class IndexInfo : IComparable {
		public string Name;
		public int    Count;

		public IndexInfo (string name)
		{
			Name = name;
		}

		public int CompareTo (object obj)
		{
			IndexInfo other = (IndexInfo) obj;
			return String.Compare (this.Name, other.Name);
		}
	}

	static void DumpIndexInformation (Mode mode, bool show_counts)
	{
		ArrayList index_info_list;
		index_info_list = new ArrayList ();

		DirectoryInfo dir;
		dir = new DirectoryInfo (PathFinder.IndexDir);
		foreach (DirectoryInfo subdir in dir.GetDirectories ())
			index_info_list.Add (new IndexInfo (subdir.Name));
		
		index_info_list.Sort ();

		bool set_counts = false;
		
		if (mode == Mode.TermFrequencies)
			initial_enum_term = new Term ("Text", "");

		foreach (IndexInfo info in index_info_list) {
			if (mode == Mode.Uris || mode == Mode.Properties) {
				info.Count = DumpOneIndex_Metadata (info.Name, mode == Mode.Uris);
				set_counts = true;
			} else {
				DumpOneIndex_TermFrequencies (info.Name);
			}
		}

		if (show_counts && set_counts) {
			Console.WriteLine ();
			Console.WriteLine ("FINAL COUNTS");

			foreach (IndexInfo info in index_info_list) 
				Console.WriteLine ("{0} {1}", info.Count.ToString ().PadLeft (7), info.Name);
		}
	}

	class DummyQueryResult : IQueryResult {
		public void Add (ICollection hits)
		{
		}

		public void Subtract (ICollection hits)
		{
		}
	}

	static void DumpFileIndexInformation (string path, string indexdir)
	{
		//Uri uri = UriFu.PathToFileUri (path);
	    	//Console.WriteLine ("Dumping information about:" + uri.AbsolutePath);
	    	//path = uri.AbsolutePath;
	    	if ((! File.Exists (path)) && (! Directory.Exists (path))) {
	    	        Console.WriteLine ("No such file or directory:" + path);
	    	        return;
	    	}

		if (indexdir == null)
			// default is ~/.beagle/Indexes/FileSystemIndex
			indexdir = Path.Combine (PathFinder.IndexDir, "FileSystemIndex");
		if (! Directory.Exists (indexdir)) {
			Console.WriteLine ("Index:{0} doesnt exist.", indexdir);
			return;
		}
		
	    	// get fingerprint
	    	TextReader reader;
	    	reader = new StreamReader (Path.Combine (indexdir, "fingerprint"));
	    	string fingerprint = reader.ReadLine ();
	    	reader.Close ();
		//Console.WriteLine ("Read fingerprint:" + fingerprint);

		// find out uid
	    	FileAttributesStore fa_store = new FileAttributesStore (new FileAttributesStore_Mixed (indexdir, fingerprint));
	    	Beagle.Daemon.FileAttributes attr = fa_store.Read (path);
		if (attr == null) {
			Console.WriteLine ("No information about this file in index. Ignoring.");
			return;
		}
		string uri_string = "uid:" + GuidFu.ToShortString (attr.UniqueId);
		Console.WriteLine ("Uri = " + uri_string);
		//Console.WriteLine ("FilterName:" + attr.FilterName);
		Console.WriteLine ("LastAttrTime:" + attr.LastAttrTime);
		Console.WriteLine ("LastWriteTime:" + attr.LastWriteTime);

		LuceneQueryingDriver driver;
		driver = new LuceneQueryingDriver (indexdir, -1, true);


		// first try for the Uri:"uid:xxxxxxxxxxxxxxx"
		Lucene.Net.Search.Query query = new TermQuery(new Term("Uri", uri_string));
		if (DoQuery (driver, query))
			return;
		
		// else query by path - this is for static indexes
		path = StringFu.PathToQuotedFileUri (path);
		Console.WriteLine ("Querying by:[" + path + "]");
		query = new TermQuery(new Term("Uri", path));
		DoQuery (driver, query);
		
	}

	static bool DoQuery (LuceneQueryingDriver driver, Lucene.Net.Search.Query query)
	{
		IndexSearcher primary_searcher = LuceneCommon.GetSearcher (driver.PrimaryStore);
		IndexSearcher secondary_searcher = LuceneCommon.GetSearcher (driver.SecondaryStore);
		
		Hits primary_hits = primary_searcher.Search(query);
		Hits secondary_hits = secondary_searcher.Search (query);
		Console.WriteLine ("{0} hits from primary store; {1} hits from secondary store", primary_hits.Length (), secondary_hits.Length ());
		
		Document primary_doc, secondary_doc;
		// there should be exactly one primary hit and 0/1 secondary hit
		if (primary_hits.Length () == 1) {
			primary_doc = primary_hits.Doc (0);
			Console.WriteLine (
			"-----------------------------------------[ Immutable data ]--------------------------------------");
			foreach (Field f in primary_doc.Fields ()) {

				String name = f.Name ();
				String val = f.StringValue ();
				bool stored = f.IsStored ();
				bool searchable = (val [0] == 's');
				bool tokenized = f.IsTokenized();
				if (name.Length >= 7 && name.StartsWith ("prop:"))
					tokenized = (name [5] != 't');
				float boost = f.GetBoost();

				Console.WriteLine ("{0,-30} = [{1}] (stored? {2}, searchable? {3}, tokenized? {4}, boost={5})",
						    name, val, stored, searchable, tokenized, boost);
			}
		}
		
		if (secondary_hits.Length () == 1) {
			secondary_doc = secondary_hits.Doc (0);
			Console.WriteLine (
			"------------------------------------------[ Mutable data ]---------------------------------------");
			foreach (Field f in secondary_doc.Fields ()) {

				String name = f.Name ();
				String val = f.StringValue ();
				bool stored = f.IsStored ();
				bool searchable = (val [0] == 's');
				bool tokenized = f.IsTokenized();
				if (name.Length >= 7 && name.StartsWith ("prop:"))
					tokenized = (name [5] != 't');
				float boost = f.GetBoost();

				Console.WriteLine ("{0,-30} = [{1}] (stored? {2}, searchable? {3}, tokenized? {4}, boost={5})",
						    name, val, stored, searchable, tokenized, boost);
			}
		}

		LuceneCommon.ReleaseSearcher (primary_searcher);
		LuceneCommon.ReleaseSearcher (secondary_searcher);
		
		if (primary_hits.Length () != 0 || secondary_hits.Length () != 0)
			return true;
		else
			return false;
	}

	enum Mode {
		Uris,
		Properties,
		TermFrequencies
	}
		

	static void Main (string [] args)
	{
		Mode mode = Mode.Uris;
		bool show_counts = true;
		string file = null;
		string indexdir = null;
		
		foreach (string arg in args) {

			switch (arg) {
				
			case "--help":
				Console.WriteLine (@"
beagle-dump-index [options] [ [--indexdir=dir] file]
			
--uris                   Dump all Uris (default)
--properties             Dump all properties
--term-frequencies       Dump term frequencies

--show-counts            Show index count totals (default)
--hide-counts            Hide index count totals

--indexdir=<index directory>
                         Absolute path of the directory storing the index
                         e.g. /home/user/.beagle/Indexes/FileSystemIndex
file                     Get information in index about this file or directory

--help                         What you just did");
				Environment.Exit (0);
				break;
				
			case "--uris":
				mode = Mode.Uris; 
				break;

			case "--properties":
				mode = Mode.Properties;
				break;

			case "--term-frequencies":
				mode = Mode.TermFrequencies;
				break;

			case "--hide-counts":
				show_counts = false;
				break;

			case "--show-counts":
				show_counts = false;
				break;

			default:
				if (arg.StartsWith ("--indexdir="))
					indexdir = arg.Remove (0, 11);
				else
					file = arg;
				break;
			}
		}

		if (file == null)
			DumpIndexInformation (mode, show_counts);
		else
			DumpFileIndexInformation (file, indexdir);

	}
}
