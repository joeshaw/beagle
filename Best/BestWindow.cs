//
// SearchWindow.cs
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

using Gtk;
using GtkSharp;

using Beagle;

namespace Best {

	public class BestWindow : Gtk.Window {

		static public void Create (string queryStr)
		{
			BestWindow best = new BestWindow ();
			if (queryStr != null)
				best.Search (queryStr);
			best.Show ();
		}

		static public void Create ()
		{
			Create (null);
		}

		//////////////////////////

		QueryDriver driver;
		QueryResult result;

		private BestWindow () : base (WindowType.Toplevel)
		{
			Title = "Bleeding-Edge Search Tool";

			DeleteEvent += new DeleteEventHandler (this.DoDelete);

			//Widget menus = CreateMenuBar ();
			Widget content = CreateContents ();

			VBox main = new VBox (false, 3);
			//main.PackStart (menus, false, true, 3);
			main.PackStart (content, true, true, 3);
			//menus.Show ();
			content.Show ();
			Add (main);
			main.Show ();

			const double GOLDEN = 1.61803399;
			DefaultHeight = 500;
			DefaultWidth = (int) (DefaultHeight * GOLDEN);

			driver = new QueryDriver ();

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
		
		private Gtk.ScrolledWindow swin;
		private TileCanvas canvas;
		private BestRootTile root;

		private Gtk.Widget CreateContents ()
		{
			Gtk.HBox entryLine = new HBox (false, 3);

			Gtk.Label words = new Gtk.Label ("Enter search terms:");
			entryLine.PackStart (words, false, false, 3);
			
			entry = new Gtk.Entry ();
			entry.Activated += new EventHandler (this.DoSearch);
			entryLine.PackStart (entry, true, true, 3);

			
			Gtk.HBox buttonContents = new HBox (false, 0);
			Gtk.Widget buttonImg = Images.GetWidget ("icon-search.png");
			buttonContents.PackStart (buttonImg, false, false, 1);
			Gtk.Label buttonLabel = new Gtk.Label ("Find");
			buttonContents.PackStart (buttonLabel, false, false, 1);
			
			Gtk.Button button = new Gtk.Button ();
			button.Add (buttonContents);
			button.Clicked += new EventHandler (this.DoSearch);
			entryLine.PackStart (button, false, false, 3);

			canvas = new TileCanvas ();
			canvas.PreRenderEvent += new EventHandler (OnPreRender);
			canvas.PostRenderEvent += new EventHandler (OnPostRender);

			root = new BestRootTile ();
			canvas.Root = root;

			swin = new Gtk.ScrolledWindow ();
			swin.Add (canvas);

			VBox contents = new VBox (false, 3);
			contents.PackStart (entryLine, false, true, 3);
			contents.PackStart (swin, true, true, 3);

			entryLine.ShowAll ();
			swin.ShowAll ();

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

		private void OnGotHits (QueryResult src, QueryResult.GotHitsArgs args)
		{
			if (src != result)
				return;
			Console.WriteLine ("Got {0} Hits!", args.Count);
			foreach (Hit hit in args.Hits)
				root.Add (hit);
		}

		private void OnFinished (QueryResult src)
		{
			if (src != result)
				return;
			Console.WriteLine ("Finished!");
			root.Close ();
		}

		private void OnCancelled (QueryResult src)
		{
			if (src != result)
				return;
			Console.WriteLine ("Cancelled!");
			root.Close ();
		}

		private void OnPreRender (object obj, EventArgs args)
		{
			if (swin == null)
				return;
			Gtk.Adjustment adj = swin.Vadjustment;
			if (adj == null)
				return;
			// FIXME!
		}
		
		private void OnPostRender (object obj, EventArgs args)
		{
			if (swin == null)
				return;
			Gtk.Adjustment adj = swin.Vadjustment;
			if (adj == null)
				return;
			// FIXME!
		}

		private void Search (String searchString)
		{
			entry.Text = searchString;
			Query query = new Query (searchString);

			if (result != null)
				result.Cancel ();

			result = driver.Query (query);

			result.GotHitsEvent += OnGotHits;
			result.FinishedEvent += OnFinished;
			result.CancelledEvent += OnCancelled;

			root.Open ();

			result.Start ();

		}
	}
}
