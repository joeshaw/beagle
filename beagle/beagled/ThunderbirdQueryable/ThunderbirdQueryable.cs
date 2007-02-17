//
// ThunderbirdQueryable.cs: This is where all the magic begins!
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
using System.Threading;
using System.Collections;
using System.Text.RegularExpressions;

using Beagle;
using Beagle.Util;

// Register the Thunderbird backend for this assembly.
[assembly: Beagle.Daemon.IQueryableTypes (typeof (Beagle.Daemon.ThunderbirdQueryable.ThunderbirdQueryable))]

namespace Beagle.Daemon.ThunderbirdQueryable {

	[QueryableFlavor (Name = "Thunderbird", Domain = QueryDomain.Local, RequireInotify = false)]
	public class ThunderbirdQueryable : LuceneQueryable {
		private static DateTime indexing_start;
		private ThunderbirdIndexer indexer;
		
		public ThunderbirdQueryable () :
			base ("ThunderbirdIndex")
		{
			// Remove one second from the start time to make sure we don't run into any troubles
			indexing_start = DateTime.UtcNow.Subtract (new TimeSpan (0, 0, 1));
			indexer = null;
			
			GMime.Global.Init ();
			
			if (Environment.GetEnvironmentVariable ("BEAGLE_THUNDERBIRD_DEBUG") != null) {
				Thunderbird.Debug = true;
				Logger.Log.Debug ("Running Thunderbird backend in debug mode");
			}
		}
		
		public override void Start ()
		{
			base.Start ();
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}
		
		private void StartWorker ()
		{
			Logger.Log.Info ("Starting Thunderbird backend");
			Stopwatch watch = new Stopwatch ();
			watch.Start ();
			
			string root_path = Thunderbird.GetRootPath ();
			if (!Directory.Exists (root_path)) {
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (IndexDataCheck));
				Logger.Log.Info ("No data available for indexing in {0}", root_path);
				return;
			}
			
			Started = true;

			indexer = new ThunderbirdIndexer (this, Thunderbird.GetProfilePaths (root_path));
			indexer.Crawl ();
			
			watch.Stop ();
			Logger.Log.Info ("Thunderbird backend done in {0}s", watch.ElapsedTime);
		}
		
		private bool IndexDataCheck ()
		{
			if (!Directory.Exists (Thunderbird.GetRootPath ()))
				return true;
				
			StartWorker ();
			return false;
		}
		
		// We need this in order to perform custom queries to the lucene database
		override protected LuceneQueryingDriver BuildLuceneQueryingDriver (string index_name,
										   int    minor_version,
										   bool   read_only_mode)
		{
			return new LuceneAccess (index_name, minor_version, read_only_mode);
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		public Scheduler.Task NewRemoveTaskByDate (DateTime end_date)
		{
			return NewAddTask (new DateIndexableGenerator (Driver, Lucene, end_date));
		}
		
		// The purpose of this IndexableGenerator is to remove mails older than the
		// specified date (when beagle began to index Thunderbird mails)
		private class DateIndexableGenerator : IIndexableGenerator {
			private LuceneQueryingDriver driver;
			private LuceneAccess lucene;
			private DateTime end_date;
			
			private Uri[] stored_uris;
			private IEnumerator enumerator;
			
			public DateIndexableGenerator (LuceneQueryingDriver driver, LuceneAccess lucene, DateTime end_date)
			{
				this.driver = driver;
				this.lucene = lucene;
				this.end_date = end_date;
				this.stored_uris = null;
			}
			
			public Indexable GetNextIndexable ()
			{
				return new Indexable (IndexableType.Remove, (Uri) enumerator.Current);
			}
			
			public bool HasNextIndexable ()
			{
				if (stored_uris == null) {
					stored_uris = driver.PropertyQuery (Property.NewKeyword ("fixme:client", "thunderbird"));
					enumerator = stored_uris.GetEnumerator ();
				}
				
				do {
					while (enumerator == null || !enumerator.MoveNext ())
						return false;
				} while (MatchesDate ((enumerator.Current as Uri)));
				
				return true;
			}
			
			private bool MatchesDate (Uri uri)
			{
				LuceneAccess.StoredInfo info = lucene.GetStoredInfo (uri);
				
				try {
					if (!info.Equals (end_date) && info.LastIndex.CompareTo (end_date) < 0)
						return false;
				} catch {}
				
				return true;
			}
			
			public string StatusName {
				get {
					return String.Format ("Removing Thunderbird mails past {0}", end_date.ToString ()); 
				}
			}
			
			public void PostFlushHook () { }
		}
		
		/////////////////////////////////////////////////////////////////////////////////////
		
		public LuceneAccess Lucene {
			get { return (LuceneAccess) Driver; }
		}
		
		public static DateTime IndexingStart {
			get { return indexing_start; }
		}
	}

}
