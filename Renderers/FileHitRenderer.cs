//
// GNOME Dashboard
//
// FileMatchRenderer.cs: Knows how to render File matches.
//
// Author:
//   Kevin Godby <godbyk@yahoo.com>
//

using System;
using System.Collections;
using System.Xml;
using System.IO;

//[assembly:Dashboard.MatchRendererFactory ("Dashboard.FileMatchRenderer")]

namespace Beagle {

	public class FileHitRenderer : HitRendererHtml {

		public FileHitRenderer ()
		{
			type = "File";
		}

		protected override string HitsToHtml (ArrayList hits)
		{
			StringWriter sw = new StringWriter ();
			XmlWriter xw = new XmlTextWriter (sw);

			xw.WriteStartElement ("div");	// Start the File results block

			xw.WriteStartElement ("table");
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteStartElement ("tr");
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("bgcolor", "#fffa6e");
			xw.WriteAttributeString ("nowrap", "1");
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "+2");
			xw.WriteString ("Your Files");
			xw.WriteEndElement ();	// font
			xw.WriteEndElement ();	// td
			xw.WriteEndElement ();	// tr
			xw.WriteEndElement ();	// table

			xw.WriteStartElement ("table");	// Start the results table
			xw.WriteAttributeString ("border", "0");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteAttributeString ("cellpadding", "0");
			xw.WriteAttributeString ("cellspacing", "0");

			// Sort results by score
			IComparer filescorecomparer = new FileScoreComparer ();
			hits.Sort (filescorecomparer);
			
			bool color_band = true;
			foreach (Hit hit in hits) {
				HTMLRenderSingleFile (hit, color_band, xw);
				color_band = !color_band;
			}

			xw.WriteEndElement ();	// End results table
			xw.WriteEndElement ();	// End File results block

			xw.Close ();

			//Console.WriteLine ("-- File Renderer ----------------------------------------------------");
			//Console.WriteLine (sw.ToString ());
			//Console.WriteLine ("---------------------------------------------------------------------\n\n");

			return sw.ToString ();
		}

		private void HTMLRenderSingleFile (Hit hit, bool color_band, XmlWriter xw)
		{
			string Path    = hit.Uri.Substring (7);
			string Text    = Convert.ToString (System.IO.Path.GetFileName (Path));
			string Icon    = Beagle.Util.GnomeIconLookup.LookupMimeIcon (hit.MimeType, (Gtk.IconSize) 48);
			string Score   = Convert.ToString (hit ["Score"]);
			if (Score == null || Score == "")
				Score = "0";

			// Strip protocol from file
			// FIXME: Will we ever see a non-file:// protocol?
			string File = Path;
			if (File.StartsWith ("file://"))
				File = File.Substring (7);

			// Check to make sure the file exists
			FileInfo file = new FileInfo (File);
			if (!file.Exists) {
				Console.WriteLine ("Ack!  File does not exist: {0}", Path);
				return;
			}

			// DEBUG
			//Console.WriteLine ("File name: {0}", file.FullName);
			//Console.WriteLine ("Creation time: {0}", file.CreationTime);
			//Console.WriteLine ("Last Access time: {0}", file.LastAccessTime);
			//Console.WriteLine ("Last Write Time: {0}", file.LastWriteTime);
			//Console.WriteLine ("Size: {0}", file.Length);

			string LastModifiedDate = file.LastWriteTime.ToLongDateString ();

			xw.WriteStartElement ("tr");
			if (color_band)		// highlight every other row
				xw.WriteAttributeString ("bgcolor", "#eeeeee");	//originally #f6f2f6
			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteAttributeString ("align", "left");

			// Show the file's icon (and make it a link)
			xw.WriteStartElement ("a");	// link
			xw.WriteAttributeString ("href", Path);
			xw.WriteStartElement ("img");	// icon
			xw.WriteAttributeString ("src", Icon);
			xw.WriteAttributeString ("border", "0");
			xw.WriteEndElement ();	// img
			xw.WriteEndElement ();	// a
			xw.WriteEndElement ();	// td

			xw.WriteStartElement ("td");
			xw.WriteAttributeString ("valign", "top");
			xw.WriteAttributeString ("align", "left");
			xw.WriteAttributeString ("width", "100%");
			xw.WriteStartElement ("font");	// Make the font smaller to fit window width

			// Print the filename (w/ hyperlink)
			xw.WriteStartElement ("a");
			xw.WriteAttributeString ("href", /* "file://" + */ Path);
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("size", "+1");
			xw.WriteString (Text);
			xw.WriteEndElement (); // font
			xw.WriteEndElement ();	// a href
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("color", "#666666");
			xw.WriteString (" (" + Score + ")");
			xw.WriteEndElement ();	// font
			xw.WriteStartElement ("br");
			xw.WriteEndElement ();	// br

			// Print 'Last modified: date'
			xw.WriteStartElement ("font");
			xw.WriteAttributeString ("color", "#666666");
			xw.WriteString ("Last modified " + LastModifiedDate);
			xw.WriteEndElement ();	// font

			xw.WriteEndElement ();	// font
			xw.WriteEndElement ();	// td
			xw.WriteEndElement ();	// tr

		}

		public enum SortDirection {
			Ascending,
			Descending
		}

		public class FileScoreComparer : IComparer {

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
				} catch {
					ScoreX = 0;
				}

				try {
					ScoreY = Single.Parse (Convert.ToString (MatchY ["Score"]));
				} catch {
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

		} // end class FileScoreComparer

	}
}

