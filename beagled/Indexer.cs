//
// Indexer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal// in the Software without restriction, including without limitation the rights
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
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE// SOFTWARE.
//

using DBus;
using System;
using System.IO;
using System.Collections;
using Beagle.Util;
using Beagle.Core;
using Beagle;
namespace BeagleDaemon
{

	public class Indexer : Beagle.Indexer 
	{
		IndexDriver driver = new IndexDriver ();

		// Contains Indexables
		ArrayList toBeIndexed = new ArrayList ();

		// Contains Obsoleted or Deleted Hits
		ArrayList toBeRemoved = new ArrayList ();
		
		ArrayList preCallouts = new ArrayList ();
		ArrayList postCallouts = new ArrayList ();

		int sinceOptimize = 0;
		const int optimizeCount = 10;

		public delegate void PreIndexingHandler (PreIndexHandlerArgs a);
		public event PreIndexingHandler PreIndexingEvent;

		public delegate void PostIndexingHandler (PostIndexHandlerArgs a);
		public event PostIndexingHandler PostIndexingEvent;
		
		void ScheduleAdd (Indexable indexable)
		{
			toBeIndexed.Add (indexable);
		}

		void ScheduleRemove (Hit hit)
		{
			if (hit == null)
				return;
			
			toBeRemoved.Add (hit);
		}

		void CallPostIndexingEvent (ArrayList indexables)
		{
			if (PostIndexingEvent == null)
				return;

			PostIndexHandlerArgs args = new PostIndexHandlerArgs ();
			foreach (Indexable i in indexables) {
				args.indexable = i;
				PostIndexingEvent (args);
					
			}
		}

		ArrayList CallPreIndexingEvent (ArrayList indexables)
		{
			if (PreIndexingEvent == null) 
				return indexables;

			ArrayList ret = new ArrayList ();
			PreIndexHandlerArgs args = new PreIndexHandlerArgs ();
			foreach (Indexable i in indexables) {
				args.indexable = i;
				args.shouldIndex = true;
				PreIndexingEvent (args);
				if (args.shouldIndex)
					ret.Add (i);
			}
			return ret;
		}


		void Flush () 
		{
			bool didSomething = false;
			
			toBeIndexed = CallPreIndexingEvent (toBeIndexed); 

			if (toBeIndexed.Count > 0) {
				driver.QuickAdd (toBeIndexed);
				didSomething = true;
			}
			
			if (toBeRemoved.Count > 0) {
				driver.Remove (toBeRemoved);
				toBeRemoved.Clear ();
				didSomething = true;
			}

			if (didSomething) {
				++sinceOptimize;
				if (sinceOptimize > optimizeCount) {
					driver.Optimize ();
					sinceOptimize = 0;
				}
			}
			CallPostIndexingEvent (toBeIndexed);
			toBeIndexed.Clear ();
		}

		void Index (FileInfo file)
		{
			string uri;
			uri = "file://" + file.FullName;
			
			Hit hit = driver.FindByUri (uri);
			
			DateTime changeTime = file.LastWriteTime;
			DateTime nautilusTime = NautilusTools.GetMetaFileTime (file.FullName);
			
			if (nautilusTime > changeTime)
				changeTime = nautilusTime;

			// If the file isn't newer than the hit, don't
			// even bother

			if (hit != null && !hit.IsObsoletedBy (changeTime))
				return;
			
			Indexable indexable = new IndexableFile (file.FullName);
			ScheduleAdd (indexable);
			ScheduleRemove (hit);
		}

		void IndexPath (string path) 
		{
			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);

			path = Path.GetFullPath (path);
			
			if (path == null) {
				return;
			}
			
			FileInfo file = new FileInfo (path);
			Index (file);
		}

		public override void IndexFile (string path)
		{
			IndexPath (path);
			Flush ();
		}

		public override void IndexFiles (string[] paths)
		{
			foreach (String path in paths) {
				IndexPath (path);
			}
			
			Flush ();
		}
	}
}
