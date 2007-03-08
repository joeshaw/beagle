//
// NautilusMetadataQueryable.cs
//
// Copyright (C) 2007 Novell, Inc.
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
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Daemon.NautilusMetadataQueryable {

	[QueryableFlavor (Name="NautilusMetadata", Domain=QueryDomain.Local, RequireInotify=false,
			  DependsOn="Files")]
	public class NautilusMetadataQueryable : ExternalMetadataQueryable, IIndexableGenerator  {

		private string nautilus_dir;
		private FileSystemQueryable.FileSystemQueryable target_queryable;
		private FileAttributesStore fa_store;

		public NautilusMetadataQueryable () : base ("Files")
		{
			nautilus_dir = Path.Combine (Path.Combine (PathFinder.HomeDir, ".nautilus"), "metafiles");
		}

		public override void Start () 
		{
                        base.Start ();

			// The FSQ
			this.target_queryable = (FileSystemQueryable.FileSystemQueryable) TargetQueryable.IQueryable;

			string storage_path = Path.Combine (PathFinder.IndexDir, "NautilusMetadata");
			string fingerprint_file = Path.Combine (storage_path, "fingerprint");
			string fingerprint;

			if (! Directory.Exists (storage_path)) {
				Directory.CreateDirectory (storage_path);
				fingerprint = GuidFu.ToShortString (Guid.NewGuid ());
				StreamWriter writer = new StreamWriter (fingerprint_file);
				writer.WriteLine (fingerprint);
				writer.Close ();
			} else {
				StreamReader reader = new StreamReader (fingerprint_file);
				fingerprint = reader.ReadLine ();
				reader.Close ();
			}

			string fsq_fingerprint = this.target_queryable.IndexFingerprint;

			IFileAttributesStore ifa_store;

			if (ExtendedAttribute.Supported)
				ifa_store = new FileAttributesStore_ExtendedAttribute (fingerprint + "-" + fsq_fingerprint);
			else {
				string path = Path.Combine (PathFinder.IndexDir, "NautilusMetadata");

				if (! Directory.Exists (path))
					Directory.CreateDirectory (path); 

				ifa_store = new FileAttributesStore_Sqlite (path, fingerprint + "-" + fsq_fingerprint);
			}

			fa_store = new FileAttributesStore (ifa_store);

			if (! Directory.Exists (nautilus_dir))
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForExistence));
			else
				ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}

		private void StartWorker ()
		{
			if (Inotify.Enabled) {
				// Nautilus creates a temporary file, writes
				// out the content, and moves it on top of any
				// previous file.  Files are never removed.  So
				// we only need to watch the MovedTo event.
				Inotify.EventType mask = Inotify.EventType.MovedTo;
				Inotify.Subscribe (nautilus_dir, OnInotifyEvent, mask);
			}

			// Start our crawler process
			Scheduler.Task task;
			task = this.target_queryable.NewAddTask (this);
			task.Tag = "Crawling Nautilus Metadata";
			task.Source = this;

			ThisScheduler.Add (task);

			Log.Info ("Nautilus metadata backend started");
		}

		private bool CheckForExistence ()
		{
			if (!Directory.Exists (nautilus_dir))
				return true;
			
			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));

			return false;
		}

		/////////////////////////////////////////////////

		public Indexable GetIndexable (NautilusTools.NautilusMetadata nm)
		{
			Uri internal_uri = this.target_queryable.ExternalToInternalUri (nm.Uri);

			Log.Debug ("Mapped {0} -> {1}", nm.Uri, internal_uri);

			if (internal_uri == null) {
				// If we didn't match an already indexed file,
				// add an entry for it in the FSQ.
				FileAttributes attr;
				attr = this.target_queryable.FileAttributesStore.ReadOrCreate (nm.Uri.LocalPath);
				this.target_queryable.FileAttributesStore.Write (attr);
				internal_uri = GuidFu.ToUri (attr.UniqueId);
			}

			Indexable indexable = new Indexable (internal_uri);
			indexable.Type = IndexableType.PropertyChange;
			indexable.DisplayUri = nm.Uri;

			Property prop;

			// Reset the notes property.
			if (nm.Notes == null)
				nm.Notes = String.Empty;

			prop = Property.New ("nautilus:notes", nm.Notes);
			prop.IsMutable = true;
			prop.IsPersistent = true;
			indexable.AddProperty (prop);

			foreach (string emblem in nm.Emblems) {
				prop = Property.NewKeyword ("nautilus:emblem", emblem);
				prop.IsMutable = true;
				prop.IsPersistent = true;
				indexable.AddProperty (prop);
			}

			// We add an empty keyword so that the property is reset
			if (nm.Emblems.Count == 0) {
				prop = Property.NewKeyword ("nautilus:emblem", String.Empty);
				prop.IsMutable = true;
				prop.IsPersistent = true;
				indexable.AddProperty (prop);
			}

			return indexable;

		}

		/////////////////////////////////////////////////

		private void OnInotifyEvent (Inotify.Watch watch,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (subitem == "")
				return;

			if (Path.GetExtension (subitem) != ".xml")
				return;

			// We're only handling MovedTo events here.
			string file = Path.Combine (path, subitem);

			DateTime last_checked = DateTime.MinValue;

			FileAttributes attr;
			attr = fa_store.Read (file);
			if (attr != null)
				last_checked = attr.LastWriteTime;

			foreach (NautilusTools.NautilusMetadata nm in NautilusTools.GetMetadata (file, last_checked)) {
				Indexable indexable = GetIndexable (nm);

				Scheduler.Task task;
				task = this.target_queryable.NewAddTask (indexable);
				task.Priority = Scheduler.Priority.Immediate;

				ThisScheduler.Add (task);
			}
		}

		/////////////////////////////////////////////////

		// IIndexableGenerator implementation
		public string StatusName {
			get { return "NautilusMetadataQueryable"; }
		}

		private IEnumerator metafiles = null;
		private IEnumerator metadata = null;
		
		public void PostFlushHook () { }

		public bool HasNextIndexable ()
		{
			if (metadata != null) {
				if (metadata.MoveNext ())
					return true;
				else {
					metadata = null;
					fa_store.AttachLastWriteTime ((string) metafiles.Current, DateTime.UtcNow);
				}
			}

			while (metadata == null) {
				if (metafiles == null)
					metafiles = DirectoryWalker.GetFiles (nautilus_dir).GetEnumerator ();

				if (! metafiles.MoveNext ())
					return false;

				string file = (string) metafiles.Current;

				if (fa_store.IsUpToDate (file))
					continue;

				metadata = NautilusTools.GetMetadata ((string) metafiles.Current).GetEnumerator ();

				if (metadata.MoveNext ())
					return true;
				else {
					metadata = null;
					fa_store.AttachLastWriteTime (file, DateTime.UtcNow);
				}
			}

			return false; // Makes the compiler happy
		}

		public Indexable GetNextIndexable ()
		{
			NautilusTools.NautilusMetadata nm = (NautilusTools.NautilusMetadata) metadata.Current;

			return GetIndexable (nm);
		}

	}
}
