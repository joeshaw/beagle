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
using System.Text;
using System.Threading;
using System.IO;

using Beagle.Daemon;
using Beagle.Util;

using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local)]
	public class EvolutionDataServerQueryable : LuceneQueryable {
		private static Logger log = Logger.Get ("addressbook");
		private Scheduler.Priority priority = Scheduler.Priority.Delayed;

		private string this_instance;
		private string photo_dir;
		private bool scheduled_cleanup = false;

		private Evolution.Book addressbook = null;
		private Evolution.BookView book_view;

		public EvolutionDataServerQueryable () : base (Path.Combine (PathFinder.RootDir, "AddressbookIndex"))
		{
			string dir = Path.Combine (PathFinder.RootDir, "AddressbookIndex");
			photo_dir = Path.Combine (dir, "Photos");
			System.IO.Directory.CreateDirectory (photo_dir);

			this_instance = Guid.NewGuid ().ToString ();
		}

		public override void Start ()
		{
			base.Start ();

			try {
				addressbook = Evolution.Book.NewSystemAddressbook ();
				addressbook.Open (true);

				Evolution.BookQuery q = Evolution.BookQuery.AnyFieldContains ("");
				ArrayList dummy = new ArrayList ();
				book_view = addressbook.GetBookView (q, 
								     dummy, 
								     -1);

				book_view.ContactsAdded += OnContactsAdded;
				book_view.ContactsRemoved += OnContactsRemoved;
				book_view.ContactsChanged += OnContactsChanged;
				book_view.SequenceComplete += OnSequenceComplete;
				book_view.Start ();
			} catch (Exception ex) {
				addressbook = null;
				log.Warn ("Could not open Evolution addressbook.  Addressbook searching is disabled.");
				log.Debug (ex);
			}
		}

		private Evolution.Book Addressbook {
			get { return addressbook; }
		}

		private Uri GetContactUri (Evolution.Contact contact) {
			return GetContactUri (contact.Id);
		}

		private Uri GetContactUri (string id) {
			return new Uri ("contact://" + id, true); // FIXME!
		}

		public Indexable ContactToIndexable (Evolution.Contact contact)
		{
			Indexable indexable = new Indexable (GetContactUri (contact));
			indexable.Timestamp = DateTime.Now;
			indexable.Type = "Contact";
						
			indexable.AddProperty (Property.NewKeyword ("beagle:indexinstance", this_instance));
			indexable.AddProperty (Property.NewKeyword ("beagle:dummy", "dummy"));


			indexable.AddProperty (Property.NewKeyword ("fixme:FileAs", contact.FileAs));
			indexable.AddProperty (Property.NewKeyword ("fixme:GivenName", contact.GivenName));
			indexable.AddProperty (Property.NewKeyword ("fixme:FamilyName", contact.FamilyName));
			indexable.AddProperty (Property.NewKeyword ("fixme:Nickname", contact.Nickname));
			indexable.AddProperty (Property.NewKeyword ("fixme:AddressLabelHome", contact.AddressLabelHome));
			indexable.AddProperty (Property.NewKeyword ("fixme:AddressLabelWork", contact.AddressLabelWork));
			indexable.AddProperty (Property.NewKeyword ("fixme:AddressLabelOther", contact.AddressLabelOther));
			indexable.AddProperty (Property.NewKeyword ("fixme:AssistantPhone", contact.AssistantPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:BusinessPhone", contact.BusinessPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:BusinessPhone2", contact.BusinessPhone2));
			indexable.AddProperty (Property.NewKeyword ("fixme:BusinessFax", contact.BusinessFax));
			indexable.AddProperty (Property.NewKeyword ("fixme:CallbackPhone", contact.CallbackPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:CarPhone", contact.CarPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:CompanyPhone", contact.CompanyPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:HomePhone", contact.HomePhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:HomePhone2", contact.HomePhone2));
			indexable.AddProperty (Property.NewKeyword ("fixme:HomeFax", contact.HomeFax));
			indexable.AddProperty (Property.NewKeyword ("fixme:IsdnPhone", contact.IsdnPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:MobilePhone", contact.MobilePhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:OtherPhone", contact.OtherPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:OtherFax", contact.OtherFax));
			indexable.AddProperty (Property.NewKeyword ("fixme:Pager", contact.Pager));
			indexable.AddProperty (Property.NewKeyword ("fixme:PrimaryPhone", contact.PrimaryPhone));
			indexable.AddProperty (Property.NewKeyword ("fixme:Radio", contact.Radio));
			indexable.AddProperty (Property.NewKeyword ("fixme:Telex", contact.Telex));
			indexable.AddProperty (Property.NewKeyword ("fixme:Tty", contact.Tty));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email1", contact.Email1));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email2", contact.Email2));
			indexable.AddProperty (Property.NewKeyword ("fixme:Email3", contact.Email3));
			indexable.AddProperty (Property.NewKeyword ("fixme:Mailer", contact.Mailer));
			indexable.AddProperty (Property.NewKeyword ("fixme:Org", contact.Org));
			indexable.AddProperty (Property.NewKeyword ("fixme:OrgUnit", contact.OrgUnit));
			indexable.AddProperty (Property.NewKeyword ("fixme:Office", contact.Office));
			indexable.AddProperty (Property.NewKeyword ("fixme:Title", contact.Title));
			indexable.AddProperty (Property.NewKeyword ("fixme:Role", contact.Role));
			indexable.AddProperty (Property.NewKeyword ("fixme:Manager", contact.Manager));
			indexable.AddProperty (Property.NewKeyword ("fixme:Assistant", contact.Assistant));
			indexable.AddProperty (Property.NewKeyword ("fixme:HomepageUrl", contact.HomepageUrl));
			indexable.AddProperty (Property.NewKeyword ("fixme:BlogUrl", contact.BlogUrl));
			indexable.AddProperty (Property.NewKeyword ("fixme:Categories", contact.Categories));
			indexable.AddProperty (Property.NewKeyword ("fixme:Caluri", contact.Caluri));
			indexable.AddProperty (Property.NewKeyword ("fixme:Icscalendar", contact.Icscalendar));
			indexable.AddProperty (Property.NewKeyword ("fixme:Spouse", contact.Spouse));
			indexable.AddProperty (Property.NewKeyword ("fixme:Note", contact.Note));
			
			Evolution.ContactPhoto photo = contact.Photo;

			if (photo.Data != null && photo.Data.Length > 0) {
				string photo_filename = GetPhotoFilename (contact.Id);
				Stream s = File.OpenWrite (photo_filename);
				BinaryWriter w = new BinaryWriter (s);
				w.Write (photo.Data);
				w.Close ();

				indexable.AddProperty (Property.NewKeyword ("Photo", photo_filename));
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
		
			String name = "";
			if (contact.GivenName != null && contact.GivenName != "")
				name = contact.GivenName;
			if (contact.FamilyName != null && contact.FamilyName != "")
				name += " " + contact.FamilyName;
			if (name.Length > 0)
				indexable.AddProperty (Property.NewKeyword ("fixme:Name", name));
		
			if (contact.Email1 != null)
				indexable.AddProperty (Property.NewKeyword ("fixme:Email",
									    contact.Email1));
			return indexable;
		}

		private void AddContacts (IEnumerable contacts)
		{
			foreach (Evolution.Contact contact in contacts) {
				Scheduler.Task task;
				task = NewAddTask (ContactToIndexable (contact));
				task.Priority = priority;
				ThisScheduler.Add (task);
			}
		}
		
		private void RemoveContacts (IEnumerable contacts)
		{
			foreach (string id in contacts) {
				Scheduler.Task task;
				task = NewRemoveTask (GetContactUri (id));
				task.Priority = priority;
				ThisScheduler.Add (task);
				string filename = GetPhotoFilename (id);
				if (filename != null && File.Exists (filename)) 
					File.Delete (filename);
			}
		}

		private void OnContactsAdded (object o,
					      Evolution.ContactsAddedArgs args)
		{
			AddContacts (args.Contacts);
		}

		private void OnContactsChanged (object o,
						Evolution.ContactsChangedArgs args)
		{
			AddContacts (args.Contacts);
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

			
			RemoveContacts (id_list);
		}
		
		private string GetPhotoFilename (string id)
		{
			return Path.Combine (photo_dir, id);
		}
		
		private void CleanupHits (LNS.Hits lucene_hits)
		{
			int n_hits = lucene_hits.Length ();

			for (int i = 0; i < n_hits; i++) {
				Document doc = lucene_hits.Doc (i);
				Scheduler.Task task;
				task = NewRemoveTask (new Uri (doc.Get ("Uri")));
				task.Priority = priority;
				ThisScheduler.Add (task);
				string filename = doc.Get ("Photo");
				if (filename != null && File.Exists (filename))
					File.Delete (filename);
			}

			log.Debug ("Cleaned up {0} entries", n_hits);

			// Now that we're done synching with the original state of the addressbook, switch all new changes to Immediate mode
			priority = Scheduler.Priority.Immediate;
		}

		private void Cleanup (Scheduler.Task task)
		{
			Driver.Flush ();

			LNS.BooleanQuery query = new LNS.BooleanQuery ();
			Term term = new Term ("prop:beagle:dummy",  "dummy");
			LNS.Query term_query = new LNS.TermQuery (term);
			query.Add (term_query, true, false);
			
			term = new Term ("prop:beagle:indexinstance",
					 this_instance);
			term_query = new LNS.TermQuery (term);
			query.Add (term_query, false, true);

			LNS.IndexSearcher searcher = new LNS.IndexSearcher (Driver.Store);
			LNS.Hits lucene_hits = searcher.Search (query);
			CleanupHits (lucene_hits);
		}

		private void OnSequenceComplete (object o,
						 Evolution.SequenceCompleteArgs args)
		{
			if (!scheduled_cleanup) {
				scheduled_cleanup = true;
				Scheduler.Task task = NewTaskFromHook (new Scheduler.TaskHook (Cleanup));
				task.Weight = 2;
				task.Tag = "Cleanup " + this_instance;
				task.Priority = priority;
				ThisScheduler.Add (task);
			}
		}		
	}

}
