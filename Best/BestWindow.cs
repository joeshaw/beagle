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

		static public void Create ()
		{
			new BestWindow ().ShowAll ();
		}

		//////////////////////////

		QueryDriver driver;
		QueryResult result;

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
			DefaultHeight = 500;
			DefaultWidth = (int) (DefaultHeight * GOLDEN);

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

			hitContainer = new HitContainer ();

			VBox contents = new VBox (false, 3);
			contents.PackStart (entryLine, false, true, 3);
			contents.PackStart (hitContainer, true, true, 3);
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
			//Console.WriteLine ("Got {0} Hits!", args.Count);
			foreach (Hit hit in args.Hits)
				hitContainer.Add (hit);
		}

		private void OnFinished (QueryResult src)
		{
			if (src != result)
				return;
			//Console.WriteLine ("Finished!");
			hitContainer.Close ();
		}

		private void OnCancelled (QueryResult src)
		{
			if (src != result)
				return;
			//Console.WriteLine ("Cancelled!");
			hitContainer.Close ();
		}


		private void Search (String searchString)
		{
			Query query = new Query (searchString);

			if (result != null)
				result.Cancel ();

			result = driver.Query (query);

			result.GotHitsEvent += OnGotHits;
			result.FinishedEvent += OnFinished;
			result.CancelledEvent += OnCancelled;

			hitContainer.Open ();

			result.Start ();

		}
	}
}
