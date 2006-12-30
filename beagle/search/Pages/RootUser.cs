using Gtk;
using System;
using Mono.Unix;

namespace Search.Pages {

	public class RootUser : Base {

		public RootUser ()
		{
			HeaderIconStock = Stock.DialogError;
			HeaderMarkup = "<big><b>" + Catalog.GetString ("Beagle cannot be run as root") + "</b></big>";
			Append (Catalog.GetString ("For security reasons, Beagle cannot be run as root.  You should restart as a regular user."));
		}
	}
}
