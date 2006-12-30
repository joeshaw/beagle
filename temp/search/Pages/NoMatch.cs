using Gtk;
using System;
using Mono.Unix;

namespace Search.Pages {

	public class NoMatch : Base {

		static Gdk.Pixbuf NoMatchIcon;

		static NoMatch ()
		{
			NoMatchIcon = Beagle.Images.GetPixbuf ("no-match.png");
		}

		public NoMatch (string query, bool suggestScope)
		{
			HeaderIcon = NoMatchIcon;
			HeaderMarkup = "<big><b>" + Catalog.GetString ("No results were found.") +
				"</b></big>\n\n" + String.Format (Catalog.GetString ("Your search for \"{0}\" did not match any files on your computer."),
								  "<b>" + GLib.Markup.EscapeText (query) + "</b>");

			if (suggestScope)
				Append (Catalog.GetString ("You can change the scope of your search using the \"Search\" menu. A broader search scope might produce more results."));

			Append (Catalog.GetString ("You should check the spelling of your search words to see if you accidentally misspelled any words."));
		}
	}
}
