//
// GNOME Dashboard
//
// WebLinkMatchRenderer.cs: Knows how to render WebLink matches.
//
// Author:
//   Nat Friedman <nat@nat.org>
//   Kevin Godby <godbyk@yahoo.com>
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


using Gdk;
using System;
using System.IO;
using System.Xml;
using System.Collections;
using System.Reflection;

//[assembly:Dashboard.MatchRendererFactory ("Dashboard.WebLinkMatchRenderer")]

namespace Beagle {

	public class WebLinkHitRenderer : HitRendererHtml {

		public WebLinkHitRenderer ()
		{
			type = "WebLink";
		}

		protected override string HitsToHtml (ArrayList hits)
		{
			StringWriter sw = new StringWriter ();
			XmlWriter xw = new XmlTextWriter (sw);

			xw.WriteStartElement ("div");	// start WebLink results block

			xw.WriteStartElement ("table");	// WebLink header
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteStartElement ("tr");
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("bgcolor", "#fffa6e");
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "+2");
			xw.WriteString ("Web Sites");
			xw.WriteEndElement ();	// font
			xw.WriteEndElement ();	// td
			xw.WriteEndElement ();	// tr
			xw.WriteEndElement ();	// table

			xw.WriteStartElement ("table");	// Results table
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteAttributeString ("cellpadding", "0");
			xw.WriteAttributeString ("cellspacing", "0");

			// Sort results by score
			IComparer weblinkscorecomparer = new WebLinkScoreComparer ();
			hits.Sort (weblinkscorecomparer);

			bool color_band = true;
			foreach (Hit hit in hits) {
				HTMLRenderSingleWebLink (hit, color_band, xw);
				color_band = !color_band;
			}

			xw.WriteEndElement ();	// results table (table)
			xw.WriteEndElement ();	// end WebLink block (div)

			xw.Close ();

			//Console.WriteLine ("-- WebLink Renderer -----------------------------------------------");
			//Console.WriteLine (sw.ToString ());
			//Console.WriteLine ("---------------------------------------------------------------------\n\n");

			return sw.ToString ();
		}

		private void HTMLRenderSingleWebLink (Hit hit, bool color_band, XmlWriter xw)
		{
			string icon = CompositeEmblemIcon (Favicons.GetIconPath (hit.Uri));

			string Title = (string)hit ["Title"];
			string Score = Convert.ToString (hit.Score);
			if (Score == null || Score == "")
				Score = "n/a";

			xw.WriteStartElement ("tr");

			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("valign", "center");

			xw.WriteStartElement ("a");
			xw.WriteAttributeString ("href", hit.Uri);

			xw.WriteStartElement ("img");
			xw.WriteAttributeString ("src", icon);
			xw.WriteAttributeString ("border", "0");
			xw.WriteEndElement ();	// img

			xw.WriteEndElement ();	// a href

			xw.WriteEndElement ();	// td

			xw.WriteStartElement ("td");
			xw.WriteRaw ("&nbsp;");
			xw.WriteEndElement ();	// td

			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("valign", "top");

			xw.WriteRaw (Title);

			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "-2");
			xw.WriteAttributeString ("color", "#666666");
			xw.WriteString (" (score=" + Score + ")");
			xw.WriteEndElement ();	// font

			xw.WriteStartElement ("br");
			xw.WriteEndElement ();	// br

			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "-1");
			xw.WriteAttributeString ("color", "#666666");
			xw.WriteStartElement ("a");
			xw.WriteAttributeString ("href", hit.Uri);
			xw.WriteAttributeString ("style", "text-decoration: none;");
			xw.WriteString (hit.Uri);
			xw.WriteEndElement ();	// a
			xw.WriteEndElement ();	// font

			xw.WriteEndElement (); 	// td

			xw.WriteEndElement ();	// tr

		}

		private Stream GetImage (string name)
		{
			Assembly assembly = System.Reflection.Assembly.GetCallingAssembly ();
			System.IO.Stream s = assembly.GetManifestResourceStream (name);
			if (s == null)
				Console.WriteLine ("Can't find resource '{0}'", name);
			return s;
		}

		private string CompositeEmblemIcon (String emblem) 
		{
			if (emblem == null)
				return "internal:bookmark.png";
			
			if (! File.Exists (emblem)) {
				return "internal:bookmark.png";
  		} 

			// Composite an icon...
			Gdk.Pixbuf icon = new Pixbuf (emblem);
			Gdk.Pixbuf bookmark =
				new Pixbuf (GetImage ("bookmark.png"));
			Gdk.Pixbuf white =
				new Pixbuf (GetImage ("white.png"));

			white.Composite (bookmark,
			                 0,   0,   // dest x,y
			                 48, 20,   // height,width
			                 0,   0,   // offset x,y
			                 1,   1,   // scaling x,y
			                 Gdk.InterpType.Bilinear,
			                 127);     // Alpha

			// I just want to make the icon be 16x16.
			// This does it for me!
			Gdk.Pixbuf small_icon = icon.ScaleSimple (16, 16, // x,y
			                        Gdk.InterpType.Bilinear);

			small_icon.Composite(bookmark,
			                     0,   0,   // dest x,y
			                     48, 18,   // height,width
			                     31,  2,   // offset x,y
			                     1,   1,   // scaling x,y
			                     Gdk.InterpType.Bilinear,
			                     255);     // Alpha

			emblem = System.IO.Path.GetFileName (emblem);
			emblem = PathFinder.AppDataFileName ("transient:WebLinkHitRenderer",
							     "emblem-" + emblem);
			bookmark.Savev (emblem, "png", null, null);
			return emblem;
		}

		public enum SortDirection {
			Ascending,
			Descending
		}

		public class WebLinkScoreComparer : IComparer {
			
			// Reverse sort -- highest score first
			private SortDirection m_direction = SortDirection.Descending;

			int IComparer.Compare (Object x, Object y) {
				
				Hit MatchX = (Hit) x;
				Hit MatchY = (Hit) y;
				
				float ScoreX;
				float ScoreY;
				
				// These try..catch blocks are here in case we receive
				// a match without a Score property.  (Assume Score = 0)
				try {
					ScoreX = Single.Parse (Convert.ToString (MatchX ["Score"]));
				} catch	{
					ScoreX = 0;
				}
				
				try {
					ScoreY = Single.Parse (Convert.ToString (MatchY ["Score"]));
				} catch	{
					ScoreY = 0;
				}
				
				if (MatchX == null && MatchY == null) {
					return 0;
				} else if (MatchX == null && MatchY != null) {
					return (this.m_direction == SortDirection.Ascending) ? -1 : 1;
				} else if (MatchX != null && MatchY == null) {
					return (this.m_direction == SortDirection.Ascending) ? 1 : -1;
				} else {
					return (this.m_direction == SortDirection.Ascending) ? 
						ScoreX.CompareTo (ScoreY) :
						ScoreY.CompareTo (ScoreX);
				} // end if
			} // end IComparer.Compare
		} // end public class WebLinkScoreComparer
	}
}

