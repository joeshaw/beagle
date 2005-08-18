//
// LuceneCommon.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

using Beagle.Util;

namespace Beagle.Daemon {

	public class LuceneCommon {

		public delegate bool HitFilter (Hit hit);

		public const string UnindexedNamespace = "_unindexed:";

		// VERSION HISTORY
		// ---------------
		//
		//  1: Original
		//  2: Changed format of timestamp strings
		//  3: Schema changed to be more Dashboard-Match-like
		//  4: Schema changed for files to include _Directory property
		//  5: Changed analyzer to support stemming.  Bumped version # to
		//     force everyone to re-index.
		//  6: lots of schema changes as part of the general refactoring
		//  7: incremented to force a re-index after our upgrade to lucene 1.4
		//     (in theory the file formats are compatible, we are seeing 'term
		//     out of order' exceptions in some cases)
		//  8: another forced re-index, this time because of massive changes
		//     in the file system backend (it would be nice to have per-backend
		//     versioning so that we didn't have to purge all indexes just
		//     because one changed)
		//  9: changed the way properties are stored, changed in conjunction
		//     with sane handling of multiple properties on hits.
		// 10: changed to support typed and mutable properties
		// 11: moved mime type and hit type into properties
		private const int MAJOR_VERSION = 11;
		private int minor_version = 0;

		private string index_name;
		private string top_dir;

		private string fingerprint;
		private int last_item_count = -1;

		// This is the big index, containing document full-texts and
		// data that is expensive to index.
		private Lucene.Net.Store.Directory primary_store = null;

		// This is the small index, containing document info that we
		// expect to have change.  Canonical example: file names.
		private Lucene.Net.Store.Directory secondary_store = null;

		//////////////////////////////////////////////////////////////////////////////

		protected LuceneCommon (string index_name, int minor_version)
		{
			this.index_name = index_name;
			this.minor_version = minor_version;

			this.top_dir = (Path.IsPathRooted (index_name)) ? index_name : Path.Combine (PathFinder.IndexDir, index_name);
		}

		//////////////////////////////////////////////////////////////////////////////

		protected string IndexName { get { return index_name; } }

		public Lucene.Net.Store.Directory PrimaryStore { get { return primary_store; } }

		public Lucene.Net.Store.Directory SecondaryStore { get { return secondary_store; } }

		public string Fingerprint { get { return fingerprint; } }

		public string TopDirectory { get { return top_dir; } }

		//////////////////////////////////////////////////////////////////////////////

		protected TextCache text_cache = TextCache.UserCache;

		public TextCache TextCache {
			get { return text_cache; }
			set { text_cache = value; }
		}

		//////////////////////////////////////////////////////////////////////////////

		private string VersionFile {
			get { return Path.Combine (top_dir, "version"); }
		}

		private string FingerprintFile {
			get { return Path.Combine (top_dir, "fingerprint"); }
		}

		// Shouldn't really be public
		public string PrimaryIndexDirectory {
			get { return Path.Combine (top_dir, "PrimaryIndex"); }
		}

		// Shouldn't really be public
		public string SecondaryIndexDirectory {
			get { return Path.Combine (top_dir, "SecondaryIndex"); }
		}

		public string LockDirectory {
			get { return Path.Combine (top_dir, "Locks"); }
		}

		//////////////////////////////////////////////////////////////////////////////

		// Deal with dangling locks

		private bool IsDanglingLock (FileInfo info)
		{
			// It isn't even a lock file
			if (info.Name.IndexOf ("write.lock") != -1)
				return false;

			StreamReader reader;
			string pid = null;

			try {
				reader = new StreamReader (info.FullName);
				pid = reader.ReadLine ();
				reader.Close ();

			} catch {
				// We couldn't read the lockfile, so it probably went away.
				return false;
			}

			string cmdline_file;
			cmdline_file = String.Format ("/proc/{0}/cmdline", pid);
			
			string cmdline = "";
			try {
				reader = new StreamReader (cmdline_file);
				cmdline = reader.ReadLine ();
				reader.Close ();
			} catch {
				// If we can't open that file, either:
				// (1) The process doesn't exist
				// (2) It does exist, but it doesn't belong to us.
				//     Thus it isn't an IndexHelper
				// In either case, the lock is dangling --- if it
				// still exists.
				return info.Exists;
			}

			// The process exists, but isn't an IndexHelper.
			// If the lock file is still there, it is dangling.
			// FIXME: During one run of bludgeon I got a null reference
			// exception here, so I added the cmdline == null check.
			// Why exactly would that happen?  Is this logic correct
			// in that (odd and presumably rare) case?
			if (cmdline == null || cmdline.IndexOf ("IndexHelper.exe") == -1)
				return info.Exists;
			
			// If we reach this point, we know:
			// (1) The process still exists
			// (2) We own it
			// (3) It is an IndexHelper process
			// Thus it almost certainly isn't a dangling lock.
			// The process might be wedged, but that is
			// another issue...
			return false;
		}
		

		// Return true if there are dangling locks
		protected bool HaveDanglingLocks ()
		{
			return false;
		}

		protected bool Exists ()
		{
			if (! (Directory.Exists (top_dir)
			       && File.Exists (VersionFile)
			       && File.Exists (FingerprintFile)
			       && Directory.Exists (PrimaryIndexDirectory)
			       && IndexReader.IndexExists (PrimaryIndexDirectory)
			       && Directory.Exists (SecondaryIndexDirectory)
			       && IndexReader.IndexExists (SecondaryIndexDirectory)
			       && Directory.Exists (LockDirectory)))
				return false;

			// Check the index's version number.  If it is wrong,
			// declare the index non-existent.

			StreamReader version_reader;
			string version_str;
			version_reader = new StreamReader (VersionFile);
			version_str = version_reader.ReadLine ();
			version_reader.Close ();

			int current_major_version, current_minor_version;
			int i = version_str.IndexOf ('.');
			
			if (i != -1) {
				current_major_version = Convert.ToInt32 (version_str.Substring (0, i));
				current_minor_version = Convert.ToInt32 (version_str.Substring (i+1));
			} else {
				current_minor_version = Convert.ToInt32 (version_str);
				current_major_version = 0;
			}

			if (current_major_version != MAJOR_VERSION
			    || (minor_version >= 0 && current_minor_version != minor_version)) {
				Logger.Log.Debug ("Version mismatch in {0}", index_name);
				Logger.Log.Debug ("Index has version {0}.{1}, expected {2}.{3}",
						  current_major_version, current_minor_version,
						  MAJOR_VERSION, minor_version);
				return false;
			}

			// Check the lock directory: If there is a dangling write lock,
			// assume that the index is corrupted and declare it non-existent.
			DirectoryInfo lock_dir_info;
			lock_dir_info = new DirectoryInfo (LockDirectory);
			foreach (FileInfo info in lock_dir_info.GetFiles ()) {
				if (IsDanglingLock (info))
					return false;
			}

			return true;
		}

		private Lucene.Net.Store.Directory CreateIndex (string path)
		{
			// Create a directory to put the index in.
			Directory.CreateDirectory (path);

			// Create a new store.
			Lucene.Net.Store.Directory store;
			store = Lucene.Net.Store.FSDirectory.GetDirectory (path, LockDirectory, true);

			// Create an empty index in that store.
			IndexWriter writer;
			writer = new IndexWriter (store, null, true);
			writer.Close ();

			return store;
		}

		// Create will kill your index dead.  Use it with care.
		// You don't need to call Open after calling Create.
		protected void Create ()
		{
			if (minor_version < 0)
				minor_version = 0;

			// Purge any existing directories.
			if (Directory.Exists (top_dir)) {
				Logger.Log.Debug ("Purging {0}", top_dir);
				Directory.Delete (top_dir, true);
			}

			// Create any necessary directories.
			Directory.CreateDirectory (top_dir);
			Directory.CreateDirectory (LockDirectory);
			
			// Create the indexes.
			primary_store = CreateIndex (PrimaryIndexDirectory);
			secondary_store = CreateIndex (SecondaryIndexDirectory);

			// Generate and store the index fingerprint.
			fingerprint = GuidFu.ToShortString (Guid.NewGuid ());
			TextWriter writer;
			writer = new StreamWriter (FingerprintFile, false);
			writer.WriteLine (fingerprint);
			writer.Close ();

			// Store our index version information.
			writer = new StreamWriter (VersionFile, false);
			writer.WriteLine ("{0}.{1}", MAJOR_VERSION, minor_version);
			writer.Close ();
		}

		protected void Open ()
		{
			Open (false);
		}

		protected void Open (bool read_only_mode)
		{
			// Read our index fingerprint.
			TextReader reader;
			reader = new StreamReader (FingerprintFile);
			fingerprint = reader.ReadLine ();
			reader.Close ();

			// Create stores for our indexes.
			primary_store = Lucene.Net.Store.FSDirectory.GetDirectory (PrimaryIndexDirectory, LockDirectory, false, read_only_mode);
			secondary_store = Lucene.Net.Store.FSDirectory.GetDirectory (SecondaryIndexDirectory, LockDirectory, false, read_only_mode);
		}

		////////////////////////////////////////////////////////////////

		//
		// Custom Analyzers
		//

		private class SingletonTokenStream : TokenStream {

			private string singleton_str;

			public SingletonTokenStream (string singleton_str)
			{
				this.singleton_str = singleton_str;
			}

			override public Lucene.Net.Analysis.Token Next ()
			{
				if (singleton_str == null)
					return null;

				Lucene.Net.Analysis.Token token;
				token = new Lucene.Net.Analysis.Token (singleton_str, 0, singleton_str.Length);

				singleton_str = null;
				
				return token;
			}
		}

		// FIXME: This assumes everything being indexed is in English!
		private class BeagleAnalyzer : StandardAnalyzer {

			private char [] buffer = new char [2];
			private bool strip_extra_property_info = false;

			public BeagleAnalyzer (bool strip_extra_property_info)
			{
				this.strip_extra_property_info = strip_extra_property_info;
			}

			public override TokenStream TokenStream (string fieldName, TextReader reader)
			{
				bool is_text_prop = false;

				// Strip off the first two characters in a property.
				// We store type information in those two characters, so we don't
				// want to index them.
				if (fieldName.StartsWith ("prop:")) {
					
					if (strip_extra_property_info) {
						// Skip everything up to and including the first :
						int c;
						do {
							c = reader.Read ();
						} while (c != -1 && c != ':');
					}

					is_text_prop = fieldName.StartsWith ("prop:t");

					// If this is non-text property, just return one token
					// containing the entire string.  We do this to avoid
					// tokenizing keywords.
					if (! is_text_prop)
						return new SingletonTokenStream (reader.ReadToEnd ());
				}

				TokenStream outstream;
				outstream = base.TokenStream (fieldName, reader);

				if (fieldName == "Text"
				    || fieldName == "HotText"
				    || fieldName == "PropertyText"
				    || is_text_prop) {
					outstream = new NoiseFilter (outstream);
					outstream = new PorterStemFilter (outstream);
				}

				return outstream;
			}
		}

		static private Analyzer indexing_analyzer = new BeagleAnalyzer (true);
		static private Analyzer query_analyzer = new BeagleAnalyzer (false);

		static protected Analyzer IndexingAnalyzer { get { return indexing_analyzer; } }
		static protected Analyzer QueryAnalyzer { get { return query_analyzer; } }

		////////////////////////////////////////////////////////////////

		//
		// Dealing with properties
		//

		static private char TypeToCode (PropertyType type)
		{
			switch (type) {
			case PropertyType.Text:    return 't';
			case PropertyType.Keyword: return 'k';
			case PropertyType.Date:    return 'd';
			}
			throw new Exception ("Bad property type: " + type);
		}

		static private PropertyType CodeToType (char c)
		{
			switch (c) {
			case 't': return PropertyType.Text;
			case 'k': return PropertyType.Keyword;
			case 'd': return PropertyType.Date;
			}

			throw new Exception ("Bad property code: " + c);
		}

		static private string TypeToWildcardField (PropertyType type)
		{
			switch (type) {
			case PropertyType.Text:    return "PropertyText";
			case PropertyType.Keyword: return null; // wildcard keyword lookups are crack
			case PropertyType.Date:    return "PropertyDate";
			}

			return null;
		}

		// Exposing this is a little bit suspicious.
		static protected string PropertyToFieldName (PropertyType type, string key)
		{
			return String.Format ("prop:{0}:{1}", TypeToCode (type), key);

		}

		static protected void AddPropertyToDocument (Property prop, Document doc)
		{
			if (prop == null || prop.Value == null)
				return;

			// Don't actually put properties in the UnindexedNamespace
			// in the document.  A horrible (and yet lovely!) hack.
			if (prop.Key.StartsWith (UnindexedNamespace))
				return;

			Field f;

			if (prop.IsSearched) {
				string wildcard_field = TypeToWildcardField (prop.Type);
				bool tokenize = (prop.Type == PropertyType.Text);
				if (wildcard_field != null) {
					f = new Field (wildcard_field,
						       prop.Value,
						       false, // never stored
						       true,  // always indexed
						       tokenize);
					doc.Add (f);
				}
			}

			string coded_value;
			coded_value = String.Format ("{0}:{1}",
						     prop.IsSearched ? 's' : '_',
						     prop.Value);

			f = new Field (PropertyToFieldName (prop.Type, prop.Key),
				       coded_value,
				       true,        // always store
				       true,        // always index
				       true);       // always tokenize (just strips off type code for keywords)
			doc.Add (f);
		}

		static protected Property GetPropertyFromDocument (Field f, Document doc, bool from_primary_index)
		{
			// Note: we don't use the document that we pass in,
			// but in theory we could.  At some later point we
			// might need to split a property's data across two or
			// more fields in the document.

			if (f == null)
				return null;

			string field_name;
			field_name = f.Name ();
			if (field_name.Length < 7
			    || ! field_name.StartsWith ("prop:"))
				return null;

			string field_value;
			field_value = f.StringValue ();

			Property prop;
			prop = new Property ();
			prop.Type = CodeToType (field_name [5]);
			prop.Key = field_name.Substring (7);
			prop.Value = field_value.Substring (2);
			prop.IsSearched = (field_value [0] == 's');
			prop.IsMutable = ! from_primary_index;

			return prop;
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Dealing with documents
		//

		static protected void BuildDocuments (Indexable indexable,
						      out Document primary_doc,
						      out Document secondary_doc)
		{
			primary_doc = new Document ();
			secondary_doc = null;

			Field f;

			f = Field.Keyword ("Uri", UriFu.UriToSerializableString (indexable.Uri));
			primary_doc.Add (f);

			if (indexable.ParentUri != null) {
				f = Field.Keyword ("ParentUri", UriFu.UriToSerializableString (indexable.ParentUri));
				primary_doc.Add (f);
			}
			
			if (indexable.ValidTimestamp) {
				string str = StringFu.DateTimeToString (indexable.Timestamp);
				f = Field.Keyword ("Timestamp", str);
				primary_doc.Add (f);
			}

			if (indexable.NoContent) {
				// If there is no content, make a note of that
				// in a special property.
				Property prop;
				prop = Property.NewBool ("beagle:NoContent", true);
				AddPropertyToDocument (prop, primary_doc);
				
			} else {

				// Since we might have content, add our text
				// readers.

				TextReader reader;
				
				reader = indexable.GetTextReader ();
				if (reader != null) {
					f = Field.Text ("Text", reader);
					primary_doc.Add (f);
				}
			
				reader = indexable.GetHotTextReader ();
				if (reader != null) {
					f = Field.Text ("HotText", reader);
					primary_doc.Add (f);
				}
			}

			// Store the Type and MimeType in special properties

			if (indexable.Type != null) {
				Property prop;
				prop = Property.NewKeyword ("beagle:Type", indexable.Type);
				AddPropertyToDocument (prop, primary_doc);
			}

			if (indexable.MimeType != null) {
				Property prop;
				prop = Property.NewKeyword ("beagle:MimeType", indexable.MimeType);
				AddPropertyToDocument (prop, primary_doc);
			}

			// Store the other properties
				
			foreach (Property prop in indexable.Properties) {
				
				Document target_doc = primary_doc;
				if (prop.IsMutable) {
					if (secondary_doc == null) {
						secondary_doc = new Document ();
						f = Field.Keyword ("Uri", UriFu.UriToSerializableString (indexable.Uri));
						secondary_doc.Add (f);
					}
					target_doc = secondary_doc;
				}
					
				AddPropertyToDocument (prop, target_doc);
			}
		}

		static protected Document RewriteDocument (Document old_secondary_doc,
							   Indexable prop_only_indexable)
		{
			Hashtable seen_props;
			seen_props = new Hashtable ();

			Document new_doc;
			new_doc = new Document ();

			Field uri_f;
			uri_f = Field.Keyword ("Uri", UriFu.UriToSerializableString (prop_only_indexable.Uri));
			new_doc.Add (uri_f);

			Logger.Log.Debug ("Rewriting {0}", prop_only_indexable.DisplayUri);

			// Add the new properties to the new document.  To
			// delete a property, set the Value to null... then it
			// will be added to seen_props (so the old value will
			// be ignored below), but AddPropertyToDocument will
			// return w/o doing anything.
			foreach (Property prop in prop_only_indexable.Properties) {
				seen_props [prop.Key] = prop;
				AddPropertyToDocument (prop, new_doc);
				Logger.Log.Debug ("New prop '{0}' = '{1}'", prop.Key, prop.Value);
			}

			// Copy the other properties from the old document to the
			// new one, skipping any properties that we got new values
			// for out of the Indexable.
			if (old_secondary_doc != null) {
				foreach (Field f in old_secondary_doc.Fields ()) {
					Property prop;
					prop = GetPropertyFromDocument (f, old_secondary_doc, false);
					if (prop != null && ! seen_props.Contains (prop.Key)) {
						Logger.Log.Debug ("Old prop '{0}' = '{1}'", prop.Key, prop.Value);
						AddPropertyToDocument (prop, new_doc);
					}
				}
			}

			return new_doc;
		}

		static protected Uri GetUriFromDocument (Document doc)
		{
			string uri;
			uri = doc.Get ("Uri");
			if (uri == null)
				throw new Exception ("Got document from Lucene w/o a URI!");
			return UriFu.UriStringToUri (uri);
		}

		static protected Hit DocumentToHit (Document doc)
		{
			Hit hit;
			hit = new Hit ();

			hit.Uri = GetUriFromDocument (doc);

			string str;
			str = doc.Get ("ParentUri");
			if (str != null)
				hit.ParentUri = UriFu.UriStringToUri (str);
			
			hit.Timestamp = StringFu.StringToDateTime (doc.Get ("Timestamp"));

			hit.Source = "lucene";
			hit.ScoreRaw = 1.0;

			AddPropertiesToHit (hit, doc, true);

			// Get the Type and MimeType from the properties.
			hit.Type = hit.GetFirstProperty ("beagle:Type");
			hit.MimeType = hit.GetFirstProperty ("beagle:MimeType");

			return hit;
		}

		static protected void AddPropertiesToHit (Hit hit, Document doc, bool from_primary_index)
		{
			foreach (Field f in doc.Fields ()) {
				Property prop;
				prop = GetPropertyFromDocument (f, doc, from_primary_index);
				if (prop != null)
					hit.AddProperty (prop);
			}
		}


		//////////////////////////////////////////////////////////////////////////////

		//
		// Handle the index's item count
		//

		public int GetItemCount ()
		{
			if (last_item_count < 0) {
				IndexReader reader;
				reader = IndexReader.Open (PrimaryStore);
				last_item_count = reader.NumDocs ();
				reader.Close ();
			}
			return last_item_count;
		}

		// We should set the cached count of index items when IndexReaders
		// are open and available, so calls to GetItemCount will return immediately.

		protected bool HaveItemCount { get { return last_item_count >= 0; } }
		
		protected void SetItemCount (IndexReader reader)
		{
			last_item_count = reader.NumDocs ();
		}

		protected void AdjustItemCount (int delta)
		{
			if (last_item_count >= 0)
				last_item_count += delta;
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Access to the stemmer and list of stop words
		//

		static PorterStemmer stemmer = new PorterStemmer ();

		static public string Stem (string str)
		{
			return stemmer.Stem (str);
		}

		public static bool IsStopWord (string stemmed_word)
		{
			return ArrayFu.IndexOfString (StopAnalyzer.ENGLISH_STOP_WORDS, stemmed_word) != -1;
		}

		//////////////////////////////////////////////////////////////////////////////

		//
		// Queries
		//

		static private LNS.Query StringToQuery (string field_name, string text)
		{
			ArrayList tokens = new ArrayList ();

			// Use the analyzer to extract the query's tokens.
			// This code is taken from Lucene's query parser.
			TokenStream source = QueryAnalyzer.TokenStream (field_name, new StringReader (text));
			while (true) {
				Lucene.Net.Analysis.Token token;
				try {
					token = source.Next ();
					if (token == null)
						break;
				} catch (IOException) {
					break;
				}
				if (token != null)
					tokens.Add (token.TermText ());
			}
			try {
				source.Close ();
			} catch (IOException) { 
				// ignore
			}

			if (tokens.Count == 0)
				return null;

			LNS.PhraseQuery query = new LNS.PhraseQuery ();

			foreach (string token in tokens) {
				Term term;
				term = new Term (field_name, token);
				query.Add (term);
			}

			return query;
		}

		// search_subset_uris is a list of Uris that this search should be
		// limited to.
		static protected void QueryPartToQuery (QueryPart     abstract_part,
							bool          only_build_primary_query,
							out LNS.Query primary_query,
							out LNS.Query secondary_query,
							out HitFilter hit_filter)
		{
			primary_query = null;
			secondary_query = null;
			hit_filter = null;

			if (abstract_part == null)
				return;

			if (abstract_part is QueryPart_Text) {
				QueryPart_Text part = (QueryPart_Text) abstract_part;

				if (! (part.SearchFullText || part.SearchTextProperties))
					return;

				LNS.BooleanQuery p_query = new LNS.BooleanQuery ();
				primary_query = p_query;

				if (part.SearchFullText) {
					LNS.Query subquery;
					subquery = StringToQuery ("Text", part.Text);
					if (subquery != null)
						p_query.Add (subquery, false, false);

					// FIXME: HotText is ignored for now!
					// subquery = StringToQuery ("HotText", part.Text);
					// if (subquery != null) 
					//    p_query.Add (subquery, false, false);
				}

				if (part.SearchTextProperties) {
					LNS.Query subquery;
					subquery = StringToQuery ("PropertyText", part.Text);
					if (subquery != null) {
						p_query.Add (subquery, false, false);
						
						// Properties can live in either index
						if (! only_build_primary_query)
							secondary_query = subquery.Clone () as LNS.Query;
					}
				}

				return;
			}

			if (abstract_part is QueryPart_Property) {
				QueryPart_Property part = (QueryPart_Property) abstract_part;

				string field_name;
				if (part.Key == QueryPart_Property.AllProperties) {
					field_name = TypeToWildcardField (part.Type);
					// FIXME: probably shouldn't just return silently
					if (field_name == null)
						return;
				} else
					field_name = PropertyToFieldName (part.Type, part.Key);

				if (part.Type == PropertyType.Text)
					primary_query = StringToQuery (field_name, part.Value);
				else
					primary_query = new LNS.TermQuery (new Term (field_name, part.Value));

				// Properties can live in either index
				if (! only_build_primary_query && primary_query != null)
					secondary_query = primary_query.Clone () as LNS.Query;
				
				return;
			}

			if (abstract_part is QueryPart_DateRange) {

				// FIXME: Unsupported
				return;
			} 

			if (abstract_part is QueryPart_Or) {
				QueryPart_Or part = (QueryPart_Or) abstract_part;
				
				// Assemble a new BooleanQuery combining all of the sub-parts.
				LNS.BooleanQuery p_query;
				p_query = new LNS.BooleanQuery ();

				LNS.BooleanQuery s_query = null;
				if (! only_build_primary_query)
					s_query = new LNS.BooleanQuery ();

				primary_query = p_query;
				secondary_query = s_query;
				
				foreach (QueryPart  sub_part in part.SubParts) {
					LNS.Query p_subq, s_subq;
					HitFilter sub_hit_filter; // FIXME: This is (and must be) ignored
					QueryPartToQuery (sub_part, only_build_primary_query,
							  out p_subq, out s_subq, out sub_hit_filter);
					if (p_subq != null)
						p_query.Add (p_subq, false, false);
					if (s_subq != null)
						s_query.Add (s_subq, false, false);
				}

				return;
			} 

			throw new Exception ("Unhandled QueryPart type! " + abstract_part.ToString ());
		}

		static protected LNS.Query UriQuery (string field_name, Uri uri)
		{
			return new LNS.TermQuery (new Term (field_name, UriFu.UriToSerializableString (uri)));
		}

		static protected LNS.Query UriQuery (string field_name, ICollection uri_list)
		{
			return UriQuery (field_name, uri_list, null);
		}

		static protected LNS.Query UriQuery (string field_name, ICollection uri_list, LNS.Query extra_requirement)
		{
			if (uri_list.Count == 0)
				return null;

			int max_clauses;
			max_clauses = LNS.BooleanQuery.GetMaxClauseCount ();
			
			int N;
			N = 1 + (uri_list.Count - 1) / max_clauses;
			
			LNS.BooleanQuery top_query;
			top_query = new LNS.BooleanQuery ();

			int cursor = 0;
			if (extra_requirement != null) {
				top_query.Add (extra_requirement, true, false);
				++cursor;
			}

			ArrayList bottom_queries = null;

			if (N > 1) {
				bottom_queries = new ArrayList ();
				for (int i = 0; i < N; ++i) {
					LNS.BooleanQuery bq;
					bq = new LNS.BooleanQuery ();
					bottom_queries.Add (bq);
					top_query.Add (bq, false, false);
				}
			}

			foreach (Uri uri in uri_list) {
				LNS.Query subquery;
				subquery = UriQuery (field_name, uri);
				
				LNS.BooleanQuery target;
				if (N == 1)
					target = top_query;
				else {
					target = (LNS.BooleanQuery) bottom_queries [cursor];
					++cursor;
					if (cursor >= N)
						cursor = 0;
				}
				
				target.Add (subquery, false, false);
			}

			return top_query;
		}

		///////////////////////////////////////////////////////////////////////////////////

		public int SegmentCount {
			get {
				DirectoryInfo dir_info;
				int p_count = 0, s_count = 0;

				dir_info = new DirectoryInfo (PrimaryIndexDirectory);
				foreach (FileInfo file_info in dir_info.GetFiles ())
					if (file_info.Extension == ".cfs")
						++p_count;

				dir_info = new DirectoryInfo (SecondaryIndexDirectory);
				foreach (FileInfo file_info in dir_info.GetFiles ())
					if (file_info.Extension == ".cfs")
						++s_count;

				return p_count > s_count ? p_count : s_count;
			}
		}

		///////////////////////////////////////////////////////////////////////////////////

		//
		// Grabbing a block of hits
		//

		public int GetBlockOfHits (int cookie,
					   Hit [] block_of_hits)
		{
			IndexReader primary_reader;
			IndexReader secondary_reader;
			primary_reader = IndexReader.Open (PrimaryStore);
			secondary_reader = IndexReader.Open (SecondaryStore);

			int request_size;
			request_size = block_of_hits.Length;
			if (request_size > primary_reader.NumDocs ())
				request_size = primary_reader.NumDocs ();

			int max_doc;
			max_doc = primary_reader.MaxDoc ();

			if (cookie < 0) {
				Random random;
				random = new Random ();
				cookie = random.Next (max_doc);
			}

			int original_cookie;
			original_cookie = cookie;

			Hashtable primary_docs, secondary_docs;
			primary_docs = UriFu.NewHashtable ();
			secondary_docs = UriFu.NewHashtable ();

			// Load the primary documents
			for (int i = 0; i < request_size; ++i) {
				
				if (! primary_reader.IsDeleted (cookie)) {
					Document doc;
					doc = primary_reader.Document (cookie);
					primary_docs [GetUriFromDocument (doc)] = doc;
				}
				
				++cookie;
				if (cookie >= max_doc) // wrap around
					cookie = 0;

				// If we somehow end up back where we started,
				// give up.
				if (cookie == original_cookie)
					break;
			}

			// If necessary, load the secondary documents
			if (secondary_reader != null) {
				LNS.IndexSearcher searcher;
				searcher = new LNS.IndexSearcher (secondary_reader);
				
				LNS.Query uri_query;
				uri_query = UriQuery ("Uri", primary_docs.Keys);
				
				LNS.Hits hits;
				hits = searcher.Search (uri_query);
				for (int i = 0; i < hits.Length (); ++i) {
					Document doc;
					doc = hits.Doc (i);
					secondary_docs [GetUriFromDocument (doc)] = doc;
				}
				
				searcher.Close ();
			}

			// Now assemble the hits
			int j = 0;
			foreach (Uri uri in primary_docs.Keys) {
				Document primary_doc, secondary_doc;
				primary_doc = primary_docs [uri] as Document;
				secondary_doc = secondary_docs [uri] as Document;

				Hit hit;
				hit = DocumentToHit (primary_doc);
				if (secondary_doc != null)
					AddPropertiesToHit (hit, secondary_doc, false);
				
				block_of_hits [j] = hit;
				++j;
			}

			// null-pad the array, if necessary
			for (; j < block_of_hits.Length; ++j)
				block_of_hits [j] = null;


			// Return the new cookie
			return cookie;
		}
	}
}
