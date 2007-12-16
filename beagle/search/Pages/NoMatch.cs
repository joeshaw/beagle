//
//  NoMatch.cs
//
//  Copyright (c) 2007 Novell, Inc.
//

using System;
using System.Text;
using System.Collections;

using Gtk;
using Mono.Unix;

namespace Search.Pages {

	public class NoMatch : Base {

		private static Gdk.Pixbuf icon = null;

		static NoMatch ()
		{
			icon = WidgetFu.LoadThemeIcon ("face-surprise", 48);
		}

		public NoMatch (string query, bool suggest_scope, ArrayList suggestions)
		{
			HeaderIcon = icon;
			Header = Catalog.GetString ("No results were found.");

			if (suggestions.Count != 0) {
				StringBuilder message = new StringBuilder ();
				message.Append (String.Format ("{0} ", Catalog.GetString ("Did you mean")));
				
				int count = Math.Min (3, suggestions.Count);
				
				for (int i = 0; i < count; i++) {
					message.Append (String.Format (" <b>{0}</b>", suggestions [i]));
					
					if (i < (count - 2))
						message.Append (", ");
					
					if (i == (count - 2))
						message.Append (String.Format (" {0}", Catalog.GetString ("or")));
				}
				
				message.Append (Catalog.GetString ("?"));
				
				Append (message.ToString ());
			}

			Append (String.Format (Catalog.GetString ("Your search for \"{0}\" did not match any files on your computer."), "<b>" + GLib.Markup.EscapeText (query) + "</b>"));

			if (suggest_scope)
				Append (Catalog.GetString ("You can change the scope of your search using the \"Search\" menu. A broader search scope might produce more results."));
			
			Append (Catalog.GetString ("You should check the spelling of your search words to see if you accidentally misspelled any words."));
		}
	}
}
