//
// ThunderbirdIndexableGenerator.cs: A helper class that makes it very easy to add new features to this backend
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using Beagle.Util;
using Beagle.Daemon;
using TB = Beagle.Util.Thunderbird;

using GMime;

namespace Beagle.Daemon.ThunderbirdQueryable {

	// This is a generic IndexableGenerator-class and should be used to index mork files only!
	public abstract class ThunderbirdIndexableGenerator : IIndexableGenerator {
		protected ThunderbirdIndexer indexer;
		protected TB.Database db;
		protected TB.Account account;
		protected IEnumerator db_enumerator;
		
		private bool full_index;
		private string db_file;
		private bool done;
		private string relative_path;
		protected Hashtable stored_cache;
		
		public ThunderbirdIndexableGenerator (ThunderbirdIndexer indexer, TB.Account account, string db_file)
		{
			this.indexer = indexer;
			this.indexer.NotificationEvent += OnNotification;
			this.account = account;
			this.full_index = true;
			this.db_file = db_file;
			this.done = false;
			this.relative_path = Thunderbird.GetRelativePath (db_file);
			
			// Load the database and make sure the enumerator is up to date. Otherwise we will
			// get lots of null exceptions when enumerating the database.
			LoadDatabase ();
			ResetEnumerator ();

			// Fetch all already stored uris in the index. This way we can remove one uri at the time
			// while we are indexing and thus in the end now which mails that doesn't exist anymore.
			stored_cache = indexer.Lucene.GetStoredUriStrings (account.Server, relative_path);
		}
		
		public abstract bool HasNextIndexable ();
		public abstract Indexable GetNextIndexable ();
		public abstract void LoadDatabase ();
		
		public virtual bool IsUpToDate (Uri uri)
		{
			if (uri == null)
				return false;
			
			LuceneAccess.StoredInfo info = indexer.Lucene.GetStoredInfo (uri);

			// Remove this uri from the cache
			if (stored_cache != null)
				stored_cache.Remove (uri.ToString ());
			
			// Check if this time is "older" than the time we began to index and if the index
			// status has changed (partial vs. full indexing)
			if (info != null && ThunderbirdQueryable.IndexingStart.CompareTo (info.LastIndex) < 0 && 
				FullIndex == info.FullyIndexed) {
				return true;
			}

			return false;
		}
		
		public virtual void PostFlushHook ()
		{
			if (!Done || (stored_cache == null) || (Done && stored_cache.Count == 0))
				return;
			
			if (Thunderbird.Debug)
				Logger.Log.Debug ("Cleaning out old objects in {0} ({1})", RelativePath, stored_cache.Count);
		
			ArrayList uris = new ArrayList ();
			foreach (string uri_str in stored_cache.Keys)
				uris.Add (new Uri (uri_str));
				
			indexer.ScheduleRemoval ((Uri[]) uris.ToArray (typeof (Uri)), 
				String.Format ("PostFlushHook-{0}", RelativePath), Scheduler.Priority.Delayed);
		}
		
		protected virtual Indexable NewIndexable (Uri uri, DateTime timestamp, string hit_type)
		{
			Indexable indexable;
			
			indexable = new Indexable (uri);
			indexable.HitType = hit_type;
			indexable.Timestamp = timestamp;
			
			indexable.AddProperty (Property.NewKeyword ("fixme:account", account.Server));
			indexable.AddProperty (Property.NewKeyword ("fixme:client", "thunderbird"));
			indexable.AddProperty (Property.NewUnsearched ("fixme:fullyIndexed", full_index));
			indexable.AddProperty (Property.NewUnsearched ("fixme:file", RelativePath));
			indexable.AddProperty (Property.NewDate ("fixme:indexDateTime", DateTime.UtcNow));
			
			return indexable;
		}
		
		protected virtual void ResetEnumerator ()
		{
			if (db != null && db.Count > 0)
				db_enumerator = db.GetEnumerator ();
			else
				db_enumerator = null;
		}
		
		protected virtual void OnNotification (object o, NotificationEventArgs args)
		{
			if (args.Account != account)
				return;
			
			switch (args.Type) {
			case NotificationType.StopIndexing:
				indexer.NotificationEvent -= OnNotification;
				Logger.Log.Debug ("Stopping running task {0}", account.Server);
				break;
				
			case NotificationType.RestartIndexing:
				LoadDatabase ();
				break;
				
			case NotificationType.UpdateAccountInformation:
				account = (TB.Account) args.Data;
				LoadDatabase ();
				break;
				
			}
		}

		// Done should be set to true when there's no more objects to index. This will allow
		// PostFlushHook to remove old objects from the index.
		public bool Done {
			get { return done; }
			set { done = value; }
		}
		
		public string DbFile {
			get { return db_file; }
			set { db_file = value; }
		}
		
		public bool FullIndex {
			get { return full_index; }
			set { full_index = value; }
		}
		
		// Realtive path to current mork_file
		public string RelativePath {
			get { return relative_path; }
		}
		
		protected IEnumerator DbEnumerator {
			get { return db_enumerator; }
		}
		
		public string StatusName { 
			get { return account.Server; }
		}
	}
	
	/////////////////////////////////////////////////////////////////////////////////////
	
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
	public class ThunderbirdIndexableGeneratorAttribute : System.Attribute {
		private TB.AccountType type;
		private string description;
		private bool enabled;
		
		public ThunderbirdIndexableGeneratorAttribute (TB.AccountType type, string description, bool enabled)
		{
			this.type = type;
			this.description = description;
			this.enabled = enabled;
		}
		
		public TB.AccountType Type {
			get { return type; }
			set { type = value; }
		}
		
		public string Description {
			get { return description; }
			set { description = value; }
		}
		
		public bool Enabled {
			get { return enabled; }
			set { enabled = value; }
		}
	}
	
	/////////////////////////////////////////////////////////////////////////////////////
	
	
	public class UriRemovalIndexableGenerator : IIndexableGenerator {
		private Uri[] uris;
		
		private IEnumerator enumerator;
		
		public UriRemovalIndexableGenerator (Uri[] uris)
		{
			this.uris = uris;
			this.enumerator = this.uris.GetEnumerator ();
		}
		
		public Indexable GetNextIndexable ()
		{
			return new Indexable (IndexableType.Remove, (Uri) enumerator.Current);
		}
		
		public bool HasNextIndexable ()
		{
			while (enumerator == null || !enumerator.MoveNext ())
				return false;
			
			return true;
		}
		
		public string StatusName {
			get { return String.Format ("Removing {0} uris", uris.Length); }
		}
		
		public void PostFlushHook () { }
	}
}
