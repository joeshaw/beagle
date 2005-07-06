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

using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Documents;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local, RequireInotify=false)]
	public class EvolutionDataServerQueryable : LuceneQueryable {
		private static Logger log = Logger.Get ("addressbook");
		//private Scheduler.Priority priority = Scheduler.Priority.Immediate;
		private Scheduler.Priority priority = Scheduler.Priority.Delayed;

		private string photo_dir;
		private DateTime sequence_start_time;
		
		private DateTime indexed_through = DateTime.MinValue;

		private Evolution.Book addressbook = null;
		private Evolution.BookView book_view;

		public EvolutionDataServerQueryable () : base ("AddressbookIndex")
		{
			string dir = Path.Combine (PathFinder.StorageDir, "AddressbookIndex");
			photo_dir = Path.Combine (dir, "Photos");
			System.IO.Directory.CreateDirectory (photo_dir);
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
				
				sequence_start_time = DateTime.Now;
				book_view.Start ();

			} catch (Exception ex) {
				addressbook = null;
				log.Warn ("Could not open Evolution addressbook.  Addressbook searching is disabled.");
				log.Debug (ex);
			}
		}

		private DateTime IndexedThrough {
			
			get {
				if (indexed_through == DateTime.MinValue) {
					string filename = Path.Combine (IndexStoreDirectory, "IndexedThrough");
					
					string line = null;
					try {
						StreamReader sr = new StreamReader (filename);
						line = sr.ReadLine ();
						sr.Close ();
					} catch (Exception ex) { }

					if (line != null)
						indexed_through = StringFu.StringToDateTime (line);
				}
				return indexed_through;
			}
			
			set {
				indexed_through = value;

				string filename = Path.Combine (IndexStoreDirectory, "IndexedThrough");
				StreamWriter sw = new StreamWriter (filename);
				sw.WriteLine (StringFu.DateTimeToString (indexed_through));
				sw.Close ();
			}
		}
		
		private Uri GetContactUri (Evolution.Contact contact) {
			return GetContactUri (contact.Id);
		}

		private Uri GetContactUri (string id) {
			return new Uri ("contact://" + id, true); // FIXME!
		}

		private static string ExtractFieldFromVCardString (string vcard_str, string field)
		{
			field = "\n" + field + ":";

			int i = vcard_str.IndexOf (field);
			if (i == -1)
				return null;
			i += field.Length;
			
			int j = vcard_str.IndexOf ('\n', i);
			
			string retval = null;
			if (j == -1)
				retval = vcard_str.Substring (i);
			else
				retval = vcard_str.Substring (i, j-i);

			if (retval != null) {
				retval = retval.Trim ();
				if (retval.Length == 0)
					retval = null;
			}

			return retval;
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
			string vcard_str = contact.ToString (Evolution.VCardFormat.Three0);

			string rev_str = ExtractFieldFromVCardString (vcard_str, "REV");
			DateTime rev = RevStringToDateTime (rev_str);

			if (rev != DateTime.MinValue && rev < IndexedThrough)
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

				indexable.AddProperty (Property.NewUnsearched ("Photo", photo_filename));
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

		private void AddContacts (IEnumerable contacts)
		{
			foreach (Evolution.Contact contact in contacts) {
				Indexable indexable = ContactToIndexable (contact);
				if (indexable != null) {
					Scheduler.Task task;
					task = NewAddTask (indexable);
					task.Priority = priority;
					ThisScheduler.Add (task);
				}
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
		
		private void OnSequenceComplete (object o,
						 Evolution.SequenceCompleteArgs args)
		{
			// Contacts that get changed while the beagled is
			// running will be re-indexed during the next scan.
			// That isn't optimal, but is much better than the
			// current situation.
			IndexedThrough = sequence_start_time;

			// Now that we're done synching with the original
			// state of the addressbook, switch all new changes to
			// Immediate mode
			priority = Scheduler.Priority.Immediate;
		}

	}

}
