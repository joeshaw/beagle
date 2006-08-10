//
// Contact.cs: Adds address book indexing support to the Thunderbird backend
//
// Copyright (C) 2006 Pierre Östlund
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using Beagle.Util;
using Beagle.Daemon;
using TB = Beagle.Util.Thunderbird;

using GMime;

namespace Beagle.Daemon.ThunderbirdQueryable {	
	
	[ThunderbirdIndexableGenerator (TB.AccountType.AddressBook, "Address book support", true)]
	public class ContactIndexableGenerator : ThunderbirdIndexableGenerator {

		public ContactIndexableGenerator (ThunderbirdIndexer indexer, TB.Account account, string abook_file)
			: base (indexer, account, abook_file)
		{
		}
		
		public override bool HasNextIndexable ()
		{
			do {
				if (DbEnumerator == null || !DbEnumerator.MoveNext ()) {
					Done = true;
					indexer.NotificationEvent -= OnNotification;
					indexer.ChildComplete ();
					return false;
				}
			} while ((DbEnumerator.Current as TB.Contact).GetString ("table") != "BF" ||
				IsUpToDate ((DbEnumerator.Current as TB.Contact).Uri));
			
			return true;
		}
		
		public override Indexable GetNextIndexable ()
		{
			return ContactToIndexable (DbEnumerator.Current as TB.Contact);
		}
		
		public override void LoadDatabase ()
		{
			try {
				db = new TB.Database (account, DbFile);
				db.Load ();
			} catch (Exception e) {
				Logger.Log.Warn (e, "Failed to load {0}:", DbFile);
				return;
			}
			
			if (db.Count <= 0)
				return;
			
			Logger.Log.Info ("Indexing address book containing {0} contact(s) ({1})", db.Count, RelativePath);
		}

		private Indexable ContactToIndexable (TB.Contact contact)
		{
			Indexable indexable = NewIndexable (contact.Uri, DateTime.Now.ToUniversalTime (), "Contact");
			
			indexable.AddProperty (Property.New ("fixme:FirstName", contact.GetString ("FirstName")));
			indexable.AddProperty (Property.New ("fixme:LastName", contact.GetString ("LastName")));
			indexable.AddProperty (Property.New ("fixme:DisplayName", contact.GetString ("LastName")));
			indexable.AddProperty (Property.New ("fixme:NickName", contact.GetString ("NickName")));
			indexable.AddProperty (Property.NewKeyword ("fixme:PrimaryEmail", contact.GetString ("PrimaryEmail")));
			indexable.AddProperty (Property.NewKeyword ("fixme:SecondEmail", contact.GetString ("SecondEmail")));
			indexable.AddProperty (Property.New ("fixme:WorkPhone", contact.GetString ("WorkPhone")));
			indexable.AddProperty (Property.New ("fixme:FaxNumber", contact.GetString ("FaxNumber")));
			indexable.AddProperty (Property.New ("fixme:HomePhone", contact.GetString ("HomePhone")));
			indexable.AddProperty (Property.New ("fixme:PagerNumber", contact.GetString ("PagerNumber")));
			indexable.AddProperty (Property.New ("fixme:CellularNumber", contact.GetString ("CellularNumber")));
			indexable.AddProperty (Property.New ("fixme:HomeAddress", contact.GetString ("HomeAddress")));
			indexable.AddProperty (Property.New ("fixme:HomeAddress2", contact.GetString ("HomeAddress2")));
			indexable.AddProperty (Property.New ("fixme:HomeCity", contact.GetString ("HomeCity")));
			indexable.AddProperty (Property.New ("fixme:HomeState", contact.GetString ("HomeState")));
			indexable.AddProperty (Property.New ("fixme:HomeZipCode", contact.GetString("HomeZipCode")));
			indexable.AddProperty (Property.New ("fixme:HomeCountry", contact.GetString ("HomeCountry")));
			indexable.AddProperty (Property.New ("fixme:WorkAddress", contact.GetString ("WorkAddress")));
			indexable.AddProperty (Property.New ("fixme:WorkAddress2", contact.GetString ("WorkAddress2")));
			indexable.AddProperty (Property.New ("fixme:WorkCity", contact.GetString ("WorkCity")));
			indexable.AddProperty (Property.New ("fixme:WorkState", contact.GetString ("WorkState")));
			indexable.AddProperty (Property.New ("fixme:WorkZipCode", contact.GetString ("WorkZipCode")));
			indexable.AddProperty (Property.New ("fixme:WorkCountry", contact.GetString ("WorkCountry")));
			indexable.AddProperty (Property.New ("fixme:JobTitle", contact.GetString ("JobTitle")));
			indexable.AddProperty (Property.New ("fixme:Department", contact.GetString ("Department")));
			indexable.AddProperty (Property.New ("fixme:Company", contact.GetString ("Company")));
			indexable.AddProperty (Property.New ("fixme:_AimScreenName", contact.GetString ("_AimScreenName")));
			indexable.AddProperty (Property.New ("fixme:FamilyName", contact.GetString ("FamilyName")));
			indexable.AddProperty (Property.NewKeyword ("fixme:WebPage1", contact.GetString ("WebPage1")));
			indexable.AddProperty (Property.NewKeyword ("fixme:WebPage2", contact.GetString ("WebPage2")));
			indexable.AddProperty (Property.New ("fixme:BirthYear", contact.GetString ("BirthYear")));
			indexable.AddProperty (Property.New ("fixme:BirthMonth", contact.GetString ("BirthMonth")));
			indexable.AddProperty (Property.New ("fixme:BirthDay", contact.GetString ("BirthDay")));
			indexable.AddProperty (Property.New ("fixme:Custom1", contact.GetString ("Custom1")));
			indexable.AddProperty (Property.New ("fixme:Custom2", contact.GetString ("Custom2")));
			indexable.AddProperty (Property.New ("fixme:Custom3", contact.GetString ("Custom3")));
			indexable.AddProperty (Property.New ("fixme:Custom4", contact.GetString ("Custom4")));
			indexable.AddProperty (Property.New ("fixme:Notes", contact.GetString ("Notes")));
			indexable.AddProperty (Property.New ("fixme:PreferMailFormat", contact.GetString ("PreferMailFormat")));
			
			indexable.AddProperty (Property.NewKeyword ("fixme:Email", contact.GetString ("PrimaryEmail")));
			indexable.AddProperty (Property.New ("fixme:Name", contact.GetString ("DisplayName")));
			
			return indexable;
		}
		
		// Why? Because it's very likely that the user will sometimes change contact details. Current IsUpToDate
		// (in ThunderbirdIndexableGenerator-class) only checks the "fullyIndexed" property and when the contact
		// was indexed, thus if the user changes an email address it won't be updated until beagle is restarted.
		// By always returning false here, we make sure that beagle always re-index contacts when something
		// happens. It's a really fast and not a very cpu intensive task, so it doesn't really matter.
		protected new bool IsUpToDate (Uri uri)
		{
			// Remove this uri from the cache
			if (stored_cache != null)
				stored_cache.Remove (uri.ToString ());
			
			return false;
		}
	}
}
	
