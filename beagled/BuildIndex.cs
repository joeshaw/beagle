//
// BuildIndex.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

using System.Xml;
using System.Xml.Serialization;

using Beagle;
using Beagle.Util;
using FSQ = Beagle.Daemon.FileSystemQueryable.FileSystemQueryable;

namespace Beagle.Daemon 
{
	class BuildIndex 
	{
		static string [] argv;

		static bool arg_recursive = false, arg_debug = false, arg_cache_text = false, arg_disable_filtering = false, arg_disable_restart = false;

		static Hashtable remap_table = new Hashtable ();

		static string arg_output, arg_tag, arg_source;

		/////////////////////////////////////////////////////////
		
		static FileAttributesStore_Sqlite backing_fa_store;
		static FileAttributesStore fa_store;

		static LuceneIndexingDriver driver;

		static bool crawling = true, indexing = true, shutdown = false, restart = false;

		static ArrayList allowed_patterns = new ArrayList ();
		static ArrayList denied_patterns = new ArrayList ();

		static Queue pending_files = new Queue ();
		static Queue pending_directories = new Queue ();
		
		const int BATCH_SIZE = 30;
		
		/////////////////////////////////////////////////////////
		
		static void Main (string [] args)
		{
			try {
				DoMain (args);
			} catch (Exception ex) {
				Logger.Log.Error ("Unhandled exception thrown.  Exiting immediately.");
				Logger.Log.Error (ex);
				Environment.Exit (1);
			}
		}

		static void DoMain (string [] args)
		{
			if (args.Length < 2)
				PrintUsage ();
		
			int i = 0;
			while (i < args.Length) {
			
				string arg = args [i];
				++i;
				string next_arg = i < args.Length ? args [i] : null;
			
				switch (arg) {
				case "-h":
				case "--help":
					PrintUsage ();
					break;
					
				case "--tag":
					if (next_arg != null)
						arg_tag = next_arg;
					++i;
					break;
					
				case "-r":
				case "--recursive":
					arg_recursive = true;
					break;
					
				case "--enable-text-cache":
					arg_cache_text = true;
					break;

				case "--remap":
					if (next_arg == null) 
						break;
					
					int j = next_arg.IndexOf (":");

					if (j == -1) {
						Logger.Log.Error ("Invalid remap argument: {0}", next_arg);
						Environment.Exit (1);
					}
					
					remap_table [next_arg.Substring (0, j)] = next_arg.Substring (j+1);

					++i;
					break;

				case "--target":
					if (next_arg != null)
						arg_output = Path.IsPathRooted (next_arg) ? next_arg : Path.GetFullPath (next_arg);					
					++i;
					break;

				case "--disable-filtering":
					arg_disable_filtering = true;
					break;

				case "--allow-pattern":
					if (next_arg == null)
						break;

					if (next_arg.IndexOf (',') != -1) {
						foreach (string pattern in next_arg.Split (','))
							allowed_patterns.Add (new ExcludeItem (ExcludeType.Pattern, pattern));
						
					} else {
						allowed_patterns.Add (new ExcludeItem (ExcludeType.Pattern, next_arg));
					}
					
					++i;
					break;

				case "--deny-pattern":
					if (next_arg == null)
						break;

					if (next_arg.IndexOf (',') != -1) {
						foreach (string pattern in next_arg.Split (','))
							denied_patterns.Add (new ExcludeItem (ExcludeType.Pattern, pattern));

					} else {
						denied_patterns.Add (new ExcludeItem (ExcludeType.Pattern, next_arg));
					}

					++i;
					break;

				case "--disable-restart":
					arg_disable_restart = true;
					break;

				case "--source":
					if (next_arg == null)
						break;

					arg_source = next_arg;
					++i;
					break;

				default:
					string path = Path.IsPathRooted (arg) ? arg : Path.GetFullPath (arg);
					
					if (Directory.Exists (path))
						pending_directories.Enqueue (new DirectoryInfo (path));
					else if (File.Exists (path))
						pending_files.Enqueue (new FileInfo (path));
					break;
				}
			}
			
			argv = args;
			
			/////////////////////////////////////////////////////////
				
			if (arg_output == null) {
				Logger.Log.Error ("--target must be specified");
				Environment.Exit (1);
			}

			foreach (FileSystemInfo info in pending_directories) {
				if (Path.GetFullPath (arg_output) == info.FullName) {
					Logger.Log.Error ("Target directory cannot be one of the source paths.");
					Environment.Exit (1);
				}
			}

			foreach (FileSystemInfo info in pending_files) {
				if (Path.GetFullPath (arg_output) == info.FullName) {
					Logger.Log.Error ("Target directory cannot be one of the source paths.");
					Environment.Exit (1);
				}
			}
			
			if (!Directory.Exists (Path.GetDirectoryName (arg_output))) {
				Logger.Log.Error ("Index directory not available for construction: {0}", arg_output);
				Environment.Exit (1);
			}

			// Set the IO priority to idle so we don't slow down the system
			IoPriority.SetIdle ();
			
			driver = new LuceneIndexingDriver (arg_output);
			driver.TextCache = (arg_cache_text) ? new TextCache (arg_output) : null;

			backing_fa_store = new FileAttributesStore_Sqlite (driver.TopDirectory, driver.Fingerprint);
			fa_store = new FileAttributesStore (backing_fa_store);
			
			// Set up signal handlers
			SetupSignalHandlers ();

			Thread crawl_thread, index_thread, monitor_thread = null;

			// Start the thread that does the crawling
			crawl_thread = ExceptionHandlingThread.Start (new ThreadStart (CrawlWorker));

			// Start the thread that does the actual indexing
			index_thread = ExceptionHandlingThread.Start (new ThreadStart (IndexWorker));

			if (!arg_disable_restart) {
				// Start the thread that monitors memory usage.
				monitor_thread = ExceptionHandlingThread.Start (new ThreadStart (MemoryMonitorWorker));
			}

			// Join all the threads so that we know that we're the only thread still running
			crawl_thread.Join ();
			index_thread.Join ();
			if (monitor_thread != null)
				monitor_thread.Join ();

			if (restart) {
				Logger.Log.Debug ("Restarting helper");
				Process p = new Process ();
				p.StartInfo.UseShellExecute = false;
				// FIXME: Maybe this isn't the right way to do things?  It should be ok,
				// the PATH is inherited from the shell script which runs mono itself.
				p.StartInfo.FileName = "mono";
				p.StartInfo.Arguments = String.Join (" ", Environment.GetCommandLineArgs ());
				p.Start ();
			}
		}
		
		/////////////////////////////////////////////////////////////////
		
		static void CrawlWorker ()
		{
			Logger.Log.Debug ("Starting CrawlWorker");
			
			
			int count_dirs = 0;
			
			while (pending_directories.Count > 0) {
				DirectoryInfo dir = (DirectoryInfo) pending_directories.Dequeue ();
				
				try {
					if (arg_recursive)
						foreach (DirectoryInfo subdir in DirectoryWalker.GetDirectoryInfos (dir))
							if (!Ignore (subdir)
							    && !FileSystem.IsSymLink (subdir.FullName))
								pending_directories.Enqueue (subdir);
					
					foreach (FileInfo file in DirectoryWalker.GetFileInfos (dir))
						if (!Ignore (file)
						    && !FileSystem.IsSymLink (file.FullName))
							pending_files.Enqueue (file);
					
				} catch (DirectoryNotFoundException e) {}
				
				if (shutdown)
					break;
				
				count_dirs++;
			}

			Logger.Log.Debug ("Scanned {0} files in {1} directories", pending_files.Count, count_dirs);
			Logger.Log.Debug ("CrawlWorker Done");

			crawling = false;
		}
		
		/////////////////////////////////////////////////////////////////

		static IndexerReceipt [] FlushIndexer (IIndexer indexer, IndexerRequest request)
		{
			IndexerReceipt [] receipts;
			receipts = indexer.Flush (request);

			ArrayList pending_children;
			pending_children = new ArrayList ();

			foreach (IndexerReceipt raw_r in receipts) {

				if (raw_r is IndexerAddedReceipt) {
					// Update the file attributes 
					IndexerAddedReceipt r = (IndexerAddedReceipt) raw_r;

					Indexable indexable = request.GetByUri (r.Uri);

					// We don't need to write out any file attributes for
					// children.
					if (indexable.ParentUri != null) 
						continue;

					string path = r.Uri.LocalPath;
					
					FileAttributes attr;
					attr = fa_store.ReadOrCreate (path);

					attr.LastWriteTime = indexable.Timestamp;
					attr.FilterName = r.FilterName;
					attr.FilterVersion = r.FilterVersion;

					fa_store.Write (attr);
					
				} else if (raw_r is IndexerChildIndexablesReceipt) {
					// Add any child indexables back into our indexer
					IndexerChildIndexablesReceipt r = (IndexerChildIndexablesReceipt) raw_r;
					pending_children.AddRange (r.Children);
				}
			}

			request.Clear (); // clear out the old request
			foreach (Indexable i in pending_children) // and then add the children
				request.Add (i);
			
			return receipts;
		}
		
		static void IndexWorker ()
		{
			Logger.Log.Debug ("Starting IndexWorker");
			
			Indexable indexable;
			IndexerRequest pending_request;
			pending_request = new IndexerRequest ();
			
			while (!shutdown) {
				if (pending_files.Count > 0) {
					FileInfo file = (FileInfo) pending_files.Dequeue ();
					Uri uri = UriFu.PathToFileUri (file.FullName);
					
					// Check that we really should be indexing the file
					if (!file.Exists || Ignore (file) || fa_store.IsUpToDate (file.FullName))
						continue;

					// Create the indexable and add the standard properties we
					// use in the FileSystemQueryable.
					indexable = new Indexable (uri);
					indexable.Timestamp = file.LastWriteTimeUtc;
					FSQ.AddStandardPropertiesToIndexable (indexable, file.Name, Guid.Empty, false);

					// Disable filtering and only index file attributes
					if (arg_disable_filtering)
						indexable.Filtering = IndexableFiltering.Never;
					
					// Tag the item for easy identification (for say, removal)
					if (arg_tag != null)
						indexable.AddProperty (Property.NewKeyword("Tag", arg_tag));

					if (arg_source == null) {
						DirectoryInfo dir = new DirectoryInfo (StringFu.SanitizePath (arg_output));
						arg_source = dir.Name;
					}

					indexable.Source = arg_source;
					
					pending_request.Add (indexable);
					
					if (pending_request.Count >= BATCH_SIZE) {
						Logger.Log.Debug ("Flushing driver, {0} items in queue", pending_request.Count);
						FlushIndexer (driver, pending_request);
						// FlushIndexer clears the pending_request
					}

				} else if (crawling) {
					//Logger.Log.Debug ("IndexWorker: La la la...");
					Thread.Sleep (50);
				} else {
					break;
				}
			}

			// Call Flush until our request is empty.  We have to do this in a loop
			// because children can get added back to the pending request in a flush.
			while (pending_request.Count > 0)
				FlushIndexer (driver, pending_request);

			backing_fa_store.Flush ();

			driver.OptimizeNow ();

			Logger.Log.Debug ("IndexWorker Done");

			indexing = false;
		}

		/////////////////////////////////////////////////////////////////

		static void MemoryMonitorWorker ()
		{
			int vmrss_original = SystemInformation.VmRss;

			const double threshold = 5.0;
			int last_vmrss = 0;

			while (! shutdown && (crawling || indexing)) {

				// Check resident memory usage
				int vmrss = SystemInformation.VmRss;
				double size = vmrss / (double) vmrss_original;
				if (vmrss != last_vmrss)
					Logger.Log.Debug ("Size: VmRSS={0:0.0} MB, size={1:0.00}, {2:0.0}%",
							  vmrss/1024.0, size, 100.0 * (size - 1) / (threshold - 1));
				last_vmrss = vmrss;
				if (size > threshold) {
					Logger.Log.Debug ("Process too big, shutting down!");
					restart = true;
					shutdown = true;
					return;
				} else {
					Thread.Sleep (3000);
				}
			}
		}

		/////////////////////////////////////////////////////////////////
		
		// From BeagleDaemon.cs

		static void SetupSignalHandlers ()
		{
			// Force OurSignalHandler to be JITed
			OurSignalHandler (-1);
			
			// Set up our signal handler
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGINT, OurSignalHandler);
			Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGTERM, OurSignalHandler);
			if (Environment.GetEnvironmentVariable("BEAGLE_THERE_BE_NO_QUITTIN") == null)
				Mono.Unix.Native.Stdlib.signal (Mono.Unix.Native.Signum.SIGQUIT, OurSignalHandler);
		}
		
		static void OurSignalHandler (int signal)
		{
			// This allows us to call OurSignalHandler w/o doing anything.
			// We want to call it once to ensure that it is pre-JITed.
			if (signal < 0)
				return;

			Logger.Log.Debug ("Shutdown Requested");
			shutdown = true;
		}

		/////////////////////////////////////////////////////////////////
		
		static void PrintUsage ()
		{
			string usage = 
				"beagle-build-index: Build an index.\n" + 
				"Web page: http://www.gnome.org/projects/beagle\n" +
				"Copyright (C) 2005 Novell, Inc.\n\n";
			
			usage += 
				"Usage: beagle-build-index [OPTIONS] --target <index_path> <path> [path]\n\n" +
				"Options:\n" +
				"  --source [name]\t\tThe index's source name.  Defaults to the target directory name\n" +
				"  --remap [path1:path2]\t\tRemap data paths to fit target. \n" +
				"  --tag [tag]\t\t\tTag index data for identification.\n" + 
				"  --recursive\t\t\tCrawl source path recursivly.\n" + 
				"  --enable-text-cache\t\tBuild text-cache of documents used for snippets.\n" + 
				"  --disable-filtering\t\tDisable all filtering of files. Only index attributes.\n" + 
				"  --allow-pattern [pattern]\tOnly allow files that match the pattern to be indexed.\n" + 
				"  --deny-pattern [pattern]\tKeep any files that match the pattern from being indexed.\n" + 
				"  --disable-restart\t\tDon't restart when memory usage gets above a certain threshold.\n" +
				"  --debug\t\t\tEcho verbose debugging information.\n";
			
			Console.WriteLine (usage);
			Environment.Exit (0);
		}
		
		/////////////////////////////////////////////////////////
		
		static Uri RemapUri (Uri uri)
		{
			// FIXME: This is ghetto
			foreach (DictionaryEntry dict in remap_table) {
				if (uri.LocalPath.IndexOf ((string) dict.Key) == -1)
					continue;
				return new Uri (uri.LocalPath.Replace ((string) dict.Key, (string) dict.Value));
			}
			return uri;
		}
		
		static bool Ignore (DirectoryInfo directory)
		{
			if (directory.Name.StartsWith ("."))
				return true;
			
			return false;
		}

		static bool Ignore (FileInfo file)
		{
			if (file.Name.StartsWith ("."))
				return true;

			if (FileSystem.IsSymLink (file.FullName))
				return true;

			if (allowed_patterns.Count > 0) {
				foreach (ExcludeItem pattern in allowed_patterns)
					if (pattern.IsMatch (file.Name))
						return false;
				
				return true;
			}

			foreach (ExcludeItem pattern in denied_patterns)
				if (pattern.IsMatch (file.Name))
					return true;

			// FIXME: Add more stuff here
			
			return false;
		}
	}
}
