//
// HitRendererControl.cs
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

namespace Beagle {

	public class HitRendererControl : Gtk.HBox {

		string name;
		HitRenderer renderer;

		Gtk.Label nameLabel;
		Gtk.Label displayedLabel;

		Gtk.Button prevButton;
		Gtk.Button nextButton;

		public HitRendererControl (string _name, string icon, HitRenderer r) 
			: base (false, 3)
		{
			name = _name;
			renderer = r;

			renderer.RefreshEvent += new HitRenderer.RefreshHandler (OnRefresh);
			
			if (icon != null) {
				Gtk.Widget iconW = DataBarn.GetImageWidget (icon);
				if (iconW != null)
					this.PackStart (iconW, false, false, 3);
			}

			nameLabel = new Gtk.Label ("");
			this.PackStart (nameLabel, false, false, 3);


			nextButton = new Gtk.Button (">>");
			nextButton.Clicked += new EventHandler (OnNextClicked);
			this.PackEnd (nextButton, false, false, 3);			

			displayedLabel = new Gtk.Label ("");
			this.PackEnd (displayedLabel, false, false, 3);

			prevButton = new Gtk.Button ("<<");
			prevButton.Clicked += new EventHandler (OnPrevClicked);
			this.PackEnd (prevButton, false, false, 3);			



			// Initialize things
			OnRefresh (null);
		}

		private void OnRefresh (HitRenderer signaller)
		{
			string str;
			
			str = "<b>" + name + "</b>";
			if (renderer.TotalCount > 0) {
				str += " -- " + renderer.TotalCount + " match";
				if (renderer.TotalCount > 1)
					str += "es";
			}
			nameLabel.Markup = str;


			str = "";
			if (renderer.DisplayedCount > 1) {
				str = String.Format ("{0} - {1} displayed",
						     renderer.FirstDisplayed + 1,
						     renderer.LastDisplayed + 1);
			}
			displayedLabel.Text = str;
		}

		private void OnPrevClicked (object o, EventArgs args)
		{
			renderer.DisplayPrev ();
		}

		private void OnNextClicked (object o, EventArgs args)
		{
			renderer.DisplayNext ();
		}
		
	}
}
