//
// HitContainer.cs
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

	public class HitContainer : Gtk.VBox {

		bool open = false;

		HitRenderer renderer;
		HitRendererControl control;
		
		public HitContainer () : base (false, 1)
		{
			renderer = new HitRendererHtml ();

			control = new HitRendererControl ("Search Results", "flag-for-followup.png", renderer);

			Gtk.ScrolledWindow sw = new Gtk.ScrolledWindow ();
			sw.Add (renderer.Widget);

			this.PackStart (control, false, false, 3);
			this.PackStart (sw, true, true, 3);
			control.ShowAll ();
			sw.Show ();
		}

		// Call Open before adding any hits.
		public void Open ()
		{
			renderer.Clear ();
			open = true;
		}

		public void Add (Hit hit)
		{
			if (! open) {
				Console.WriteLine ("Adding Hit to closed HitContainer", hit.Uri);
				return;
			}

			renderer.Add (hit);
		}

		// Call Close when you are done adding hits.
		public void Close ()
		{ 
			open = false;
		}
	}
}
