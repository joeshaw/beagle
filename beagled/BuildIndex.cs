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
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;

using System.Xml;
using System.Xml.Serialization;

using Beagle;
using Beagle.Util;
using FSQ = Beagle.Daemon.FileSystemQueryable.FileSystemQueryable;

namespace Beagle.Daemon 
{
	class BuildIndex 
	{
		static bool arg_recursive = false, arg_debug = false, arg_cache_text = false, arg_disable_filtering = false;

		static Hashtable remap_table = new Hashtable ();

		static string arg_output, arg_tag;
		
		/////////////////////////////////////////////////////////
		
		static FileAttributesStore_Sqlite backing_fa_store;
		static FileAttributesStore fa_store;

		static LuceneIndexingDriver driver;

		static bool crawling = true, shutdown = false;

		static Queue pending_files = new Queue ();
		static Queue pending_directories = new Queue ();
		
		const int BATCH_SIZE = 30;
		
		/////////////////////////////////////////////////////////
		
		static void Main (string [] args)
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
						Console.WriteLine ("Invalid remap argument: {0}", next_arg);
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

				default:
					string path = Path.IsPathRooted (arg) ? arg : Path.GetFullPath (arg);
					
					if (Directory.Exists (path))
							pending_directories.Enqueue (new DirectoryInfo (path));
					else if (File.Exists (path))
						pending_files.Enqueue (new FileInfo (path));
					break;
				}
			}

			/////////////////////////////////////////////////////////
			
			if (!Directory.Exists (Path.GetDirectoryName (arg_output))) {
				Console.WriteLine ("Index directory not available for construction: {0}", arg_output);
				Environment.Exit (1);
			}
			
			driver = new LuceneIndexingDriver (arg_output);
			driver.TextCache = (arg_cache_text) ? new TextCache (arg_output) : null;

			backing_fa_store = new FileAttributesStore_Sqlite (driver.TopDirectory, driver.Fingerprint);
			fa_store = new FileAttributesStore (backing_fa_store);
			
			// Set up signal handlers
			SetupSignalHandlers ();

			// Start the thread that does the crawling
			ExceptionHandlingThread.Start (new ThreadStart (CrawlWorker));

			// Start the thread that does the actual indexing
			ExceptionHandlingThread.Start (new ThreadStart (IndexWorker));
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
							if (!Ignore (subdir))
								pending_directories.Enqueue (subdir);
					
					foreach (FileInfo file in DirectoryWalker.GetFileInfos (dir))
						if (!Ignore (file))
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

		static IndexerReceipt [] FlushIndexer (IIndexer indexer)
		{
			IndexerReceipt [] receipts;
			receipts = indexer.FlushAndBlock ();


			foreach (IndexerReceipt raw_r in receipts) {

				if (raw_r is IndexerAddedReceipt) {
					// Update the file attributes 
					IndexerAddedReceipt r = (IndexerAddedReceipt) raw_r;

					string path = r.Uri.LocalPath;
					
					FileAttributes attr;
					attr = fa_store.ReadOrCreate (path);

					attr.LastWriteTime = FileSystem.GetLastWriteTime (path);
					attr.FilterName = r.FilterName;
					attr.FilterVersion = r.FilterVersion;

					fa_store.Write (attr);

				} else if (raw_r is IndexerChildIndexablesReceipt) {
					// Add any child indexables back into our indexer
					IndexerChildIndexablesReceipt r = (IndexerChildIndexablesReceipt) raw_r;
					foreach (Indexable i in r.Children)
						indexer.Add (i);
				}
			}
			
			return receipts;
		}
		
		static void IndexWorker ()
		{
			Logger.Log.Debug ("Starting IndexWorker");
			
			Indexable indexable;
			int pending_adds = 0;
			
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
					FSQ.AddStandardPropertiesToIndexable (indexable, file.Name, Guid.Empty, false);

					// Disable filtering and only index file attributes
					if (arg_disable_filtering)
						indexable.Filtering = IndexableFiltering.Never;
					
					// Tag the item for easy identification (for say, removal)
					if (arg_tag != null)
						indexable.AddProperty (Property.NewKeyword("Tag", arg_tag));
					
					driver.Add (indexable);
					++pending_adds;
					
					if (pending_adds % BATCH_SIZE == 0) {
						Logger.Log.Debug ("Flushing driver, {0} items in queue", pending_files.Count);
						FlushIndexer (driver);
						pending_adds = 0;
					}
				} else if (crawling) {
					//Logger.Log.Debug ("IndexWorker: La la la...");
					Thread.Sleep (50);
				} else {
					break;
				}
			}

			// Call Flush one last time.
			// This should be a totally safe no-op if there are no pending operations.
			// FIXME: This is incorrect.  We will drop any children in the final flush.
			FlushIndexer (driver);

			backing_fa_store.Flush ();

			Logger.Log.Debug ("IndexWorker Done");
		}

		/////////////////////////////////////////////////////////////////
		
		// From BeagleDaemon.cs

                // The integer values of the Mono.Posix.Signal enumeration don't actually
		// match the Linux signal numbers of Linux.  Oops!
		// This is fixed in Mono.Unix, but for the moment we want to maintain
		// compatibility with mono 1.0.x.
		const int ACTUAL_LINUX_SIGINT  = 2;
		const int ACTUAL_LINUX_SIGQUIT = 3;
		const int ACTUAL_LINUX_SIGTERM = 15;
		
		static void SetupSignalHandlers ()
		{
			// Force OurSignalHandler to be JITed
			OurSignalHandler (-1);
			
			// Set up our signal handler
			Mono.Posix.Syscall.sighandler_t sig_handler;
			sig_handler = new Mono.Posix.Syscall.sighandler_t (OurSignalHandler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGINT, sig_handler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGQUIT, sig_handler);
                        Mono.Posix.Syscall.signal (ACTUAL_LINUX_SIGTERM, sig_handler);
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
				"  --remap [path1:path2]\tRemap data paths to fit target. \n" +
				"  --tag [tag]\t\tTag index data for identification.\n" + 
				"  --recursive\t\tCrawl source path recursivly.\n" + 
				"  --enable-text-cache\t\tBuild text-cache of documents used for snippets.\n" + 
				"  --disable-filtering\t\tDisable all filtering of files. Only index attributes.\n" + 
				"  --debug\t\tEcho verbose debugging information.\n";
			
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

			// FIXME: Add more stuff here
			
			return false;
		}
	}
}
