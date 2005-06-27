//
// TileLauncher.cs
//
// Copyright (C) 2004 Novell, Inc.
// Copyright (C) 2004 Joe Gasiorek
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Globalization;

using Gnome;

using BU = Beagle.Util;

namespace Beagle.Tile {
	[HitFlavor (Name="Applications",Rank=800, Emblem="emblem-file.png", Color="#f5f5fe",
	    Type="File", MimeType="application/x-desktop")]
	public class TileLauncher : TileFromHitTemplate {
		
		Hit hit;
		
		public TileLauncher (Hit _hit) : base (_hit, "template-launcher.html")
		{
			hit = _hit;
		}

		[TileAction]
		public override void Open ()
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = true;
			p.StartInfo.FileName = hit ["fixme:Exec"];
			Console.WriteLine ("LAUNCHER: going to run {0}", hit ["fixme:Exec"]);
			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.StartInfo.FileName, e.Message);
			}
		}

		private string ImagePath ()
		{
			if (hit == null)
				return null;

			string path = hit ["fixme:Icon"];
			if (path == null)
				return null;
			if (path.StartsWith ("/")) {
				// don't worry about themes
				return path;
			} else {
				IconTheme icon_theme = new IconTheme ();
				int base_size;
				string icon_path;

				// Try GNOME Icons
				if (path.EndsWith (".png")) 
					icon_path = icon_theme.LookupIcon (path.Substring (0, path.Length-4), -1, IconData.Zero, out base_size);
				else
					icon_path = icon_theme.LookupIcon (path, -1, IconData.Zero, out base_size);

				if (icon_path != null)
					return icon_path;

				// Fall back to KDE icons
				return BU.KdeUtils.LookupIcon (path);
			}
		}

		// FIXME: This doesn't work for all locales
		static string locale = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
		static string name_key = String.Format ("fixme:Name[{0}]",locale);
		static string comment_key = String.Format ("fixme:Comment[{0}]",locale);

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();
			String icon = Images.GetHtmlSource (ImagePath (), Hit.MimeType);

			if (icon != null)
				Template["Icon"] = icon;
			else
				Template["Icon"] = Images.GetHtmlSource ("document", "image/png");

			string name = Hit [name_key];
			string comment = Hit [comment_key];

			if (name == null || name == "")
				name = Hit ["fixme:Name"];

			if (comment == null || comment == "")
				comment = Hit ["fixme:Comment"];
			
			Template ["Name"] = name;
			Template ["Comment"] = comment;
		}

	}		
}

