//
// Settings.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Threading;

using Gtk;
using Gdk;
using Glade;
using Mono.Unix;

using Beagle;
using Beagle.Util;

public class SettingsDialog 
{
	public static void Main (string[] args)
	{
		SettingsDialog settings = new SettingsDialog ();
		settings.Run ();
	}

	////////////////////////////////////////////////////////////////
	// Widgets

	[Widget] VBox administration_frame;
	[Widget] CheckButton allow_root_toggle;

	[Widget] CheckButton autostart_toggle;

	[Widget] CheckButton press_ctrl_toggle;
 	[Widget] CheckButton press_alt_toggle;
	[Widget] Entry show_search_window_entry;
	[Widget] SpinButton max_displayed_spinbutton;

	[Widget] CheckButton index_home_toggle;
	[Widget] Button remove_include_button;
	[Widget] Button remove_exclude_button;
		
	[Widget] Button display_up_button;
	[Widget] Button display_down_button;

	[Widget] Gtk.Window settings_dialog;

	[Widget] ScrolledWindow display_sw;

	[Widget] ScrolledWindow include_sw;
	[Widget] ScrolledWindow exclude_sw;
						
#if false
	DisplayView display_view;
#endif

	IncludeView include_view;
	ExcludeView exclude_view;

	////////////////////////////////////////////////////////////////
	// WebServices

	[Widget] VBox networking_box;
	[Widget] CheckButton allow_global_access_toggle;
	[Widget] Button remove_publicfolder_button;
	[Widget] Button remove_netbeagle_button;

	[Widget] ScrolledWindow publicfolder_sw;
	[Widget] ScrolledWindow netbeagle_sw;
		
	[Widget] Dialog netbeagle_entry_dialog;
	[Widget] Entry netbeagle_textentry;

	PublicfolderView	publicfolder_view;
	NetbeagleView		netbeagle_view; 

	////////////////////////////////////////////////////////////////
	// Initialize       

	public SettingsDialog ()
	{
		Application.Init ();

		Catalog.Init ("beagle", ExternalStringsHack.LocaleDir);

		Glade.XML glade = new Glade.XML (null, "settings.glade", "settings_dialog", "beagle");
		glade.Autoconnect (this);

		administration_frame.Visible = (Environment.UserName == "root");

#if false
		display_view = new DisplayView ();
		display_view.Selection.Changed += new EventHandler (OnDisplaySelected);
		display_view.Show ();
	        // The display sorting stuff is something i have been testing locally, Best CVS wont use it
		display_sw.Child = display_view;
#endif
		include_view = new IncludeView ();
		include_view.Selection.Changed += new EventHandler (OnIncludeSelected);
		include_view.Show ();
		include_sw.Child = include_view;

		exclude_view = new ExcludeView ();
		exclude_view.Selection.Changed += new EventHandler (OnExcludeSelected);
		exclude_view.Show ();
		exclude_sw.Child = exclude_view;

#if ENABLE_WEBSERVICES	
		networking_box.Visible = true;

		publicfolder_view = new PublicfolderView ();
		publicfolder_view.Selection.Changed += new EventHandler (OnPublicfolderSelected);
		publicfolder_view.Show ();
		publicfolder_sw.Child = publicfolder_view;
			
		netbeagle_view = new NetbeagleView ();
		netbeagle_view.Selection.Changed += new EventHandler (OnNetbeagleSelected);
		netbeagle_view.Show ();
		netbeagle_sw.Child = netbeagle_view;
#endif
		LoadConfiguration ();

		Conf.Subscribe (typeof (Conf.IndexingConfig), new Conf.ConfigUpdateHandler (OnConfigurationChanged));
		Conf.Subscribe (typeof (Conf.SearchingConfig), new Conf.ConfigUpdateHandler (OnConfigurationChanged));

#if ENABLE_WEBSERVICES	
		Conf.Subscribe (typeof (Conf.NetworkingConfig), new Conf.ConfigUpdateHandler (OnConfigurationChanged));
		Conf.Subscribe (typeof (Conf.WebServicesConfig), new Conf.ConfigUpdateHandler (OnConfigurationChanged));
#endif 		
	}

	public void Run ()
	{
		Application.Run ();
	}

	////////////////////////////////////////////////////////////////
	// Configuration

	private void LoadConfiguration ()
	{	
		allow_root_toggle.Active = Conf.Daemon.AllowRoot;
		autostart_toggle.Active = Conf.Searching.Autostart;

		KeyBinding show_binding = Conf.Searching.ShowSearchWindowBinding;
		press_ctrl_toggle.Active = show_binding.Ctrl;
		press_alt_toggle.Active = show_binding.Alt;
		show_search_window_entry.Text = show_binding.Key;
		max_displayed_spinbutton.Value = (uint) Conf.Searching.MaxDisplayed;

		if (Conf.Indexing.IndexHomeDir)
			index_home_toggle.Active = true;

		foreach (string include in Conf.Indexing.Roots)
			include_view.AddPath (include);

		foreach (ExcludeItem exclude_item in Conf.Indexing.Excludes)
			exclude_view.AddItem (exclude_item);

#if ENABLE_WEBSERVICES				
		foreach (string netbeagle in Conf.Networking.NetBeagleNodes)
				netbeagle_view.AddNode (netbeagle);

		if (Conf.WebServices.AllowGlobalAccess)
				allow_global_access_toggle.Active = true;
								
		foreach (string publicfolder in Conf.WebServices.PublicFolders)
			publicfolder_view.AddPath (publicfolder);
#endif 											
	}

	private void SaveConfiguration ()
	{
		Conf.Daemon.AllowRoot = allow_root_toggle.Active;
		Conf.Searching.Autostart = autostart_toggle.Active;
		
		Conf.Searching.ShowSearchWindowBinding = new KeyBinding (show_search_window_entry.Text, 
									 press_ctrl_toggle.Active, 
									 press_alt_toggle.Active);
		
		Conf.Searching.MaxDisplayed = (int) max_displayed_spinbutton.Value;
		Conf.Indexing.IndexHomeDir = index_home_toggle.Active;
		
		Conf.Indexing.Roots = include_view.Includes;
		Conf.Indexing.Excludes = exclude_view.Excludes;

#if ENABLE_WEBSERVICES	
		Conf.Networking.NetBeagleNodes = netbeagle_view.Netbeagles;
		Conf.WebServices.AllowGlobalAccess = allow_global_access_toggle.Active;
		Conf.WebServices.PublicFolders = publicfolder_view.Publicfolders;
#endif 			
		Conf.Save (true);
	}

	private void OnConfigurationChanged (Conf.Section section)
	{
		HigMessageDialog dialog = new HigMessageDialog (settings_dialog,
								DialogFlags.Modal,
								MessageType.Question,
								ButtonsType.YesNo,
								Catalog.GetString ("Reload configuration"),
								Catalog.GetString ("The configuration file has been modified by another application. " + 
										   "Do you wish to discard the currently displayed values and reload configuration from disk?"));

		ResponseType response = (ResponseType) dialog.Run ();

		if (response == ResponseType.Yes)
			LoadConfiguration ();

		dialog.Destroy ();
	}

	////////////////////////////////////////////////////////////////
	// Eventhandlers

	private void OnDialogResponse (object o, ResponseArgs args)
	{
		switch (args.ResponseId) {
		case ResponseType.Help:
			Gnome.Url.Show ("http://www.beagle-project.org/Configuration_Help");
			break;
		case ResponseType.Ok:
			SaveConfiguration ();
			Application.Quit ();
			break;
		default:
			Application.Quit ();
			break;
		}
	}

	private void OnDisplaySelected (object o, EventArgs args)
	{
		display_up_button.Sensitive = true;
		display_down_button.Sensitive = true;
	}

	private void OnAddIncludeClicked (object o, EventArgs args)
	{
		CompatFileChooserDialog fs_dialog = new CompatFileChooserDialog (Catalog.GetString ("Select Path"), 
										 settings_dialog, 
										 CompatFileChooserDialog.Action.SelectFolder);
		fs_dialog.SelectMultiple = false;

		ResponseType fs_response = (ResponseType) fs_dialog.Run ();
		string new_include = fs_dialog.Filename;
		fs_dialog.Destroy ();
		
		if (fs_response == ResponseType.Ok) {
			string error_message = "";
			bool throw_error = false;
			ArrayList obsolete_includes = new ArrayList ();

			// Check and see if the current data collides with the new path in any way
			// FIXME: Do this with System.IO.Path or something
			foreach (string old_include in include_view.Includes) {
				if (new_include == old_include) {
					throw_error = true;
					error_message = Catalog.GetString ("The selected path is already selected for indexing and wasn't added.");
				} else if (new_include.StartsWith (old_include)) {
					throw_error = true;
					error_message = Catalog.GetString ("The selected path wasn't added. The list contains items that supercedes it and the data is already being indexed.");
				} else if (old_include.StartsWith (new_include)) {
					obsolete_includes.Add (old_include);
				}
			}

			if (throw_error) {
				HigMessageDialog.RunHigMessageDialog (settings_dialog,
								      DialogFlags.Modal,
								      MessageType.Warning,
								      ButtonsType.Ok,
								      Catalog.GetString ("Path not added"),
								      error_message);
			} else {
				// Confirm the removal of obsolete includes
				if (obsolete_includes.Count != 0) {
					HigMessageDialog dialog = new HigMessageDialog (settings_dialog,
											DialogFlags.Modal,
											MessageType.Question,
											ButtonsType.YesNo,
											Catalog.GetString ("Remove obsolete paths"),
											Catalog.GetString ("Adding this path will obsolete some of the existing include paths. " + 
													   "This will result in the removal of the old obsolete paths. Do you still wish to add it?"));
					
					ResponseType confirm_response = (ResponseType) dialog.Run ();
					
					if (confirm_response != ResponseType.Yes)
						return;

					foreach (string obsolete_include in obsolete_includes)
						include_view.RemovePath (obsolete_include);

					dialog.Destroy ();
				}

				include_view.AddPath (new_include);
			}
		}
	}

	private void OnRemoveIncludeClicked (object o, EventArgs args)
	{
		// Confirm removal
		HigMessageDialog dialog  = new HigMessageDialog (settings_dialog,
								 DialogFlags.Modal,
								 MessageType.Question,
								 ButtonsType.YesNo,
								 Catalog.GetString ("Remove path"),
								 Catalog.GetString ("Are you sure you wish to remove this path from the list of directories to be included for indexing?"));
		ResponseType response = (ResponseType) dialog.Run ();
		dialog.Destroy ();

		if (response != ResponseType.Yes)
			return;

		include_view.RemoveSelectedPath ();
		remove_include_button.Sensitive = false;
	}

	private void OnIncludeSelected (object o, EventArgs args)
	{
		remove_include_button.Sensitive = true;
	}

	private void OnAddExcludeClicked (object o, EventArgs args)
	{
		AddExcludeDialog dialog = new AddExcludeDialog (settings_dialog);
		dialog.ExcludeItemAddedEvent += new ExcludeItemAddedHandler (OnExcludeItemAdded);

	}

	private void OnRemoveExcludeClicked (object o, EventArgs args)
	{
		HigMessageDialog dialog = new HigMessageDialog (settings_dialog,
								DialogFlags.Modal,
								MessageType.Question,
								ButtonsType.YesNo,
								Catalog.GetString ("Remove item"),
								Catalog.GetString ("Are you sure you wish to remove this item from the list of data to be excluded from indexing?"));

		ResponseType response = (ResponseType) dialog.Run ();
		dialog.Destroy ();

		if (response != ResponseType.Yes)
			return;

		exclude_view.RemoveSelectedItem ();
		remove_exclude_button.Sensitive = false;
	}

	private void OnExcludeSelected (object o, EventArgs args)
	{
		remove_exclude_button.Sensitive = true;
	}

	private void OnExcludeItemAdded (ExcludeItem exclude_item)
	{
		exclude_view.AddItem (exclude_item);
	}

	//Netbeagle: Add, Remove operations
	private void OnAddNetbeagleClicked (object o, EventArgs args)
	{	
		Glade.XML glade = new Glade.XML (null, "settings.glade", "netbeagle_entry_dialog", "beagle");
		glade.Autoconnect (this);
		ResponseType nbed_response = (ResponseType) netbeagle_entry_dialog.Run ();

		string new_node = netbeagle_textentry.Text.Trim();
								
		if ((new_node.Length > 1) && (((string[])new_node.Split(':')).Length < 2))
			new_node += ":8888";
			
		netbeagle_entry_dialog.Destroy ();
		
		if (nbed_response == ResponseType.Ok) {
			string error_message = "";
			bool throw_error = false;

			if (new_node.Length < 2)
			{
				throw_error = true;
				error_message = Catalog.GetString ("Invalid host entry");			
			}
			else
				// Check if the new entry matches an existing netbeagle entry
				foreach (string old_node in netbeagle_view.Netbeagles) {
					if (new_node == old_node) {
						throw_error = true;
						error_message = Catalog.GetString ("Remote host already present in the list.");
					} 
				}

			if (throw_error) {
				HigMessageDialog.RunHigMessageDialog (settings_dialog,
								      DialogFlags.Modal,
								      MessageType.Warning,
								      ButtonsType.Ok,
								      Catalog.GetString ("Netbeagle Node not added"),
								      error_message);
			} 
			else 
					netbeagle_view.AddNode (new_node);

		}
	}

	private void OnRemoveNetbeagleClicked (object o, EventArgs args)
	{
		// Confirm removal
		HigMessageDialog dialog  = new HigMessageDialog (settings_dialog,
								 DialogFlags.Modal,
								 MessageType.Question,
								 ButtonsType.YesNo,
								 Catalog.GetString ("Remove host"),
								 Catalog.GetString ("Are you sure you wish to remove this host from the list?"));

		ResponseType response = (ResponseType) dialog.Run ();
		dialog.Destroy ();

		if (response != ResponseType.Yes)
			return;

		netbeagle_view.RemoveSelectedNode ();
		
		remove_netbeagle_button.Sensitive = false;
	}

	private void OnNetbeagleSelected (object o, EventArgs args)
	{
		remove_netbeagle_button.Sensitive = true;
	}
	
	//Publicfolders: Add, Remove operations
	private void OnAddPublicfolderClicked (object o, EventArgs args)
	{
		CompatFileChooserDialog fs_dialog = new CompatFileChooserDialog (Catalog.GetString ("Select path"), 
										 settings_dialog, 
										 CompatFileChooserDialog.Action.SelectFolder);
		fs_dialog.SelectMultiple = false;

		ResponseType fs_response = (ResponseType) fs_dialog.Run ();
		string new_pf = fs_dialog.Filename;
		fs_dialog.Destroy ();
		
		if (fs_response == ResponseType.Ok) {
		
			string error_message = "";
			bool throw_error = false;

			// Check and see if the current data collides with the new path in any way
			// FIXME: Do this with System.IO.Path or something
			foreach (string old_pf in publicfolder_view.Publicfolders) {
				if (new_pf == old_pf) {
					throw_error = true;
					error_message = Catalog.GetString ("The selected path is already configured for external access.");
				} 
			}

			if (throw_error) {
				HigMessageDialog.RunHigMessageDialog (settings_dialog,
								      DialogFlags.Modal,
								      MessageType.Warning,
								      ButtonsType.Ok,
								      Catalog.GetString ("Path not added"),
								      error_message);
			} 
			else 
					publicfolder_view.AddPath (new_pf);		
		}
	}

	private void OnRemovePublicfolderClicked (object o, EventArgs args)
	{
		// Confirm removal
		HigMessageDialog dialog  = new HigMessageDialog (settings_dialog,
								 DialogFlags.Modal,
								 MessageType.Question,
								 ButtonsType.YesNo,
								 Catalog.GetString ("Remove public path"),
								 Catalog.GetString ("Are you sure you wish to remove this entry from the list of public paths?"));

		ResponseType response = (ResponseType) dialog.Run ();
		dialog.Destroy ();

		if (response != ResponseType.Yes)
			return;

		publicfolder_view.RemoveSelectedPath ();
		remove_publicfolder_button.Sensitive = false;
	}

	private void OnPublicfolderSelected (object o, EventArgs args)
	{
		remove_publicfolder_button.Sensitive  = true;
	}

	////////////////////////////////////////////////////////////////
	// IncludeView 

	class IncludeView : TreeView 
	{
		private ListStore store;

		private ArrayList includes = new ArrayList ();

		public ArrayList Includes {
			get { return includes; }
		}

		private enum TargetType {
			Uri,
		};

		private static TargetEntry [] target_table = new TargetEntry [] {
			new TargetEntry ("STRING", 0, (uint) TargetType.Uri ),
			new TargetEntry ("text/plain", 0, (uint) TargetType.Uri),
		};

		public IncludeView ()
		{
			store = new ListStore (typeof (string));

			this.Model = store;

			AppendColumn (Catalog.GetString ("Name"), new CellRendererText (), "text", 0);

			// Enable drag and drop folders from nautilus
			Gtk.Drag.DestSet (this, DestDefaults.All, target_table, DragAction.Copy | DragAction.Move);
			DragDataReceived += new DragDataReceivedHandler (HandleData);
		}

		public void AddPath (string path)
		{
			includes.Add (path);
			store.AppendValues (path);
		} 

		public void RemovePath (string path)
		{
			find_path = path;
			found_iter = TreeIter.Zero;

			this.Model.Foreach (new TreeModelForeachFunc (ForeachFindPath));

			store.Remove (ref found_iter);
			includes.Remove (path);
		}

		private string find_path;
		private TreeIter found_iter;

		private bool ForeachFindPath (TreeModel model, TreePath path, TreeIter iter)
		{
			if ((string) model.GetValue (iter, 0) == find_path) {
				found_iter = iter;
				return true;
			}

			return false;
		}

		public void RemoveSelectedPath ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected(out model, out iter)) {
				return;
			}
			string path = (string)model.GetValue(iter, 0);

			store.Remove (ref iter);
			includes.Remove (path);
		}

	        // Handle drag and drop data. Enables users to drag a folder that he wishes 
		// to add for indexing from Nautilus.
		// FIXME: Pass checks as in OnAddIncludeButtonClicked
		private void HandleData (object o, DragDataReceivedArgs args) {
			Uri uri;
			if (args.SelectionData.Length >=0 && args.SelectionData.Format == 8) {
				uri = new Uri (args.SelectionData.Text.Trim ());
				AddPath (uri.LocalPath);
				Gtk.Drag.Finish (args.Context, true, false, args.Time);
			}
			Gtk.Drag.Finish (args.Context, false, false, args.Time);
		}
	}

	////////////////////////////////////////////////////////////////
	// Exclude view

	class ExcludeView : TreeView 
	{
		ArrayList excludes = new ArrayList ();

		public ArrayList Excludes {
			get { return excludes; }
		}
			
		public ExcludeView ()
		{
			this.Model =  new ListStore (typeof (ExcludeItem));

			CellRendererText renderer_text = new CellRendererText ();

			TreeViewColumn type_column = new TreeViewColumn ();
			type_column.Title = Catalog.GetString ("Type");
			type_column.PackStart (renderer_text, false);
			type_column.SetCellDataFunc (renderer_text, new TreeCellDataFunc (TypeCellDataFunc));
			AppendColumn (type_column);

			TreeViewColumn name_column = new TreeViewColumn ();
			name_column.Title = Catalog.GetString ("Name");
			name_column.PackStart (renderer_text, false);
			name_column.SetCellDataFunc (renderer_text, new TreeCellDataFunc (NameCellDataFunc));
			AppendColumn (name_column);
		}

		public void RemoveSelectedItem ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected(out model, out iter)) {
				return;
			}
			ExcludeItem exclude_item = (ExcludeItem) model.GetValue(iter, 0);

		        ((ListStore)this.Model).Remove (ref iter);
			excludes.Remove (exclude_item);			
		}

		public void AddItem (ExcludeItem exclude_item)
		{
			excludes.Add (exclude_item);
			((ListStore)this.Model).AppendValues (exclude_item);
		}

		private void NameCellDataFunc (TreeViewColumn column,
					       CellRenderer renderer,
					       TreeModel model,
					       TreeIter iter)
		{
			ExcludeItem exclude_item = (ExcludeItem) model.GetValue (iter, 0);
			if (exclude_item.Type == ExcludeType.MailFolder)
				((CellRendererText)renderer).Text = MailFolder.GetNameForPath (exclude_item.Value);
			else
				((CellRendererText)renderer).Text = exclude_item.Value;
		}

		private void TypeCellDataFunc (TreeViewColumn column,
						CellRenderer renderer,
						TreeModel model,
						TreeIter iter)
		{			
			ExcludeItem exclude_item = (ExcludeItem) model.GetValue (iter, 0);

			switch (exclude_item.Type) {
			case ExcludeType.Path:
				((CellRendererText)renderer).Text = Catalog.GetString ("P_ath:");
				break;
			case ExcludeType.Pattern:
				((CellRendererText)renderer).Text = Catalog.GetString ("P_attern:");
				break;
			case ExcludeType.MailFolder:
				((CellRendererText)renderer).Text = Catalog.GetString ("M_ail folder:");
				break;
			}
		}
	}

	////////////////////////////////////////////////////////////////

	// NetbeagleView 	
	class NetbeagleView : TreeView 
	{
		private ListStore store;

		private ArrayList netBeagleList = new ArrayList ();

		public ArrayList Netbeagles {
			get { return netBeagleList; }
		}

		public NetbeagleView ()
		{
			store = new ListStore (typeof (string));

			this.Model = store;

			AppendColumn (Catalog.GetString ("Name"), new CellRendererText (), "text", 0);
		}

		public void AddNode (string node)
		{
			netBeagleList.Add (node);
			store.AppendValues (node);
		} 

		public void RemoveNode (string node)
		{
			find_node = node;
			found_iter = TreeIter.Zero;

			this.Model.Foreach (new TreeModelForeachFunc (ForeachFindNode));

			store.Remove (ref found_iter);
			netBeagleList.Remove (node);
		}

		private string find_node;
		private TreeIter found_iter;

		private bool ForeachFindNode (TreeModel model, TreePath path, TreeIter iter)
		{
			if ((string) model.GetValue (iter, 0) == find_node) {
				found_iter = iter;
				return true;
			}

			return false;
		}

		public void RemoveSelectedNode ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected(out model, out iter)) {
				return;
			}
			string node = (string)model.GetValue(iter, 0);

			store.Remove (ref iter);
			netBeagleList.Remove (node);
		}
		
	}
	////////////////////////////////////////////////////////////////
	// PublicfolderView 

	class PublicfolderView: TreeView 
	{
		private ListStore store;

		private ArrayList publicFolders = new ArrayList ();

		public ArrayList Publicfolders {
			get { return publicFolders; }
		}

		private enum TargetType {
			Uri,
		};

		private static TargetEntry [] target_table = new TargetEntry [] {
			new TargetEntry ("STRING", 0, (uint) TargetType.Uri ),
			new TargetEntry ("text/plain", 0, (uint) TargetType.Uri),
		};

		public PublicfolderView ()
		{
			store = new ListStore (typeof (string));

			this.Model = store;

			AppendColumn (Catalog.GetString ("Name"), new CellRendererText (), "text", 0);

			// Enable drag and drop folders from nautilus
			Gtk.Drag.DestSet (this, DestDefaults.All, target_table, DragAction.Copy | DragAction.Move);
			DragDataReceived += new DragDataReceivedHandler (HandleData);
		}

		public void AddPath (string path)
		{
			publicFolders.Add (path);
			store.AppendValues (path);
		} 

		public void RemovePath (string path)
		{
			find_path = path;
			found_iter = TreeIter.Zero;

			this.Model.Foreach (new TreeModelForeachFunc (ForeachFindPath));

			store.Remove (ref found_iter);
			publicFolders.Remove (path);
		}

		private string find_path;
		private TreeIter found_iter;

		private bool ForeachFindPath (TreeModel model, TreePath path, TreeIter iter)
		{
			if ((string) model.GetValue (iter, 0) == find_path) {
				found_iter = iter;
				return true;
			}

			return false;
		}

		public void RemoveSelectedPath ()
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected(out model, out iter)) {
				return;
			}
			string path = (string)model.GetValue(iter, 0);

			store.Remove (ref iter);
			publicFolders.Remove (path);
		}

	    // Handle drag and drop data. Enables users to drag a folder that he wishes 
		// to add for indexing from Nautilus.
		// FIXME: Pass checks as in OnAddIncludeButtonClicked
		private void HandleData (object o, DragDataReceivedArgs args) {
			Uri uri;
			if (args.SelectionData.Length >=0 && args.SelectionData.Format == 8) {
				uri = new Uri (args.SelectionData.Text.Trim ());
				AddPath (uri.LocalPath);
				Gtk.Drag.Finish (args.Context, true, false, args.Time);
			}
			Gtk.Drag.Finish (args.Context, false, false, args.Time);
		}
	}

	////////////////////////////////////////////////////////////////
	// Mail folder dialog

	class MailFolderDialog 
	{
		Gtk.Window parent;
		FolderView folder_view;

		[Widget] Dialog mail_folder_dialog;
		[Widget] ScrolledWindow folder_sw;

		public event ExcludeItemAddedHandler ExcludeItemAddedEvent;

		public MailFolderDialog (Gtk.Window parent)
		{
			this.parent = parent;

			Glade.XML glade = new Glade.XML (null, "settings.glade", "mail_folder_dialog", "beagle");
			glade.Autoconnect (this);

			folder_view = new FolderView ();
			folder_view.Show ();
			folder_sw.Child = folder_view;
		}

		private void OnDialogResponse (object o, ResponseArgs args)
		{
			if (args.ResponseId == ResponseType.Cancel) {
				mail_folder_dialog.Destroy ();
				return;
			}

			ExcludeItem exclude_item;
			object obj = folder_view.GetCurrentItem ();

			if (obj is MailAccount) {

			} else if (obj is MailFolder) {
				MailFolder folder = (MailFolder) obj;
				exclude_item = new ExcludeItem (ExcludeType.MailFolder, folder.Path);

				if (ExcludeItemAddedEvent != null)
					ExcludeItemAddedEvent (exclude_item);

				mail_folder_dialog.Destroy ();
			}
		}

		class FolderView : TreeView
		{
			TreeStore store;
			Gdk.Pixbuf folder_icon;

			public FolderView ()
			{
				store = new TreeStore (typeof (MailFolder));
				this.Model = store;

				folder_icon = this.RenderIcon (Stock.Open, IconSize.Menu, "");

				HeadersVisible = false;

				TreeViewColumn column = new TreeViewColumn ();
				CellRendererPixbuf renderer_icon = new CellRendererPixbuf ();
				column.PackStart (renderer_icon, false);
				column.SetCellDataFunc (renderer_icon, new TreeCellDataFunc (IconCellDataFunc));

				CellRendererText renderer_text = new CellRendererText ();
				column.PackStart (renderer_text, false);
				column.SetCellDataFunc (renderer_text, new TreeCellDataFunc (NameCellDataFunc));
				AppendColumn (column);

				foreach (MailAccount account in Beagle.Util.Evolution.Accounts) {
					TreeIter iter = store.AppendValues (account);

					foreach (MailFolder folder in account.Children) {
						Add (iter, folder);
					}
				}
			}

			private void Add (TreeIter parent, MailFolder folder)
			{
				TreeIter current = store.AppendValues (parent, folder);
				
				foreach (MailFolder child in folder.Children)
					Add (current, child);
			}

			private void IconCellDataFunc (TreeViewColumn column,
						       CellRenderer renderer,
						       TreeModel model,
						       TreeIter iter)
			{
				object obj = model.GetValue (iter, 0);
				((CellRendererPixbuf)renderer).Pixbuf = (obj is MailAccount) ? null : folder_icon;
			}

			private void NameCellDataFunc (TreeViewColumn column,
						       CellRenderer renderer,
						       TreeModel model,
						       TreeIter iter)
			{
				object obj = model.GetValue (iter, 0);

				if (obj is MailAccount) {
					MailAccount account = obj as MailAccount;
					((CellRendererText)renderer).Markup = String.Format ("<b>{0}</b>",account.Name);
				} else {
					MailFolder folder = obj as MailFolder;
					((CellRendererText)renderer).Text = folder.Name;
				}
			}
			
			public MailFolder GetCurrentItem ()
			{
				TreeModel model;
				TreeIter iter;
				
				if (!this.Selection.GetSelected (out model, out iter)) {
					return null;
				}

				return (MailFolder) model.GetValue (iter, 0);
			}
		}
	}

	////////////////////////////////////////////////////////////////
	// Exclude dialog

	private delegate void ExcludeItemAddedHandler (ExcludeItem item);

	class AddExcludeDialog 
	{
		Gtk.Window parent;

		[Widget] Dialog add_exclude_dialog;
		[Widget] Entry value_entry;
		[Widget] Label value_name_label;
		[Widget] Button browse_button;

		[Widget] RadioButton type_path_radio;
		[Widget] RadioButton type_pattern_radio;
		[Widget] RadioButton type_mailfolder_radio;

		public event ExcludeItemAddedHandler ExcludeItemAddedEvent;

		private string value;

		public string Value {
			get { 
				if (Type == ExcludeType.MailFolder)
					return value;
				else 
					return value_entry.Text; 
			}
		}

		public ExcludeType Type {
			get {
				if (type_path_radio.Active) 
					return ExcludeType.Path;
				else if (type_pattern_radio.Active) 
					return ExcludeType.Pattern;
				else
					return ExcludeType.MailFolder;
			}
		}

		public AddExcludeDialog (Gtk.Window parent)
		{
			this.parent = parent;

			Glade.XML glade = new Glade.XML (null, "settings.glade", "add_exclude_dialog", "beagle");
			glade.Autoconnect (this);
		}

		private void OnBrowseButtonClicked (object o, EventArgs args)
		{
			switch (Type) {
			case ExcludeType.Path:
				CompatFileChooserDialog fs_dialog = new CompatFileChooserDialog (Catalog.GetString ("Select Folder"), 
												 add_exclude_dialog, 
												 CompatFileChooserDialog.Action.SelectFolder);
				fs_dialog.SelectMultiple = false;
				
				ResponseType response = (ResponseType) fs_dialog.Run ();

				if (response == ResponseType.Ok)
					value_entry.Text = fs_dialog.Filename;

				fs_dialog.Destroy ();
				break;
			case ExcludeType.MailFolder:
				MailFolderDialog mf_dialog = new MailFolderDialog (add_exclude_dialog);
				mf_dialog.ExcludeItemAddedEvent += new ExcludeItemAddedHandler (OnExcludeItemAdded);
				break;
			}
		}

		private void OnRadioGroupChanged (object o, EventArgs args)
		{
			value_entry.Text = "";

			switch (Type) {
			case ExcludeType.Path:
				browse_button.Sensitive = true;
				value_name_label.TextWithMnemonic = Catalog.GetString ("P_ath:");
				value_entry.IsEditable = true;
				break;
			case ExcludeType.MailFolder:
				browse_button.Sensitive = true;
				value_name_label.TextWithMnemonic = Catalog.GetString ("M_ail folder:");
				value_entry.IsEditable = false;
				break;
			case ExcludeType.Pattern:
				browse_button.Sensitive = false;
				value_name_label.TextWithMnemonic = Catalog.GetString ("P_attern:");
				value_entry.IsEditable = true;
				break;
			}
		}
		
		private void OnExcludeItemAdded (ExcludeItem item)
		{
			value = item.Value; 
			value_entry.Text = MailFolder.GetNameForPath (item.Value);
		}

		private void OnDialogResponse (object o, ResponseArgs args)
		{
			if (((ResponseType)args.ResponseId) == ResponseType.Ok) {
				ExcludeItem exclude_item = new ExcludeItem (Type, Value);
				
				switch (Type) {
				case ExcludeType.Path:
					if (!Directory.Exists (Value)) {
						HigMessageDialog.RunHigMessageDialog(add_exclude_dialog,
										     DialogFlags.Modal,
										     MessageType.Error,
										     ButtonsType.Ok,
										     Catalog.GetString ("Error adding path"),
										     Catalog.GetString ("The specified path could not be found and therefore it could not be added to the list of resources excluded for indexing."));
						return;
					}
					break;
				}
				
				if (ExcludeItemAddedEvent != null)
					ExcludeItemAddedEvent (exclude_item);
			}
			add_exclude_dialog.Destroy ();
		}
		
		public void Destroy ()
		{
			add_exclude_dialog.Destroy ();
		}
	}
	
#if false
	////////////////////////////////////////////////////////////////
	// Display view
	
	class DisplayView : TreeView 
	{
		public DisplayView ()
		{
			ListStore store = new ListStore (typeof(bool),typeof(string));

			this.Model = store;

			AppendColumn (Catalog.GetString ("Show"), new CellRendererToggle (), "active", 0);
			AppendColumn (Catalog.GetString ("Name"), new CellRendererText (), "text", 1);
		}
	}
#endif
}
