using Gtk;
using System;
using System.Text;
using System.Collections;

using Mono.Unix;

namespace Search.Pages {

	public class NoMatch : Base {

		static Gdk.Pixbuf NoMatchIcon;

		static NoMatch ()
		{
			NoMatchIcon = Beagle.Images.GetPixbuf ("no-match.png");
		}

		public NoMatch (string query, bool suggestScope, ArrayList suggestions)
		{
			HeaderIcon = NoMatchIcon;
			HeaderMarkup = "<big><b>" + Catalog.GetString ("No results were found.") +
				"</b></big>\n\n" + String.Format (Catalog.GetString ("Your search for \"{0}\" did not match any files on your computer."),
								  "<b>" + GLib.Markup.EscapeText (query) + "</b>");

			if (suggestScope)
				Append (Catalog.GetString ("You can change the scope of your search using the \"Search\" menu. A broader search scope might produce more results."));

			Append (Catalog.GetString ("You should check the spelling of your search words to see if you accidentally misspelled any words."));

			if (suggestions.Count == 0)
				return;
			
			StringBuilder message = new StringBuilder ();
			message.Append (String.Format ("{0} ", Catalog.GetString ("Did you mean")));

			if (suggestions.Count == 1) {
				message.Append (String.Format ("<b>{0}</b>", (string)suggestions [0]));
			} else {
				bool first = true;
				for (int i = 0; i < suggestions.Count -1; i++) {
					if (!first)
						message.Append (", ");

					message.Append (String.Format ("<b>{0}</b>", (string)suggestions [i]));
					first = false;
				}
				
				message.Append (String.Format (" {0} <b>{1}</b>", Catalog.GetString ("or"), (string)suggestions [suggestions.Count - 1]));
			}

			message.Append (Catalog.GetString ("?"));
			Append (message.ToString ());
		}
	}
}
