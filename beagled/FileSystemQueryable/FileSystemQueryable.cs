//
// FileSystemQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Reflection;
using System.IO;

using Beagle.Daemon;
using BU = Beagle.Util;


namespace Beagle.Daemon.FileSystemQueryable {

	public class FileSystemChangeData : IQueryableChangeData {
		public ArrayList AddedUris = new ArrayList ();
		public ArrayList SubtractedUris = new ArrayList ();
	}

	[QueryableFlavor (Name="FileSystemQueryable", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : IQueryable {

		public event IQueryableChangedHandler ChangedEvent;

		IndexerQueue indexerQueue;
		Indexer indexer;

		FileSystemEventMonitor monitor = new FileSystemEventMonitor ();
		BU.FileMatcher doNotIndex = new BU.FileMatcher ();

		uint changedId = 0;
		FileSystemChangeData changeData = null;

		public FileSystemQueryable ()
		{
			indexerQueue = new IndexerQueue ();
			ScanAssemblyForHandlers (Assembly.GetExecutingAssembly ());

			indexer = new Indexer (indexerQueue);
			
			DBusisms.Service.RegisterObject (indexer,
							 Beagle.DBusisms.IndexerPath);

			// Set up file system monitor
			string home = Environment.GetEnvironmentVariable ("HOME");
			monitor.Subscribe (home);
			monitor.Subscribe (Path.Combine (home, "Desktop"));
			monitor.Subscribe (Path.Combine (home, "Documents"));

			monitor.FileSystemEvent += OnFileSystemEvent;
		}

		private void ScanAssemblyForHandlers (Assembly assembly)
		{
			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (PreIndexHandler))) {

					PreIndexHandler handler = (PreIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PreIndexingEvent += handler.Run;
				}
				if (t.IsSubclassOf (typeof (PostIndexHandler))) {
					PostIndexHandler handler = (PostIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PostIndexingEvent += handler.Run;
				}
			}
		}

		private void OnFileSystemEvent (object source, FileSystemEventType eventType,
						string oldPath, string newPath)
		{
			if (changeData == null)
				changeData = new FileSystemChangeData ();

			Console.WriteLine ("Got event {0} {1} {2}", eventType, oldPath, newPath);

			if (eventType == FileSystemEventType.Changed
			    || eventType == FileSystemEventType.Created) {

				if (doNotIndex.IsMatch (newPath)) {
					Console.WriteLine ("Ignoring {0}", newPath);
					return;
				}

				string uri = BU.StringFu.PathToQuotedFileUri (newPath);
				FilteredIndexable indexable = new FilteredIndexable (new Uri (uri, true));
				indexerQueue.ScheduleAdd (indexable);

				changeData.AddedUris.Add (indexable.Uri);

			} else if (eventType == FileSystemEventType.Deleted) {

				string uri = BU.StringFu.PathToQuotedFileUri (oldPath);
				indexerQueue.ScheduleRemoveByUri (uri);
				
				changeData.SubtractedUris.Add (new Uri (uri, false));
				
			} else {
				Console.WriteLine ("Unhandled!");
			}

			if (changedId != 0)
				GLib.Source.Remove (changedId);
			changedId = GLib.Timeout.Add (1000, new GLib.TimeoutHandler (FireChangedEvent));
		}

		private bool FireChangedEvent ()
		{
			changedId = 0;
			if (ChangedEvent != null && changeData != null)
				ChangedEvent (this, changeData);
			changeData = null;
			return false;
		}


		public string Name {
			get { return "FileSystemQueryable"; }
		}

		public bool AcceptQuery (QueryBody body)
		{
			return indexer.driver.AcceptQuery (body);
		}

		public void DoQuery (QueryBody body,
				     IQueryResult queryResult,
				     IQueryableChangeData iChangeData)
		{
			FileSystemChangeData changeData = (FileSystemChangeData) iChangeData;

			if (changeData == null) {
				indexer.driver.DoQuery (body, queryResult, null);
			} else {
				if (changeData.AddedUris.Count > 0)
					indexer.driver.DoQuery (body, queryResult, changeData.AddedUris);
				queryResult.Subtract (changeData.SubtractedUris);
			}
		}

	}

}
