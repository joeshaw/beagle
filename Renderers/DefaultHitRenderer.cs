//
// GNOME Dashboard
//
// DefaultMatchRenderer.cs: The vanilla renderer for match types with
// no type-specific renderer to call their own.  Cold, lonely match
// types.
//
// Author:
//   Nat Friedman <nat@nat.org>
//

using System;
using System.Collections;

//[assembly:Dashboard.MatchRendererFactory ("Dashboard.DefaultMatchRenderer")]

namespace Beagle {

	class DefaultHitRenderer : HitRendererHtml {
		
		public DefaultHitRenderer ()
		{
			type = "Default";
		}

		protected override string HitsToHtml (ArrayList hits)
		{
			string html = "";

			foreach (Hit hit in hits)
				html += HTMLRenderSingleHit (hit);

			return html;
		}

		private string HTMLRenderSingleHit (Hit hit)
		{
			if (hit ["Icon"] == null && hit ["Text"] == null)
				return "";

			string html;

			html = String.Format (
					      "<table border=0 cellpadding=0 cellspacing=0>" +
					      "<tr>");

			if (hit ["Icon"] != null)
				html += String.Format (
						       "    <td valign=center>" +
						       + "        <a href=\"{0}\"><img src=\"{1}\" border=0></a>" +
						       "    </td>",
						       hit ["Action"],
						       hit ["Icon"]);

			html += String.Format ("<td>&nbsp;&nbsp;</td>" +
					       "    <td valign=top>" +
					       "        <a href=\"{0}\" style=\"text-decoration: none;\">{1}" +
					       "    </td>" +
					       "</tr>" +
					       "</table>",
					       hit ["Action"],
					       hit ["Text"]);

			return html;
		}
	}
}
