using Gtk;
using System;
using Mono.Unix;

namespace Search.Pages {

	public class NoMatch : Base {

		private static Gdk.Pixbuf icon = null;

		static NoMatch ()
		{
			icon = WidgetFu.LoadThemeIcon ("face-surprise", 48);
		}

		public NoMatch (string query, bool suggest_scope)
		{
			HeaderIcon = icon;
			Header = Catalog.GetString ("No results were found.");

			Append (String.Format (Catalog.GetString ("Your search for \"{0}\" did not match any files on your computer."), "<b>" + GLib.Markup.EscapeText (query) + "</b>"));

			if (suggest_scope)
				Append (Catalog.GetString ("You can change the scope of your search using the \"Search\" menu. A broader search scope might produce more results."));
			
			Append (Catalog.GetString ("You should check the spelling of your search words to see if you accidentally misspelled any words."));
		}
	}
}
