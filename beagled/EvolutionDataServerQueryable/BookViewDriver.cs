//
// BookViewDriver.cs
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
using Beagle.Daemon;
using System.Threading;

using System.Runtime.InteropServices;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	internal class BookViewDriver {
		private Evolution.BookView view;
		private IQueryResult result;
		
		private Uri GetContactUri (Evolution.Contact contact) {
			return GetContactUri (contact.Id);
		}

		private Uri GetContactUri (string id) {
			return new Uri ("contact://" + id, true); // FIXME!
		}

		public Hit HitFromContact (Evolution.Contact contact)
		{
			Hit hit = new Hit ();
			
			hit.Uri    = GetContactUri (contact.Id);
			hit.Type   = "Contact";
			hit.Source = "EvolutionDataServer";
			hit.ScoreRaw  = 1.0f; // FIXME
			
			hit ["fixme:FileAs"] = contact.FileAs;
			hit ["fixme:GivenName"] = contact.GivenName;
			hit ["fixme:FamilyName"] = contact.FamilyName;
			hit ["fixme:Nickname"] = contact.Nickname;
			hit ["fixme:AddressLabelHome"] = contact.AddressLabelHome;
			hit ["fixme:AddressLabelWork"] = contact.AddressLabelWork;
			hit ["fixme:AddressLabelOther"] = contact.AddressLabelOther;
			hit ["fixme:AssistantPhone"] = contact.AssistantPhone;
			hit ["fixme:BusinessPhone"] = contact.BusinessPhone;
			hit ["fixme:BusinessPhone2"] = contact.BusinessPhone2;
			hit ["fixme:BusinessFax"] = contact.BusinessFax;
			hit ["fixme:CallbackPhone"] = contact.CallbackPhone;
			hit ["fixme:CarPhone"] = contact.CarPhone;
			hit ["fixme:CompanyPhone"] = contact.CompanyPhone;
			hit ["fixme:HomePhone"] = contact.HomePhone;
			hit ["fixme:HomePhone2"] = contact.HomePhone2;
			hit ["fixme:HomeFax"] = contact.HomeFax;
			hit ["fixme:IsdnPhone"] = contact.IsdnPhone;
			hit ["fixme:MobilePhone"] = contact.MobilePhone;
			hit ["fixme:OtherPhone"] = contact.OtherPhone;
			hit ["fixme:OtherFax"] = contact.OtherFax;
			hit ["fixme:Pager"] = contact.Pager;
			hit ["fixme:PrimaryPhone"] = contact.PrimaryPhone;
			hit ["fixme:Radio"] = contact.Radio;
			hit ["fixme:Telex"] = contact.Telex;
			hit ["fixme:Tty"] = contact.Tty;
			hit ["fixme:Email1"] = contact.Email1;
			hit ["fixme:Email2"] = contact.Email2;
			hit ["fixme:Email3"] = contact.Email3;
			hit ["fixme:Mailer"] = contact.Mailer;
			hit ["fixme:Org"] = contact.Org;
			hit ["fixme:OrgUnit"] = contact.OrgUnit;
			hit ["fixme:Office"] = contact.Office;
			hit ["fixme:Title"] = contact.Title;
			hit ["fixme:Role"] = contact.Role;
			hit ["fixme:Manager"] = contact.Manager;
			hit ["fixme:Assistant"] = contact.Assistant;
			hit ["fixme:HomepageUrl"] = contact.HomepageUrl;
			hit ["fixme:BlogUrl"] = contact.BlogUrl;
			hit ["fixme:Categories"] = contact.Categories;
			hit ["fixme:Caluri"] = contact.Caluri;
			hit ["fixme:Icscalendar"] = contact.Icscalendar;
			hit ["fixme:Spouse"] = contact.Spouse;
			hit ["fixme:Note"] = contact.Note;
		
			if (contact.Photo.Data != null && contact.Photo.Data.Length > 0)
				hit.SetData ("Photo", contact.Photo.Data);
		
			// FIXME: List?
			// FIXME: ListShowAddresses?
		
			// FIXME: Should we not drop the extra Im addresses?
			if (contact.ImAim.Length > 0)
				hit ["fixme:ImAim"] = contact.ImAim [0];
			if (contact.ImIcq.Length > 0)
				hit ["fixme:ImIcq"] = contact.ImIcq [0];
			if (contact.ImJabber.Length > 0)
				hit ["fixme:ImJabber"] = contact.ImJabber [0];
			if (contact.ImMsn.Length > 0)
				hit ["fixme:ImMsn"] = contact.ImMsn [0];
			if (contact.ImYahoo.Length > 0)
				hit ["fixme:ImYahoo"] = contact.ImYahoo [0];
		
			String name = "";
			if (contact.GivenName != null && contact.GivenName != "")
				name = contact.GivenName;
			if (contact.FamilyName != null && contact.FamilyName != "")
				name += " " + contact.FamilyName;
			if (name.Length > 0)
				hit ["fixme:Name"] = name;
		
			if (hit ["fixme:Email1"] != null)
				hit ["fixme:Email"] = hit ["fixme:Email1"];
		
			return hit;
		}
	
		void OnContactsAdded (object o,
				      Evolution.ContactsAddedArgs args)
		{
			ArrayList array = new ArrayList ();
		
			foreach (Evolution.Contact contact in args.Contacts) {
				Hit hit = HitFromContact (contact);
				array.Add (hit);
			}
		
			result.Add (array);
		}
	
		void OnContactsRemoved (object o,
					Evolution.ContactsRemovedArgs args)
		{
			// FIXME: This is a temporary workaround for the 
			// fact that the evolution bindings return a 
			// GLib.List with an object type, but there
			// are really strings in there
			GLib.List idList = new GLib.List (args.Ids.Handle,
							  typeof (string));

			ArrayList array = new ArrayList ();

			foreach (string id in idList) {
				array.Add (GetContactUri (id));
			}

			result.Subtract (array);
		}
	
		void OnContactsChanged (object o,
					Evolution.ContactsChangedArgs args)
		{
			// FIXME: handle this as a remove/add?  Add a
			// "changed" event to beagle?
		}

		void OnSequenceComplete (object o, 
					 Evolution.SequenceCompleteArgs args)
		{
			lock (this) {
				Monitor.Pulse (this);
			}
		}

		private void DisconnectView () 
		{
			if (view != null) {
				view.ContactsAdded -= OnContactsAdded;
				view.ContactsRemoved -= OnContactsRemoved;
				view.ContactsChanged -= OnContactsChanged;
				view.SequenceComplete -= OnSequenceComplete;
				
				view.Dispose ();
				
				view = null;
			}
		}
	
		private void OnResultCancelled (QueryResult source) 
		{
			lock (this) {
				DisconnectView ();
				result = null;
				Monitor.Pulse (this);
			}
		}

		private void OnShutdown () 
		{
			lock (this) {
				DisconnectView ();
				result = null;
				Monitor.Pulse (this);
			}
		}

		public BookViewDriver (Evolution.Book addressbook,
				       QueryBody body,
				       IQueryResult _result)
		{
			// FIXME: Evolution.BookQuery's bindings are all
			// screwed up, so we can't construct compound queries.
			// This will have to do for now.
			Evolution.BookQuery[] ebqs = new Evolution.BookQuery [body.Text.Count];
			for (int i = 0; i < body.Text.Count; ++i) {
				string text = (string) body.Text [i];
				ebqs [i] = Evolution.BookQuery.AnyFieldContains (text);
			}
		
			Evolution.BookQuery bq;
			bq = Evolution.BookQuery.And (ebqs, false);

			ArrayList dummy = new ArrayList ();
			view = addressbook.GetBookView (bq, dummy, -1);

			result = _result;
		
			view.ContactsAdded += OnContactsAdded;
			view.ContactsRemoved += OnContactsRemoved;
			view.ContactsChanged += OnContactsChanged;
			view.SequenceComplete += OnSequenceComplete;

			// FIXME: bad hack - need Cancelled on IQueryResult
			QueryResult queryResult = (QueryResult)result;
			queryResult.CancelledEvent += OnResultCancelled;

			Shutdown.ShutdownEvent += OnShutdown;
		}
	
		public void Start () 
		{
			view.Start ();
		}
	}
}
