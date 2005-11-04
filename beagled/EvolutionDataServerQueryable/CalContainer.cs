//
// CalContainer.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Globalization;
using System.IO;

using Beagle.Util;

using Evolution;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	public class CalContainer : Container {

		private Cal cal;
		private CalView cal_view;
		private Scheduler.Priority priority = Scheduler.Priority.Delayed;

		public CalContainer (Evolution.Source source, EvolutionDataServerQueryable queryable, string fingerprint) : base (source, queryable, fingerprint) { }

		public override bool OpenClient ()
		{
			if (!this.source.IsLocal ()) {
				Logger.Log.Debug ("Skipping remote calendar {0}", this.source.Uri);
				return false;
			}

			try {
				this.cal = new Cal (this.source, CalSourceType.Event);
				this.cal.Open (true);
			} catch (Exception e) {
				Logger.Log.Warn ("Unable to open calendar {0}: {1}", this.source.Uri, e.Message);
				return false;
			}

			return true;
		}

		public override void OpenView ()
		{
			this.cal_view = this.cal.GetCalView ("#t");

			this.cal_view.ObjectsAdded += OnObjectsAdded;
			this.cal_view.ObjectsModified += OnObjectsModified;
			this.cal_view.ObjectsRemoved += OnObjectsRemoved;
			this.cal_view.ViewDone += OnViewDone;

			this.cal_view.Start ();
		}

		public override void IndexAll ()
		{
			CalComponent[] event_list = this.cal.GetItems ("#t");

			Logger.Log.Debug ("Calendar has {0} items", event_list.Length);

			foreach (CalComponent cc in event_list)
				AddCalComponent (cc);
		}

		public override void IndexChanges ()
		{
			CalComponent[] added, changed;
			string[] removed;

			Logger.Log.Debug ("Getting calendar changes for {0}", this.source.Uri);
			this.cal.GetChanges ("beagle-" + this.fingerprint, out added, out changed, out removed);
			Logger.Log.Debug ("Calendar {0}: {1} added, {2} changed, {3} removed",
					  this.cal.Uri, added.Length, changed.Length, removed.Length);

			foreach (CalComponent cc in added)
				AddCalComponent (cc);

			foreach (CalComponent cc in changed)
				AddCalComponent (cc);

			foreach (string id in removed) {
				// FIXME: Broken in e-d-s right now
				//RemoveCalComponent (id);
			}
		}

		public override void Remove ()
		{
			Logger.Log.Debug ("Removing calendar source {0}", this.source.Uid);

			Property prop = Property.NewKeyword ("fixme:source_uid", this.source.Uid);
			this.queryable.RemovePropertyIndexable (prop);

			this.cal_view.Dispose ();
			this.cal.Dispose ();
		}

		private void OnObjectsAdded (object o, Evolution.ObjectsAddedArgs args)
		{
			foreach (CalComponent cc in CalUtil.CalCompFromICal (args.Objects.Handle, this.cal_view.Client))
				AddCalComponent (cc);
		}

		private void OnObjectsModified (object o, Evolution.ObjectsModifiedArgs args)
		{
			foreach (CalComponent cc in CalUtil.CalCompFromICal (args.Objects.Handle, this.cal_view.Client))
				AddCalComponent (cc);
		}

		private void OnObjectsRemoved (object o, Evolution.ObjectsRemovedArgs args)
		{
			// FIXME: This is a temporary workaround for the
			// fact that the evolution bindings return a
			// GLib.List with an object type, but there are
			// really strings in there.

			GLib.List id_list = new GLib.List (args.Uids.Handle,
							   typeof (string));

			foreach (string id in id_list)
				RemoveCalComponent (id);
		}

		private void OnViewDone (object o, Evolution.ViewDoneArgs args)
		{
			// Now that we're done synching with the original
			// state of the calendar, switch all new changes to
			// Immediate mode
			priority = Scheduler.Priority.Immediate;
		}

		/////////////////////////////////////
			
		// URI scheme is:
		// calendar:///?source-uid=<value>&comp-uid=<value>[&comp-rid=value]
		//
		// The Uri class sucks SO MUCH ASS.  It shits itself
		// on foo:///?bar so we have to insert something in
		// before "?bar".  This is filed as Ximian bug #76146.
		// Hopefully it is just a bug in Mono and not a
		// fundamental problem of the Uri class.  Fortunately
		// Evolution can handle the horribly mangled URIs
		// that come out of it.

		private Uri GetCalendarUri (CalComponent cc) {
			return GetCalendarUri (cc.Uid);
		}

		private Uri GetCalendarUri (string id) {
			return new Uri (String.Format ("calendar://uri-class-sucks/?source-uid={0}&comp-uid={1}",
						       this.source.Uid, id));
		}

		/////////////////////////////////////

		private void AddCalComponent (CalComponent cc)
		{
			Indexable indexable = CalComponentToIndexable (cc);

			this.queryable.AddIndexable (indexable, this.priority);
		}

		private void RemoveCalComponent (string id)
		{
			this.queryable.RemoveIndexable (GetCalendarUri (id));
		}

		/////////////////////////////////////

		private Indexable CalComponentToIndexable (CalComponent cc)
		{
			Indexable indexable = new Indexable (GetCalendarUri (cc));

			indexable.Timestamp = cc.Dtstart;
			indexable.HitType = "Calendar";

			indexable.AddProperty (Property.NewKeyword ("fixme:source_uid", this.source.Uid));
			indexable.AddProperty (Property.NewKeyword ("fixme:uid", cc.Uid));
			indexable.AddProperty (Property.NewDate ("fixme:starttime", cc.Dtstart.ToUniversalTime ()));
			indexable.AddProperty (Property.NewDate ("fixme:endtime", cc.Dtend.ToUniversalTime ()));

			foreach (string attendee in cc.Attendees)
				indexable.AddProperty (Property.New ("fixme:attendee", attendee));

			foreach (string comment in cc.Comments)
				indexable.AddProperty (Property.New ("fixme:comment", comment));
			
			foreach (string description in cc.Descriptions)
				indexable.AddProperty (Property.New ("fixme:description", description));

			foreach (string summary in cc.Summaries)
				indexable.AddProperty (Property.New ("fixme:summary", summary));

			foreach (string category in cc.Categories)
				indexable.AddProperty (Property.NewKeyword ("fixme:category", category));

			foreach (string location in cc.Location)
				indexable.AddProperty (Property.New ("fixme:location", location));

			return indexable;
		}
	}
}
