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

// FIXME: Implement a shared textcache

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Collections;

using System.Xml;
using System.Xml.Serialization;

using Beagle;
using Beagle.Util;

namespace Beagle.Daemon 
{
	class BuildIndex 
	{
		static bool arg_recursive = false, arg_debug = false;
		
		static string arg_prefix = null;
		
		static string arg_path, arg_output, arg_tag, arg_configuration;
		
		/////////////////////////////////////////////////////////
		
		static FileAttributesStore_Sqlite backing_fa_store;
		static FileAttributesStore fa_store;
		static LuceneDriver driver;
		
		static bool crawling = true;
		static Queue pending_files = new Queue ();
		
		const int BATCH_SIZE = 30;
		
		/////////////////////////////////////////////////////////
		
		static void Main (string [] args)
		{
			if (args.Length < 2)
				PrintUsage ();
		
			int i = 0;
			while (i < args.Length-2) {
			
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
					
				case "--prefix":
					if (next_arg != null) 
						arg_prefix = next_arg;
					++i;
					break;
				}
			}
			
			arg_path = args [i];
			if (!Path.IsPathRooted (arg_path))
				arg_path = Path.GetFullPath (arg_path);
			
			arg_output = args [++i];
			if (!Path.IsPathRooted (arg_output))
				arg_output = Path.GetFullPath (arg_output);
				
			/////////////////////////////////////////////////////////
			
			if (!Directory.Exists (Path.GetDirectoryName (arg_output))) {
				Console.WriteLine ("Index directory not available for construction: {0}", arg_output);
				Environment.Exit (1);
			}
			
			driver = new LuceneDriver (arg_output);
			driver.ChildIndexableEvent += new IIndexerChildIndexableHandler (OnChildIndexableEvent);

			backing_fa_store = new FileAttributesStore_Sqlite (driver.IndexDirectory, driver.Fingerprint);
			fa_store = new FileAttributesStore (backing_fa_store);
			
			// Start the thread that does the crawling
			ExceptionHandlingThread.Start (new ThreadStart (CrawlWorker));

			// Start the thread that does the actual indexing
			ExceptionHandlingThread.Start (new ThreadStart (IndexWorker));
		}
		
		/////////////////////////////////////////////////////////////////
		
		static void CrawlWorker ()
		{
			Logger.Log.Debug ("Starting CrawlWorker");
			
			if (Directory.Exists (arg_path)) {
				if (arg_recursive) {
					int count_dirs = 0;
					Queue pending_dirs = new Queue ();
					pending_dirs.Enqueue (arg_path);
					
					while (pending_dirs.Count > 0) {
						string dir = (string) pending_dirs.Dequeue ();
						
						try {
							foreach (string subdir in DirectoryWalker.GetDirectories (dir))
								if (!Ignore (subdir))
									pending_dirs.Enqueue (subdir);

							foreach (FileInfo file in DirectoryWalker.GetFileInfos (dir))
								if (!Ignore (file.FullName))
									pending_files.Enqueue (file);

						} catch (DirectoryNotFoundException e) {}
						
						count_dirs++;
					}
					Logger.Log.Debug ("Scanned {0} files in {1} directories", pending_files.Count, count_dirs);
				} else {
					foreach (FileInfo file in DirectoryWalker.GetFileInfos (arg_path))
						pending_files.Enqueue (file);
					Logger.Log.Debug ("Scanned {0} files", pending_files.Count);
				}
			} else if (File.Exists (arg_path)) {
				pending_files.Enqueue (new FileInfo (arg_path));
			} else {
				Console.WriteLine ("No such file or directory: {0}", arg_path);
				Environment.Exit (1);
			}
			
			Logger.Log.Debug ("CrawlWorker Done");
			crawling = false;
		}
		
		/////////////////////////////////////////////////////////////////
		
		static void IndexWorker ()
		{
			Logger.Log.Debug ("Starting IndexWorker");
			
			Indexable indexable;
			
			while (true) {
				if (pending_files.Count > 0) {
					FileInfo file = (FileInfo) pending_files.Dequeue ();
					Uri uri = UriFu.PathToFileUri (file.FullName);
					
					// Check that we really should be indexing the file
					if (!file.Exists || Ignore (file.FullName) || fa_store.IsUpToDate (file.FullName))
						continue;

					// Create the indexable
					indexable = new Indexable (uri);
					indexable.Uri = (arg_prefix == null) ? uri : RemapUri (uri);
					indexable.ContentUri = uri;
					indexable.CacheContent = false;
					
					// Tag the item for easy identification (for say, removal)
					if (arg_tag != null)
						indexable.AddProperty (Property.NewKeyword("Tag", arg_tag));
					
					driver.Add (indexable);
					
					fa_store.AttachTimestamp (file.FullName, FileSystem.GetLastWriteTime (file.FullName));
					
					if (driver.PendingAdds % BATCH_SIZE == 0) {
						Logger.Log.Debug ("Flushing driver, {0} items in queue", pending_files.Count);
						driver.Flush ();
					}
				} else if (crawling) {
					Logger.Log.Debug ("IndexWorker: La la la...");
					Thread.Sleep (50);
				} else {
					break;
				}
			}

			// Flush out any pending changes in either the
			// LuceneDriver or the sqlite attributes database.
			while (driver.PendingAdds != 0)
				driver.Flush ();

			backing_fa_store.Flush ();

			Logger.Log.Debug ("IndexWorker Done");
		}

		static void OnChildIndexableEvent (Indexable[] indexables) {
			foreach (Indexable indexable in indexables) {
				indexable.StoreStream ();
				driver.Add (indexable);
			}
		}
		
		/////////////////////////////////////////////////////////////////
		
		static void PrintUsage ()
		{
			string usage = 
				"beagle-build-index: Build an index.\n" + 
				"Web page: http://www.gnome.org/projects/beagle\n" +
				"Copyright (C) 2005 Novell, Inc.\n\n";
			
			usage += 
				"Usage: beagle-build-index [OPTIONS] <data path> <index path>\n\n" +
				"Options:\n" +
				"  --prefix [prefix]\tTarget prefix for Uri remapping.\n" + 
				"  --tag [tag]\t\tTag index data for identification.\n" + 
				"  --recursive\t\tCrawl source path recursivly.\n" + 
				"  --debug\t\tEcho verbose debugging information.\n";
			
			
			Console.WriteLine (usage);
			Environment.Exit (0);
		}
		
		/////////////////////////////////////////////////////////
		
		static Uri RemapUri (Uri current_uri)
		{
			if (arg_prefix == null)
				return current_uri;
			
			return new Uri (Path.Combine (arg_prefix, current_uri.LocalPath.Substring (arg_path.Length)));
		}
		
		static bool Ignore (string path)
		{
			if (FileSystem.IsSymLink (path))
				return true;

			// FIXME: Add more stuff here
			
			return false;
		}
	}
}
