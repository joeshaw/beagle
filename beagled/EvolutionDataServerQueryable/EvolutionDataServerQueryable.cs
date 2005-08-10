//
// EvolutionDataServerQueryable.cs
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
using System.Globalization;
using System.Text;
using System.Threading;
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

using Evolution;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local, RequireInotify=false)]
	public class EvolutionDataServerQueryable : LuceneQueryable {
		//private Scheduler.Priority priority = Scheduler.Priority.Immediate;
		private Scheduler.Priority priority = Scheduler.Priority.Delayed;

		private string photo_dir;
		private DateTime sequence_start_time;
		
		private DateTime addressbook_indexed_through = DateTime.MinValue;
		private DateTime calendar_indexed_through = DateTime.MinValue;

		public EvolutionDataServerQueryable () : base ("EvolutionDataServerIndex")
		{
			photo_dir = Path.Combine (Driver.TopDirectory, "Photos");
			System.IO.Directory.CreateDirectory (photo_dir);
		}

		public override void Start ()
		{
			base.Start ();

			Logger.Log.Info ("Scanning addressbooks and calendars");
			Stopwatch timer = new Stopwatch ();
			timer.Start ();

			EdsSource src;

			src = new EdsSource ("/apps/evolution/addressbook/sources");
			src.IndexSourceAll += AddressbookIndexSourceAll;
			src.IndexSourceChanges += AddressbookIndexSourceChanges;
			src.RemoveSource += AddressbookRemoveSource;
			src.Index ();

			src = new EdsSource ("/apps/evolution/calendar/sources");
			src.IndexSourceAll += CalendarIndexSourceAll;
			src.IndexSourceChanges += CalendarIndexSourceChanges;
			src.RemoveSource += CalendarRemoveSource;
			src.Index ();

			timer.Stop ();
			Logger.Log.Info ("Scanned addressbooks and calendars in {0}", timer);
		}

		public void Add (Indexable indexable, Scheduler.Priority priority)
		{
			Scheduler.Task task;
			task = NewAddTask (indexable);
			task.Priority = priority;
			ThisScheduler.Add (task);
		}

		public void Remove (Uri uri)
		{
			Scheduler.Task task;
			task = NewRemoveTask (uri);
			task.Priority = Scheduler.Priority.Immediate;
			ThisScheduler.Add (task);
		}

		///////////////////////////////////////

		private void AddressbookIndexSourceAll (Evolution.Source src)
		{
			if (!src.IsLocal ()) {
				Logger.Log.Debug ("Skipping remote addressbook {0}", src.Uri);
				return;
			}

			Logger.Log.Debug ("Indexing all data in this addressbook ({0})!", src.Uri);

			Book book = new Book (src);
			book.Open (true);

			BookView book_view = book.GetBookView (BookQuery.AnyFieldContains (""),
							       new object [0],
							       -1);

			book_view.ContactsAdded += OnContactsAdded;
			book_view.ContactsRemoved += OnContactsRemoved;
			book_view.ContactsChanged += OnContactsChanged;
			book_view.SequenceComplete += OnSequenceComplete;

			book_view.Start ();
		}

		private void AddressbookIndexSourceChanges (Evolution.Source src)
		{
			if (!src.IsLocal ()) {
				Logger.Log.Debug ("Skipping remote addressbook {0}", src.Uri);
				return;
			}

			Book book = new Book (src);
			book.Open (true);

			Contact[] added, changed;
			string[] removed;

			Logger.Log.Debug ("Getting addressbook changes for {0}", src.Uri);
			book.GetChanges ("beagle-" + Driver.Fingerprint, out added, out changed, out removed);
			Logger.Log.Debug ("Addressbook {0}: {1} added, {2} changed, {3} removed",
					  book.Uri, added.Length, changed.Length, removed.Length);

			foreach (Contact contact in added)
				AddContact (contact);

			foreach (Contact contact in changed)
				AddContact (contact);

			foreach (string id in removed)
				RemoveContact (id);

			BookView book_view = book.GetBookView (BookQuery.AnyFieldContains (""),
							       new object [0],
							       -1);

			book_view.ContactsAdded += OnContactsAdded;
			book_view.ContactsRemoved += OnContactsRemoved;
			book_view.ContactsChanged += OnContactsChanged;
			book_view.SequenceComplete += OnSequenceComplete;

			book_view.Start ();
		}

		private void AddressbookRemoveSource (Evolution.Source src)
		{
			// FIXME: We need to index the group's UID and then
			// we need a way to schedule removal tasks for
			// anything that matches that lucene property
			Logger.Log.Debug ("FIXME: Remove addressbook source {0}", src.Uri);
		}

		private static Uri GetContactUri (Evolution.Contact contact) {
			return GetContactUri (contact.Id);
		}

		private static Uri GetContactUri (string id) {
			return new Uri ("contact://" + id, true); // FIXME!
		}

		private DateTime AddressbookIndexedThrough {
			
			get {
				if (addressbook_indexed_through == DateTime.MinValue) {
					string filename = Path.Combine (IndexDirectory, "AddressbookIndexedThrough");
					
					string line = null;
					try {
						StreamReader sr = new StreamReader (filename);
						line = sr.ReadLine ();
						sr.Close ();
					} catch (Exception ex) { }

					if (line != null)
						addressbook_indexed_through = StringFu.StringToDateTime (line);
				}
				return addressbook_indexed_through;
			}
			
			set {
				addressbook_indexed_through = value;

				string filename = Path.Combine (IndexDirectory, "AddressbookIndexedThrough");
				StreamWriter sw = new StreamWriter (filename);
				sw.WriteLine (StringFu.DateTimeToString (addressbook_indexed_through));
				sw.Close ();
			}
		}

		private static DateTime RevStringToDateTime (string date_str)
		{
			if (date_str == null)
				return DateTime.MinValue;

			string[] formats = {
				"yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
				"yyyyMMdd'T'HHmmss'Z'"
			};

			try {
				return DateTime.ParseExact (date_str, formats,
							    CultureInfo.InvariantCulture,
							    DateTimeStyles.None);
			} catch (FormatException) {
				Logger.Log.Warn ("Unable to parse last revision string: {0}", date_str);
				return DateTime.MinValue;
			}
		}

		private Indexable ContactToIndexable (Evolution.Contact contact)
		{
			DateTime rev = RevStringToDateTime (contact.Rev);

			if (rev != DateTime.MinValue && rev < AddressbookIndexedThrough)
				return null;

			Indexable indexable = new Indexable (GetContactUri (contact));
			indexable.Timestamp = rev;
			indexable.Type = "Contact";
						
			indexable.AddProperty (Property.New ("fixme:FileAs", contact.FileAs));
			indexable.AddProperty (Property.New ("fixme:GivenName", contact.GivenName));
			indexable.AddProperty (Property.New ("fixme:FamilyName", contact.FamilyName));
			indexable.AddProperty (Property.New ("fixme:Nickname", contact.Nickname));
			indexable.AddProperty (Property.New ("fixme:AddressLabelHome", contact.AddressLabelHome));
			indexable.AddProperty (Property.New ("fixme:AddressLabelWork", contact.AddressLabelWork));
			indexable.AddProperty (Property.New ("fixme:AddressLabelOther", contact.AddressLabelOther));
			indexable.AddProperty (Property.New ("fixme:AssistantPhone", contact.AssistantPhone));
			indexable.AddProperty (Property.New ("fixme:BusinessPhone", contact.BusinessPhone));
			indexable.AddProperty (Property.New ("fixme:BusinessPhone2", contact.BusinessPhone2));
			indexable.AddProperty (Property.New ("fixme:BusinessFax", contact.BusinessFax));
			indexable.AddProperty (Property.New ("fixme:CallbackPhone", contact.CallbackPhone));
			indexable.AddProperty (Property.New ("fixme:CarPhone", contact.CarPhone));
			indexable.AddProperty (Property.New ("fixme:CompanyPhone", contact.CompanyPhone));
			indexable.AddProperty (Property.New ("fixme:HomePhone", contact.HomePhone));
			indexable.AddProperty (Property.New ("fixme:HomePhone2", contact.HomePhone2));
			indexable.AddProperty (Property.New ("fixme:HomeFax", contact.HomeFax));
			indexable.AddProperty (Property.New ("fixme:IsdnPhone", contact.IsdnPhone));
			indexable.AddProperty (Property.New ("fixme:MobilePhone", contact.MobilePhone));
			indexable.AddProperty (Property.New ("fixme:OtherPhone", contact.OtherPhone));
			indexable.AddProperty (Property.New ("fixme:OtherFax", contact.OtherFax));
			indexable.AddProperty (Property.New ("fixme:Pager", contact.Pager));
			indexable.AddProperty (Property.New ("fixme:PrimaryPhone", contact.PrimaryPhone));
			indexable.AddProperty (Property.New ("fixme:Radio", contact.Radio));
			indexable.AddProperty (Property.New ("fixme:Telex", contact.Telex));
			indexable.AddProperty (Property.NewKeyword ("fixme:Tty", contact.Tty));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email1", contact.Email1));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email2", contact.Email2));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email3", contact.Email3));
			indexable.AddProperty (Property.NewKeyword ("fixme:Mailer", contact.Mailer));
			indexable.AddProperty (Property.New ("fixme:Org", contact.Org));
			indexable.AddProperty (Property.New ("fixme:OrgUnit", contact.OrgUnit));
			indexable.AddProperty (Property.New ("fixme:Office", contact.Office));
			indexable.AddProperty (Property.New ("fixme:Title", contact.Title));
			indexable.AddProperty (Property.New ("fixme:Role", contact.Role));
			indexable.AddProperty (Property.New ("fixme:Manager", contact.Manager));
			indexable.AddProperty (Property.New ("fixme:Assistant", contact.Assistant));
			indexable.AddProperty (Property.NewKeyword ("fixme:HomepageUrl", contact.HomepageUrl));
			indexable.AddProperty (Property.NewKeyword ("fixme:BlogUrl", contact.BlogUrl));
			indexable.AddProperty (Property.NewKeyword ("fixme:Categories", contact.Categories));
			indexable.AddProperty (Property.NewKeyword ("fixme:Caluri", contact.Caluri));
			indexable.AddProperty (Property.NewKeyword ("fixme:Icscalendar", contact.Icscalendar));
			indexable.AddProperty (Property.New ("fixme:Spouse", contact.Spouse));
			indexable.AddProperty (Property.New ("fixme:Note", contact.Note));
			
			Evolution.ContactPhoto photo = contact.Photo;

			if (photo.Data != null && photo.Data.Length > 0) {
				string photo_filename = GetPhotoFilename (contact.Id);
				Stream s = new FileStream (photo_filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
				BinaryWriter w = new BinaryWriter (s);
				w.Write (photo.Data);
				w.Close ();
				s.Close ();

				indexable.AddProperty (Property.NewUnsearched ("beagle:Photo", photo_filename));
			}
			// FIXME: List?
			// FIXME: ListShowAddresses?
		
			// FIXME: Should we not drop the extra Im addresses?
			if (contact.ImAim.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImAim", contact.ImAim [0]));
			if (contact.ImIcq.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImIcq", contact.ImIcq [0]));
			if (contact.ImJabber.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImJabber", contact.ImJabber [0]));
			if (contact.ImMsn.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImMsn", contact.ImMsn [0]));
			if (contact.ImYahoo.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImYahoo", contact.ImYahoo [0]));
			if (contact.ImGroupwise.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:ImGroupWise", contact.ImGroupwise [0]));
		
			String name = "";
			if (contact.GivenName != null && contact.GivenName != "")
				name = contact.GivenName;
			if (contact.FamilyName != null && contact.FamilyName != "")
				name += " " + contact.FamilyName;
			if (name.Length > 0)
				indexable.AddProperty (Property.New ("fixme:Name", name));
		
			if (contact.Email1 != null)
				indexable.AddProperty (Property.NewKeyword ("fixme:Email",
									    contact.Email1));
			return indexable;
		}

		private void AddContact (Contact contact)
		{
			Indexable indexable = ContactToIndexable (contact);
			if (indexable != null)
				Add (indexable, priority);
		}

		private void RemoveContact (string id)
		{
			Remove (GetContactUri (id));
				
			string filename = GetPhotoFilename (id);
			if (filename != null && File.Exists (filename)) 
				File.Delete (filename);
		}

		private void OnContactsAdded (object o,
					      Evolution.ContactsAddedArgs args)
		{
			foreach (Evolution.Contact contact in args.Contacts)
				AddContact (contact);
		}

		private void OnContactsChanged (object o,
						Evolution.ContactsChangedArgs args)
		{
			foreach (Evolution.Contact contact in args.Contacts)
				AddContact (contact);
		}
		
		private void OnContactsRemoved (object o,
						Evolution.ContactsRemovedArgs args)
		{
			// FIXME: This is a temporary workaround for the 
			// fact that the evolution bindings return a 
			// GLib.List with an object type, but there
			// are really strings in there

			GLib.List id_list = new GLib.List (args.Ids.Handle,
							   typeof (string));


			foreach (string id in id_list)
				RemoveContact (id);
		}
		
		private string GetPhotoFilename (string id)
		{
			return Path.Combine (photo_dir, id);
		}
		
		private void OnSequenceComplete (object o,
						 Evolution.SequenceCompleteArgs args)
		{
			// Contacts that get changed while the beagled is
			// running will be re-indexed during the next scan.
			// That isn't optimal, but is much better than the
			// current situation.
			AddressbookIndexedThrough = sequence_start_time;

			// Now that we're done synching with the original
			// state of the addressbook, switch all new changes to
			// Immediate mode
			priority = Scheduler.Priority.Immediate;
		}

		///////////////////////////////////////

		private void CalendarIndexSourceAll (Evolution.Source src)
		{
			if (!src.IsLocal ()) {
				Logger.Log.Debug ("Skipping remote calendar {0}", src.Uri);
				return;
			}

			Logger.Log.Debug ("Indexing all data in this calendar ({0})!", src.Uri);

			Cal cal = new Cal (src, CalSourceType.Event);
			cal.Open (true);

			CalComponent[] event_list = cal.GetItems ("#t");

			Logger.Log.Debug ("Calendar has {0} items", event_list.Length);

			foreach (CalComponent cc in event_list)
				IndexCalComponent (cc, Scheduler.Priority.Immediate);

			CalView cal_view = cal.GetCalView ("#t");
			cal_view.ObjectsAdded += OnObjectsAdded;
			cal_view.ObjectsModified += OnObjectsModified;
			cal_view.ObjectsRemoved += OnObjectsRemoved;
			cal_view.Start ();
		}

		private void CalendarIndexSourceChanges (Evolution.Source src)
		{
			if (!src.IsLocal ()) {
				Logger.Log.Debug ("Skipping remote calendar {0}", src.Uri);
				return;
			}

			Cal cal = new Cal (src, CalSourceType.Event);
			cal.Open (true);

			CalComponent[] new_items, update_items;
			string[] remove_items;

			Logger.Log.Debug ("Getting calendar changes for {0}", src.Uri);
			cal.GetChanges ("beagle-" + this.Driver.Fingerprint, out new_items, out update_items, out remove_items);
			Logger.Log.Debug ("Calendar {0}: {1} new items, {2} updated items, {3} removed items",
					  cal.Uri, new_items.Length, update_items.Length, remove_items.Length);
			
			foreach (CalComponent cc in new_items)
				IndexCalComponent (cc, Scheduler.Priority.Immediate);

			foreach (CalComponent cc in update_items)
				IndexCalComponent (cc, Scheduler.Priority.Immediate);

			foreach (string id in remove_items) {
				// FIXME: Broken in evo-sharp right now
				//RemoveCalComponent (id);
			}

			CalView cal_view = cal.GetCalView ("#t");
			cal_view.ObjectsAdded += OnObjectsAdded;
			cal_view.ObjectsModified += OnObjectsModified;
			cal_view.ObjectsRemoved += OnObjectsRemoved;
			cal_view.Start ();
		}

		private void CalendarRemoveSource (Evolution.Source src)
		{
			// FIXME: We need to index the group's UID and then
			// we need a way to schedule removal tasks for
			// anything that matches that lucene property
			Logger.Log.Debug ("FIXME: Remove calendar source {0}", src.Uri);
		}

		private static Uri GetCalendarUri (CalComponent cc) {
			return GetContactUri (cc.Uid);
		}

		private static Uri GetCalendarUri (string id) {
			return new Uri ("calendar://" + id, true); // FIXME!
		}

		private DateTime CalendarIndexedThrough {
			
			get {
				if (calendar_indexed_through == DateTime.MinValue) {
					string filename = Path.Combine (IndexDirectory, "CalendarIndexedThrough");
					
					string line = null;
					try {
						StreamReader sr = new StreamReader (filename);
						line = sr.ReadLine ();
						sr.Close ();
					} catch (Exception ex) { }

					if (line != null)
						calendar_indexed_through = StringFu.StringToDateTime (line);
				}
				return calendar_indexed_through;
			}
			
			set {
				calendar_indexed_through = value;

				string filename = Path.Combine (IndexDirectory, "CalendarIndexedThrough");
				StreamWriter sw = new StreamWriter (filename);
				sw.WriteLine (StringFu.DateTimeToString (calendar_indexed_through));
				sw.Close ();
			}
		}

		private void IndexCalComponent (CalComponent cc, Scheduler.Priority priority)
		{
			Indexable indexable = CalComponentToIndexable (cc);
			Add (indexable, priority);
		}

		private void RemoveCalComponent (string id)
		{
			Remove (GetCalendarUri (id));
		}

		private Indexable CalComponentToIndexable (CalComponent cc)
		{
			Indexable indexable = new Indexable (new Uri ("calendar:///" + cc.Uid));

			indexable.Timestamp = cc.Dtstart;
			indexable.Type = "Calendar";

			indexable.AddProperty (Property.NewKeyword ("fixme:uid", cc.Uid));
			indexable.AddProperty (Property.NewDate ("fixme:starttime", cc.Dtstart));
			indexable.AddProperty (Property.NewDate ("fixme:endtime", cc.Dtend));

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

		private void OnObjectsAdded (object o, ObjectsAddedArgs args)
		{
			CalView cal_view = (CalView) o;

			foreach (CalComponent cc in CalUtil.CalCompFromICal (args.Objects.Handle, cal_view.Client)) {
				// If the minimum date is unset/invalid,
				// index it.
				if (cc.LastModified <= CalUtil.MinDate || cc.LastModified > CalendarIndexedThrough)
					IndexCalComponent (cc, Scheduler.Priority.Immediate);
			}
		}

		private void OnObjectsModified (object o, ObjectsModifiedArgs args)
		{
			CalView cal_view = (CalView) o;
			
			foreach (CalComponent cc in CalUtil.CalCompFromICal (args.Objects.Handle, cal_view.Client))
				IndexCalComponent (cc, Scheduler.Priority.Immediate);
		}

		private void OnObjectsRemoved (object o, ObjectsRemovedArgs args)
		{
			// FIXME: This is a temporary workaround for the 
			// fact that the evolution bindings return a 
			// GLib.List with an object type, but there
			// are really strings in there

			GLib.List id_list = new GLib.List (args.Uids.Handle,
							   typeof (string));

			foreach (string uid in id_list) {
				Scheduler.Task task;
				task = NewRemoveTask (new Uri ("calendar:///" + uid));
				task.Priority = Scheduler.Priority.Immediate;
				ThisScheduler.Add (task);
			}
		}
	}
}
