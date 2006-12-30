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

// Copyright (C) 2003 Nat Friedman
// Copyright (C) 2004 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
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
