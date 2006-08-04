//
// ContactWindow.cs
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

using Gtk;
using Glade;
using System;
using System.Collections;
using System.Text.RegularExpressions;

using Beagle.Util;
using Mono.Unix;

namespace ContactViewer {

	public class ContactWindow {
		private Glade.XML gxml;
		private UIManager ui_manager;
		
		[Widget] Gtk.TreeView ContactList;
		[Widget] Gtk.ComboBox ListIdentifier;
		[Widget] Gtk.Statusbar Statusbar;
		[Widget] Gtk.Window MainWindow;
		[Widget] Gtk.EventBox MenubarHolder;
		[Widget] Gtk.EventBox ContactHolder;
	
		private ListStore contact_store;
		private ListStore contact_show_type_store;
	
		private Uri uri;
		private MorkDatabase database;
		//private ContactManager contact_manager;
		
		public ContactWindow (ContactManager contact_manager, Uri uri)
		{
			this.uri = uri;
			//this.contact_manager = contact_manager;
			
			ShowWindow ();
		}
		
		public void ShowWindow ()
		{
			Application.Init ();
			
			gxml = new Glade.XML ("contactviewer.glade", "MainWindow");
			gxml.Autoconnect (this);
			
			ActionEntry[] entries = new ActionEntry [] {
				new ActionEntry ("FileMenuAction", null, "_File", null, null, null),
				new ActionEntry ("OpenAction", Gtk.Stock.Open,
					"_Open", "<control>O", Catalog.GetString ("Open..."), new EventHandler (OnOpenDatabase)),
				new ActionEntry ("QuitAction", Gtk.Stock.Quit,
					"_Quit", "<control>Q", Catalog.GetString ("Quit"), new EventHandler (OnQuit)),
				new ActionEntry ("HelpMenuAction", null, "_Help", null, null, null),
				new ActionEntry ("AboutAction", Gtk.Stock.About,
					"_About", null, Catalog.GetString ("About"), new EventHandler (OnAbout))
			};
			
			ActionGroup grp = new ActionGroup ("MainGroup");
			grp.Add (entries);
			
			ui_manager = new UIManager ();
			ui_manager.InsertActionGroup(grp, 0);
			ui_manager.AddUiFromResource ("menu.xml");
			MenubarHolder.Add (ui_manager.GetWidget ("/MainMenu"));
			
			// Fix the TreeView that will contain all contacts
			contact_store = new ListStore (typeof (string), typeof (string));
			
			ContactList.Model = contact_store;
			ContactList.RulesHint = true;
			ContactList.AppendColumn (Catalog.GetString ("Contacts"), new CellRendererText (), "text", 1);
			ContactList.ButtonReleaseEvent += OnContactSelected;
			
			// This ListStore will let the user choose what to see in the contact list
			contact_show_type_store = new ListStore (typeof (string), typeof (string));
			contact_show_type_store.AppendValues ("DisplayName", Catalog.GetString ("Display name"));
			contact_show_type_store.AppendValues ("PrimaryEmail", Catalog.GetString ("Primary E-mail"));
			contact_show_type_store.AppendValues ("SecondEmail", Catalog.GetString ("Secondary E-mail"));
			contact_show_type_store.AppendValues ("NickName", Catalog.GetString ("Nickname"));
			
			CellRendererText cell = new CellRendererText ();
			ListIdentifier.PackStart (cell, false);
			ListIdentifier.AddAttribute (cell, "text", 1);
			ListIdentifier.Model = contact_show_type_store;
			ListIdentifier.Active = 0;
			ListIdentifier.Changed += OnContactListTypeChanged;
			
			MainWindow.Icon = Beagle.Images.GetPixbuf ("contact-icon.png");
			MainWindow.DeleteEvent += OnDeleteEvent;
			
			LoadDatabase ();
			Application.Run ();
		}

		public void LoadDatabase ()
		{
			// Load the database file
			try {
				database = new MorkDatabase (uri.AbsolutePath);
				database.Read ();
				database.EnumNamespace = "ns:addrbk:db:row:scope:card:all";
			} catch (Exception e) {
				MessageDialog dialog = new MessageDialog (
					MainWindow,
					DialogFlags.DestroyWithParent, 
					MessageType.Error, 
					ButtonsType.Ok, 
					false, 
					String.Format (Catalog.GetString ("Unable to open mork database:\n\n {0}"), e.Message));
				
				dialog.Run ();
				dialog.Destroy ();
				Environment.Exit (1);
			}
			
			// Populate the gui with nice stuff
			Clear ();
			FillContactList ();
			
			try {
				Match m = Regex.Match (uri.Query, @"\?id=(?<id>[0-9A-Fa-f]+)");	
				ShowContact (m.Result ("${id}"));
			} catch (Exception e) {
				Gtk.MessageDialog dialog = new MessageDialog (
					MainWindow,
					DialogFlags.DestroyWithParent,
					MessageType.Warning,
					ButtonsType.Ok,
					Catalog.GetString ("The specified ID does not exist in this database!"));
				
				dialog.Run ();
				dialog.Destroy ();
			}
		}
		
		public void FillContactList ()
		{
			TreeIter iter;
			int count = 0;
			
			if (!ListIdentifier.GetActiveIter (out iter)) 
				return;
			
			contact_store.Clear ();
			
			// Add contacts to treeview
			foreach (string id in database) {
				Hashtable tbl = database.Compile (id, database.EnumNamespace);
				
				if (tbl ["table"] != null && tbl ["table"] as string == "BF") {
					contact_store.AppendValues (tbl ["id"], tbl [contact_show_type_store.GetValue (iter, 0)]);
					count++;
				}
			}
			
			SetStatusMessage (String.Format (Catalog.GetString ("Added {0} contacts"), count));
		}
		
		public void ShowContact (string id)
		{
			TreeIter iter;
			Hashtable tbl = database.Compile (id, database.EnumNamespace);
			
			if (ContactHolder.Child != null)
				ContactHolder.Remove (ContactHolder.Child);
			
			ContactHolder.Add (new Contact (tbl));
			MainWindow.ShowAll ();
			
			// Update selection in the contact list as well
			if (contact_store.GetIterFirst (out iter)) {
				do {
					if (contact_store.GetValue (iter, 0) as string == id) {
						ContactList.Selection.SelectIter (iter);
						break;
					}	
				} while (contact_store.IterNext (ref iter));
			}
			
			SetStatusMessage (String.Format (Catalog.GetString ("Viewing {0}"), 
				(ContactHolder.Child as Contact).GetString ("DisplayName")));
		}
		
		public void Clear ()
		{
			if (ContactHolder.Child != null)
				ContactHolder.Remove (ContactHolder.Child);
			
			contact_store.Clear ();
		}
		
		public void SetStatusMessage (string message)
		{
			Statusbar.Pop (0);
			Statusbar.Push (0, message);
		}

		protected virtual void OnContactSelected (object o, ButtonReleaseEventArgs args)
		{
			TreeIter iter;
			TreeModel model;
			
			if (!ContactList.Selection.GetSelected (out model, out iter))
				return;
			
			ShowContact ((string) model.GetValue (iter, 0));
		}
		
		protected virtual void OnContactListTypeChanged (object o, EventArgs args)
		{
			FillContactList ();
		}
		
		protected virtual void OnOpenDatabase (object o, EventArgs args)
		{
			Uri uri;
			ResponseType response;
			FileChooserDialog chooser;
			
			chooser = new FileChooserDialog (Catalog.GetString ("Select a mork database file"), 
				MainWindow, FileChooserAction.Open);
			chooser.LocalOnly = true;
			chooser.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			chooser.AddButton (Gtk.Stock.Ok, ResponseType.Ok);
			
			response = (ResponseType) chooser.Run ();
			uri = new Uri (chooser.Uri);
			chooser.Destroy ();
			
			if (response == ResponseType.Ok) {
				this.uri = uri;
				LoadDatabase ();
			}
		}
		
		protected virtual void OnAbout (object o, EventArgs args)
		{
			AboutDialog about = new AboutDialog();
			about.Authors = (new string[] { "Pierre \u00D6stlund" });
			about.Name = "Contact Viewer";
			about.Version = "0.1";
			about.Website = "http://www.beagle-project.org";
			about.Logo = Beagle.Images.GetPixbuf ("system-search.png");
			about.Icon = Beagle.Images.GetPixbuf ("icon-search.png");
			
			about.Run();
			about.Destroy();
		}
		
		protected virtual void OnQuit (object o, EventArgs args)
		{
			Application.Quit ();
		}
		
		protected virtual void OnDeleteEvent (object o, DeleteEventArgs args)
		{
			Application.Quit ();
		}
	}
	
	public class Contact : VBox {
		private Hashtable contact;
	
		public Contact (Hashtable contact) :
			base (false, 10)
		{
			HBox hbox;
			Table table;
			Button button;
			HButtonBox hbuttonbox;
			
			this.contact = contact;
			
			// Create header containing an icon and display name
			hbox = new HBox ();
			hbox.Spacing = 10;
			hbox.PackStart (Beagle.Images.GetWidget ("person.png"), false, false, 0);
			hbox.PackStart (new VLabel (String.Format ("<b><span size='large'>{0} \"{1}\" {2}</span></b>", 
				GetString ("FirstName"), GetString ("NickName"), GetString ("LastName")), false));
			PackStart (hbox, false, false, 0);
			PackStart (new HSeparator (), false, false, 0);
			
			// Create a table containing some user information
			table = new Table (5, 2, false);
			PackStart (table, false, false, 0);
			
			table.Attach (new VLabel (String.Format ("<b>{0}</b>", Catalog.GetString ("Primary E-Mail:")), false), 
				0, 1, 0, 1, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink, 10, 0);
			table.Attach (new VLabel (GetString ("PrimaryEmail"), true), 1, 2, 0, 1);
				
			table.Attach (new VLabel (String.Format ("<b>{0}</b>", Catalog.GetString ("Screen name:")), false), 
				0, 1, 1, 2, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink, 10, 0);
			table.Attach (new VLabel (GetString ("_AimScreenName"), true), 1, 2, 1, 2);
				
			table.Attach (new VLabel (String.Format ("<b>{0}</b>", Catalog.GetString ("Home phone:")), false), 
				0, 1, 2, 3, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink, 10, 0);
			table.Attach (new VLabel (GetString ("HomePhone"), true), 1, 2, 2, 3);
				
			table.Attach (new VLabel (String.Format ("<b>{0}</b>", Catalog.GetString ("Mobile phone:")), false), 
				0, 1, 3, 4, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink, 10, 0);
			table.Attach (new VLabel (GetString ("CellularNumber"), true), 1, 2, 3, 4);
				
			table.Attach (new VLabel (String.Format ("<b>{0}</b>", Catalog.GetString ("Web page:")), false), 
				0, 1, 4, 5, AttachOptions.Shrink | AttachOptions.Fill, AttachOptions.Shrink, 10, 0);
			table.Attach (new VLabel (GetString ("WebPage2"), true), 1, 2, 4, 5);
					
			// Add a button row with some informational buttons
			hbuttonbox = new HButtonBox ();
			hbuttonbox.Layout = ButtonBoxStyle.End;
			PackEnd (hbuttonbox, false, false, 0);
			
			button = new Button (Catalog.GetString ("Send E-Mail"));
			button.Clicked += OnSendEmail;
			hbuttonbox.Add (button);
			
			button = new Button (Catalog.GetString ("Details..."));
			button.Clicked += OnDetails;
			hbuttonbox.Add (button);
		}
		
		public string GetString (string str)
		{
			if (!contact.ContainsKey (str))
				return "N/A";
			
			return contact [str] as string;
		}
		
		protected virtual void OnSendEmail (object o, EventArgs args)
		{
			string mail = null;
			SafeProcess process;
			
			if (contact ["PrimaryEmail"] != null)
				mail = contact ["PrimaryEmail"] as string;
			else if (contact ["SecondEmail"] != null)
				mail = contact ["SecondMail"] as string;
			else {
				MessageDialog dialog = new MessageDialog (
					null,
					DialogFlags.DestroyWithParent, 
					MessageType.Warning,
					ButtonsType.Ok, 
					Catalog.GetString ("Could not find a valid E-mail address!"));
				
				dialog.Run ();
				dialog.Destroy ();
				return;
			}
			
			process = new SafeProcess ();
			process.Arguments = new string [2];
			process.Arguments [0] = "thunderbird";
			process.Arguments [1] = String.Format ("mailto:{0}", mail);
			process.Start ();
		}
		
		protected virtual void OnDetails (object o, EventArgs args)
		{
			new DetailedWindow (contact);
		}

		public class VLabel : Label {
			
			public VLabel (string label, bool selectable) :
				base (label)
			{
				Xalign = 0.0f;
				UseMarkup = true;
				Selectable = selectable;
			}
		}
		
	}
	
	public class DetailedWindow {
		private Glade.XML gxml;
		
		[Widget] Gtk.Button Close;
		[Widget ("DetailedWindow")] Gtk.Window Window;
		[Widget] Gtk.TextView Notes;
		[Widget] Gtk.ComboBox PreferredType;
		
		string[] widget_names = new string[] {"FirstName", "LastName", "DisplayName", 
			"NickName", "PrimaryEmail", "SecondEmail", "_AimScreenName", "WorkPhone",
			"HomePhone", "FaxNumber", "PagerNumber", "CellularNumber", "HomeAddress",
			"HomeAddress2", "HomeCity", "HomeCountry", "WebPage2", "HomeZipCode",
			"HomeState", "WorkState", "WorkZipCode", "JobTitle", "Department", "Company",
			"WorkAddress", "WorkAddress2", "WorkCity", "WorkCountry", "WebPage1",
			"Custom1", "Custom2", "Custom3", "Custom4"};
		
		public DetailedWindow (Hashtable contact)
		{
			gxml = new Glade.XML (null, "contactviewer.glade", "DetailedWindow", null);
			gxml.Autoconnect (this);
			
			// Fill all Entry-boxes with information
			foreach (string name in widget_names)
				(gxml.GetWidget (name) as Gtk.Entry).Text = (contact [name] != null ? (contact [name] as string) : "");;
			
			// Also fill the special cases
			Notes.Buffer.Text = (contact ["Notes"] != null ? (contact ["Notes"] as string) : "");
			
			try {
				int tmp = Convert.ToInt32 (contact ["PreferMailFormat"]);
				PreferredType.Active = (tmp >= 0 && tmp <= 2 ? tmp : 0);
			} catch (Exception e) {
				PreferredType.Active = 0;
			}
			
			Close.Clicked += OnClose;
			
			Window.Icon = Beagle.Images.GetPixbuf ("contact-icon.png");
			Window.Show ();
		}
		
		protected virtual void OnClose (object o, EventArgs args)
		{
			Window.Hide ();
		}
	}
}
