//
// KonqQueryable.cs
//
// Copyright (C) 2005 Debajyoti Bera
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
using System.Text;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.KonqQueryable {

	[QueryableFlavor (Name="KonquerorHistory", Domain=QueryDomain.Local, RequireInotify=false)]
	public class KonqQueryable : LuceneFileQueryable, IIndexableGenerator {

		private static Logger log = Logger.Get ("KonqQueryable");

		string konq_cache_dir;
		private IEnumerator directory_enumerator = null;
		private int polling_interval_in_seconds = 300; // 5 min

		// ISO-Latin1 is 28591
		private Encoding latin_encoding = Encoding.GetEncoding (28591);

		public KonqQueryable () : base ("KonqHistoryIndex")
		{
			/* How to determine kio-http cache location ?
			 * From KDE web-page it looks like /var/tmp/kdecache-$USERNAME/http
			 */
			//Now we use the $KDEVARTMP env variable
			string tmpdir = Environment.GetEnvironmentVariable ("KDEVARTMP");

			if (tmpdir == null || tmpdir == "")
    				tmpdir = "/var/tmp";

			konq_cache_dir = Path.Combine (tmpdir, "kdecache-" + Environment.UserName ); 
			konq_cache_dir = Path.Combine (konq_cache_dir, "http"); 
			log.Debug ("KonqCacheDir: " + konq_cache_dir);
		}

		/////////////////////////////////////////////////

		public override void Start () 
		{			
			base.Start ();
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (!Directory.Exists (konq_cache_dir)) {
				// if the directory is not present, user is not running KDE
				// no need to periodically check
				//GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
                                return;
			}
				
			if (Inotify.Enabled) {
				// watch konq_cache_dir for new directory creations
				Inotify.EventType mask = Inotify.EventType.Create;
				Inotify.Subscribe (konq_cache_dir, OnInotifyEvent, mask);
			} else {
				Scheduler.Task crawl_task = Scheduler.TaskFromHook (new Scheduler.TaskHook (CrawlHook));
				crawl_task.Tag = "Crawling konqueror webcache";
				crawl_task.Source = this;
				ThisScheduler.Add (crawl_task);
			}

                        log.Info ("Starting Konq history backend ...");
			Crawl ();
		}

		private void Crawl ()
		{
                        State = QueryableState.Crawling;
			directory_enumerator = DirectoryWalker.GetDirectoryInfos (konq_cache_dir).GetEnumerator ();
			Scheduler.Task crawl_task = NewAddTask (this);
			crawl_task.Tag = crawler_tag;
			ThisScheduler.Add (crawl_task);
			State = QueryableState.Idle;
		}

		private string crawler_tag = "Konqueror History Crawler";
		private void CrawlHook (Scheduler.Task task)
		{
			if (!ThisScheduler.ContainsByTag (crawler_tag)) {
				Crawl ();
			}

			task.Reschedule = true;
			task.TriggerTime = DateTime.Now.AddSeconds (polling_interval_in_seconds);
		}
		
		private bool CheckForExistence ()
                {
                        if (!Directory.Exists (konq_cache_dir))
                                return true;

                        this.Start ();

                        return false;
                }

		/////////////////////////////////////////////////

                // Modified/Created event using Inotify

		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "")
				return;

			// Watch konq_cache_dir for new directory creation
			// Watch its subdirectories for new file creation
			// If any file in created in konq_cache_dir, ignore it
			// Its a Konq error otherwise
			if ((type & Inotify.EventType.IsDirectory) == 0)
				IndexSingleFile (Path.Combine (path, subitem));
			else if ((type & Inotify.EventType.IsDirectory) != 0)
				Inotify.Subscribe (konq_cache_dir, OnInotifyEvent, Inotify.EventType.CloseWrite);
		}

		void IndexSingleFile (string path)
		{
			if (path.EndsWith (".new"))
				return;
			Indexable indexable = FileToIndexable (path);
			if (indexable == null)
				return;
			Scheduler.Task task = NewAddTask (indexable);
			task.Priority = Scheduler.Priority.Immediate;
			task.Tag = path;
			task.SubPriority = 0;
			ThisScheduler.Add (task);
		}

		/////////////////////////////////////////////////
		
		private Indexable FileToIndexable (string path) {
			//Logger.Log.Debug ("KonqQ: Trying to index " + path);

			FileStream stream;
			try {
				stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (FileNotFoundException) {
				// that was fast - lost the file
				return null;
			}
			
			using (StreamReader reader = new StreamReader (stream, latin_encoding)) {
				string url = null;
				string creation_date = null;
				string mimetype = null;
				string charset = null;
				bool is_ok = KonqHistoryUtil.ShouldIndex (reader, 
									  out url, 
									  out creation_date,
									  out mimetype, 
									  out charset);
			
				if (!is_ok || url == String.Empty) {
					//Logger.Log.Debug ("KonqQ: Skipping non-html file " + path + " of type=" + mimetype);
					// finding out if a cache file should be indexed is expensive
					// so, soon after we run the test, write lastwritetime attribute
					FileAttributesStore.AttachLastWriteTime (path, DateTime.UtcNow);
					return null; // we wont index bad files and non-html files
				}

				Logger.Log.Debug ("KonqQ: Indexing " + path + " with url=" + url);
				Uri uri = new Uri (url, true);
				if (uri.Scheme == Uri.UriSchemeHttps) {
					Logger.Log.Error ("Indexing secure https:// URIs is not secure!");
					return null;
				}
			
				Indexable indexable = new Indexable (uri);
				indexable.HitType = "WebHistory";
				indexable.MimeType = KonqHistoryUtil.KonqCacheMimeType;
				// store www.beaglewiki.org as www beagle org, till inpath: query is implemented
				indexable.AddProperty (Property.NewUnstored ("fixme:urltoken", StringFu.UrlFuzzyDivide (url)));
				// hint for the filter about the charset
				indexable.AddProperty (Property.NewUnsearched (StringFu.UnindexedNamespace + "charset", charset));
			
				DateTime date = new DateTime (1970, 1, 1);
				date = date.AddSeconds (Int64.Parse (creation_date));
				indexable.Timestamp = date;

				indexable.ContentUri = UriFu.PathToFileUri (path);
				return indexable;
			}
		}

		// FIXME: Implement removefile - removing files from history doesnt really make sense ? Do they ?

		// ---------------- IIndexableGenerator --------------------------
		private FileInfo current_file;
		private IEnumerator file_enumerator = null;

		public Indexable GetNextIndexable ()
		{
			if (current_file == null)
				return null;
			return FileToIndexable (current_file.FullName);
		}
		
		public bool HasNextIndexable ()
		{
			do {
				while (file_enumerator == null || ! file_enumerator.MoveNext ()) {
					if (! directory_enumerator.MoveNext ()) {
						Logger.Log.Debug ("KonqQ: Crawling done");
						file_enumerator = null;
						current_file = null;
						return false;
					}
					DirectoryInfo current_dir = (DirectoryInfo)directory_enumerator.Current;
					//Logger.Log.Debug ("Trying dir:" + current_dir.Name);
					// start watching for new files and get the list of current files
					// kind of race here - might get duplicate files
					if (Inotify.Enabled)
						Inotify.Subscribe (current_dir.FullName, OnInotifyEvent, 
								    Inotify.EventType.Create | Inotify.EventType.MovedTo);
					file_enumerator = DirectoryWalker.GetFileInfos (current_dir).GetEnumerator ();
				}
				current_file = (FileInfo) file_enumerator.Current;
				//if (!IsUpToDate (current_file.FullName))
				//	Logger.Log.Debug (current_file.FullName + " is not upto date");
			} while (IsUpToDate (current_file.FullName));

			return true;
		}

		public string StatusName {
			get { return String.Format ("KonquerorQueryable: Indexing {0}", (current_file == null ? "Done" : current_file.FullName)); }
		}

		public void PostFlushHook ()
		{ }

	}
}
