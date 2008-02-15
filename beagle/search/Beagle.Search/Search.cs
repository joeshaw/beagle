//
// Search.cs
//
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;
using System.Collections;
using System.Diagnostics;

using Gtk;
using NDesk.DBus;
using Mono.Unix;

namespace Beagle.Search {

	public class Search : ISearch {

		public static bool IconEnabled = false;
		public static bool SearchDocs = false;

		private uint ref_count = 0;

		public Search (string query_text)
		{
			/*if (IconEnabled) {
				tray = new Beagle.Search.Tray.TrayIcon ();
				tray.Clicked += OnTrayActivated;
				tray.Search += OnTraySearch;

				Config config = Conf.Get (Conf.Names.BeagleSearchConfig);
				bool binding_ctrl = config.GetOption (Conf.Names.KeyBinding_Ctrl, false);
				bool binding_alt = config.GetOption (Conf.Names.KeyBinding_Alt, false);
				string binding_key = config.GetOption (Conf.Names.KeyBinding_Key, "F12");

				string binding = new KeyBinding (binding_key, binding_ctrl, binding_alt).ToString ();
				string tip_text = Catalog.GetString ("Desktop Search");

				if (binding != String.Empty) {
					tip_text += String.Format (" ({0})", binding);

					// Attach the hide/show keybinding
					keybinder.Bind (binding, OnTrayActivated);
				}

				tray.TooltipText = tip_text;
			}*/
		}

		public void Query (string query_text)
		{
			SearchWindow window = new SearchWindow (query_text);
			window.DeleteEvent += OnWindowDeleteEvent;

			ref_count++;
		}

		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			if (--ref_count < 1)
				Application.Quit ();
		}
	}
}
