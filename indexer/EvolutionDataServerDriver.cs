//
// EvolutionAddressbookDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Collections;
using System.Text;

namespace Beagle {

	[QueryableFlavor (Name="EvolutionDataServer", Domain=QueryDomain.Local)]
	public class EvolutionDataServerDriver : IQueryable{

		Evolution.Book addressbook = null;

		private Evolution.Book Addressbook {
			get {
				if (addressbook == null) {
					addressbook = Evolution.Book.NewSystemAddressbook ();
					addressbook.Open (true);
				}
				return addressbook;
			}
		}

		public Hit HitFromContact (Evolution.Contact contact)
		{
			Hit hit = new Hit ();

			hit.Uri    = "contact://" + contact.Id; // FIXME!
			hit.Type   = "Contact";
			hit.Source = "EvolutionDataServer";
			hit.ScoreRaw  = 1.0f; // FIXME

			hit ["FileAs"] = contact.FileAs;
			hit ["GivenName"] = contact.GivenName;
			hit ["FamilyName"] = contact.FamilyName;
			hit ["Nickname"] = contact.Nickname;
			hit ["AddressLabelHome"] = contact.AddressLabelHome;
			hit ["AddressLabelWork"] = contact.AddressLabelWork;
			hit ["AddressLabelOther"] = contact.AddressLabelOther;
			hit ["AssistantPhone"] = contact.AssistantPhone;
			hit ["BusinessPhone"] = contact.BusinessPhone;
			hit ["BusinessPhone2"] = contact.BusinessPhone2;
			hit ["BusinessFax"] = contact.BusinessFax;
			hit ["CallbackPhone"] = contact.CallbackPhone;
			hit ["CarPhone"] = contact.CarPhone;
			hit ["CompanyPhone"] = contact.CompanyPhone;
			hit ["HomePhone"] = contact.HomePhone;
			hit ["HomePhone2"] = contact.HomePhone2;
			hit ["HomeFax"] = contact.HomeFax;
			hit ["IsdnPhone"] = contact.IsdnPhone;
			hit ["MobilePhone"] = contact.MobilePhone;
			hit ["OtherPhone"] = contact.OtherPhone;
			hit ["OtherFax"] = contact.OtherFax;
			hit ["Pager"] = contact.Pager;
			hit ["PrimaryPhone"] = contact.PrimaryPhone;
			hit ["Radio"] = contact.Radio;
			hit ["Telex"] = contact.Telex;
			hit ["Tty"] = contact.Tty;
			hit ["Email1"] = contact.Email1;
			hit ["Email2"] = contact.Email2;
			hit ["Email3"] = contact.Email3;
			hit ["Mailer"] = contact.Mailer;
			hit ["Org"] = contact.Org;
			hit ["OrgUnit"] = contact.OrgUnit;
			hit ["Office"] = contact.Office;
			hit ["Title"] = contact.Title;
			hit ["Role"] = contact.Role;
			hit ["Manager"] = contact.Manager;
			hit ["Assistant"] = contact.Assistant;
			hit ["HomepageUrl"] = contact.HomepageUrl;
			hit ["BlogUrl"] = contact.BlogUrl;
			hit ["Categories"] = contact.Categories;
			hit ["Caluri"] = contact.Caluri;
			hit ["Icscalendar"] = contact.Icscalendar;
			hit ["Spouse"] = contact.Spouse;
			hit ["Note"] = contact.Note;
			
			if (contact.Photo.Data != null && contact.Photo.Data.Length > 0)
				hit.SetData ("Photo", contact.Photo.Data);

			// FIXME: List?
			// FIXME: ListShowAddresses?

			// FIXME: Should we not drop the extra Im addresses?
			if (contact.ImAim.Length > 0)
				hit ["ImAim"] = contact.ImAim [0];
			if (contact.ImIcq.Length > 0)
				hit ["ImIcq"] = contact.ImIcq [0];
			if (contact.ImJabber.Length > 0)
				hit ["ImJabber"] = contact.ImJabber [0];
			if (contact.ImMsn.Length > 0)
				hit ["ImMsn"] = contact.ImMsn [0];
			if (contact.ImYahoo.Length > 0)
				hit ["ImYahoo"] = contact.ImYahoo [0];

			String name = "";
			if (contact.GivenName != null && contact.GivenName != "")
				name = contact.GivenName;
			if (contact.FamilyName != null && contact.FamilyName != "")
				name += " " + contact.FamilyName;
			if (name.Length > 0)
				hit ["Name"] = name;

			if (hit ["Email1"] != null)
				hit ["Email"] = hit ["Email1"];

			return hit;
		}

		public String Name {
			get { return "EvolutionDataServer"; }
		}

		public bool AcceptQuery (Query query)
		{
			if (! query.HasText)
				return false;

			if (! query.AllowsDomain (QueryDomain.Local))
				return false;

			return true;
		}

		public void Query (Query query, IQueryResult result)
		{
			// FIXME: Evolution.BookQuery's bindings are all
			// screwed up, so we can't construct compound queries.
			// This will have to do for now.
			Evolution.BookQuery[] ebqs = new Evolution.BookQuery [query.Text.Count];
			for (int i = 0; i < query.Text.Count; ++i) {
				string text = (string) query.Text [i];
				ebqs [i] = Evolution.BookQuery.AnyFieldContains (text);
			}

			Evolution.BookQuery bq;
			bq = Evolution.BookQuery.And (ebqs, false);

			Evolution.Contact[] contacts;
			contacts = Addressbook.GetContacts (bq);

			if (result.Cancelled)
				return;

			ArrayList array = new ArrayList ();

			foreach (Evolution.Contact contact in contacts) {
				Hit hit = HitFromContact (contact);
				array.Add (hit);
			}
			
			// Add is a no-op if we've already cancelled.
			result.Add (array);
		}

	}

}
