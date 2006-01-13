using Gtk;
using System;
using Mono.Unix;

namespace Search.Pages {

	public class QuickTips : Base {

		static string[] tips = new string[] {
			Catalog.GetString ("You can use upper and lower case; search is case-insensitive."),
			Catalog.GetString ("To search for optional terms, use OR.  ex: <b>George OR Ringo</b>"),
			Catalog.GetString ("To exclude search terms, use the minus symbol in front, such as <b>-cats</b>"),
			Catalog.GetString ("When searching for a phrase, add quotes. ex: <b>\"There be dragons\"</b>")
		};

		public QuickTips ()
		{
			HeaderIcon = Beagle.Images.GetPixbuf ("quick-tips.png");
			HeaderMarkup = "<big><b>" + Catalog.GetString ("Quick Tips") + "</b></big>";
			foreach (string tip in tips)
				Append (tip);
		}
	}
}
