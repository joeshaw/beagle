//
// ContactHitRenderer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Text;

namespace Beagle {

	public class ContactHitRenderer : HitRendererHtml {

		public ContactHitRenderer ()
		{
			type = "Contact";
		}

		protected override String HitsToHtml (ArrayList hits)
		{
			StringBuilder builder = new StringBuilder ();

			builder.Append ("<table width=\"100%\" bgcolor=\"#fffa6e\"><tr><td>");
			builder.Append ("<font size=\"+2\">Contacts</font>");
			builder.Append ("</td></tr></table>");
			
			bool color_band = true;
			foreach (Hit hit in hits) {
				SingleHitToHtml (hit, color_band, builder);
				color_band = ! color_band;
			}
			return builder.ToString ();
		}

		private void AddField (Hit hit, String key, String name, StringBuilder builder)
		{
			if (hit [key] != null) {
				builder.Append ("<tr><td>" + name + ":</td>");
				builder.Append ("<td>" + hit [key] + "</td></tr>");
			}
		}
		
		private void SingleHitToHtml (Hit hit, bool color_band,
					      StringBuilder builder)
		{
			builder.Append ("<table");
			if (color_band)
				builder.Append (" bgcolor=\"#eeeeee\"");
			builder.Append (" width=\"100%\">");
			builder.Append ("<tr><td colspan=2>" + hit ["Name"] + "</td></tr>");
			AddField (hit, "Email1", "Email", builder);
			AddField (hit, "Email2", "Email", builder);
			AddField (hit, "Email3", "Email", builder);
			AddField (hit, "HomePhone", "Phone", builder);
			AddField (hit, "HomePhone2", "Phone2", builder);
			AddField (hit, "MobilePhone", "Mobile", builder);
			builder.Append ("</table>");
		}
	}
}
	
