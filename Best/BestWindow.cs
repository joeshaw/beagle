//
// SearchWindow.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

using Gtk;
using GtkSharp;

using Beagle;

namespace Best {

	public class BestWindow : Gtk.Window {

		static public void Create ()
		{
			new BestWindow ().ShowAll ();
		}

		//////////////////////////

		QueryDriver driver;

		private BestWindow () : base (WindowType.Toplevel)
		{
			Title = "Bleeding-Edge Search Tool";

			DeleteEvent += new DeleteEventHandler (this.DoDelete);

			Widget menus = CreateMenuBar ();
			Widget content = CreateContents ();

			VBox main = new VBox (false, 3);
			main.PackStart (menus, false, true, 3);
			main.PackStart (content, true, true, 3);
			Add (main);

			const double GOLDEN = 1.61803399;
			DefaultWidth = 300;
			DefaultHeight = (int) (DefaultWidth * GOLDEN);

			driver = new QueryDriver ();
			driver.AutoPopulateHack ();

			Best.IncRef ();
		}

		private void DoDelete (object o, DeleteEventArgs args)
		{
			Close ();
		}
		
		//////////////////////////

		private Widget CreateMenuBar ()
		{
			AccelGroup group = new AccelGroup ();
			AddAccelGroup (group);

			MenuBar bar = new MenuBar ();

			Menu fileMenu = new Menu ();
			MenuItem file = new MenuItem ("_File");
			file.Submenu = fileMenu;

			ImageMenuItem fileNew = new ImageMenuItem (Gtk.Stock.New, group);
			fileNew.Activated += new EventHandler (this.DoNew);
			fileMenu.Append (fileNew);
			
			ImageMenuItem fileClose = new ImageMenuItem (Gtk.Stock.Close, group);
			fileClose.Activated += new EventHandler (this.DoClose);
			fileMenu.Append (fileClose);

			bar.Append (file);

			bar.ShowAll ();
			return bar;
		}
		
		private void DoNew (object o, EventArgs args)
		{
			Create ();
		}

		private void DoClose (object o, EventArgs args)
		{
			Close ();
		}

		//////////////////////////

		private Gtk.Entry entry;
		private HitContainer hitContainer;

		private Widget CreateContents ()
		{
			HBox entryLine = new HBox (false, 3);
			
			entry = new Gtk.Entry ();
			entry.Activated += new EventHandler (this.DoSearch);
			entryLine.PackStart (entry, true, true, 3);

			Gtk.Button button = new Gtk.Button ("Search");
			button.Clicked += new EventHandler (this.DoSearch);
			entryLine.PackStart (button, false, false, 3);

			////////

			hitContainer = new HitContainer ();

			ScrolledWindow sw = new ScrolledWindow ();
			sw.AddWithViewport (hitContainer);

			////////

			VBox contents = new VBox (false, 3);
			contents.PackStart (entryLine, false, true, 3);
			contents.PackStart (sw, true, true, 3);
			return contents;
		}

		private void DoSearch (object o, EventArgs args)
		{
			Search (entry.Text);
		}

		//////////////////////////

		private void Close ()
		{
			Best.DecRef ();
			Destroy ();
		}

		//////////////////////////

		private void OnGotHits (object src, QueryResult.GotHitsArgs args)
		{
			Console.WriteLine ("Got {0} Hits!", args.Count);
			foreach (Hit hit in args.Hits)
				hitContainer.Add (hit);
		}

		private void OnFinished (object src)
		{
			Console.WriteLine ("Finished!");
			hitContainer.Close ();
		}

		private void Search (String searchString)
		{
			Query query = new Query (searchString);
			QueryResult result;

			result = driver.Query (query);
			result.GotHitsEvent += OnGotHits;
			result.FinishedEvent += OnFinished;

			hitContainer.Open ();						
			result.Start ();


			//foreach (Hit hit in result.Hits)
			//hitContainer.Add (hit);
			//hitContainer.Close ();

		}
	}
}
