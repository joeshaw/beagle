//
// TileImLog.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.IO;
using System.Collections;
using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Conversations", Rank=900, Emblem="emblem-im-log.png", Color="#e5f5ef",
		    Type="IMLog")]
	public class TileImLog : TileFromTemplate {

		Hit hit;
		BU.ImBuddy buddy = null;
		static BU.GaimBuddyListReader list = null;
		
		public TileImLog (Hit _hit) : base ("template-im-log.html")
		{
			if (list == null) {
				list = new BU.GaimBuddyListReader ();
			}

			hit = _hit;

			buddy = list.Search (hit ["fixme:speakingto"]);
		}

		private string HighlightOrNull (string haystack, string [] needles)
		{
			string [] highlight_start_list = {"<font color=red>",
							  "<font color=orange>",
							  "<font color=green>",
							  "<font color=blue>"};

			string highlight_end   = "</font>";

			string hili = haystack;
			bool dirty = false;
			int hicolor = -1;
			foreach (string needle in needles) {
				string h_up = hili.ToUpper ();
				string n_up = needle.ToUpper ();

				hicolor = (hicolor + 1) % 4;
				string highlight_start = highlight_start_list [hicolor];
				
				int ni = h_up.IndexOf (n_up);
				if (ni == -1)
					continue;

				while (ni != -1) {
					hili = hili.Insert (ni, highlight_start);
					hili = hili.Insert (ni + highlight_start.Length + needle.Length, highlight_end);

					h_up = hili.ToUpper ();
					dirty = true;

					ni = h_up.IndexOf (n_up, ni + highlight_start.Length + needle.Length + highlight_end.Length);
				}
			}

			if (dirty) {
				Console.WriteLine ("Hi: " + hili);
				return hili;
			}
			else
				return null;
		}

		private string getSnippet ()
		{
			ICollection logs = BU.GaimLog.ScanLog (new FileInfo (hit ["fixme:file"]));

			string snip = "";
			
			foreach (BU.ImLog log in logs) {
					foreach (BU.ImLog.Utterance utt in log.Utterances) {
						string s = HighlightOrNull (utt.Text, Query.Text);
						if (s != null) {
							if (snip != "")
								snip += " ... ";
							    
							snip += s;
						}
						
					}
			}

			return snip.Substring (0, System.Math.Min (256, snip.Length));
		}
			
		override protected string ExpandKey (string key)
		{
			if (key == "Uri")
				return hit.Uri.ToString ();
			if (key == "nice_starttime")
				return BU.StringFu.DateTimeToPrettyString (
					   BU.StringFu.StringToDateTime (hit ["fixme:starttime"]));
			if (key == "nice_duration")
				return BU.StringFu.DurationToPrettyString (
					   BU.StringFu.StringToDateTime (hit ["fixme:endtime"]),
					   BU.StringFu.StringToDateTime (hit ["fixme:starttime"]));
			if (key == "snippet")
				return getSnippet ();
			if (key == "fixme:speakingto") {
				if (buddy != null && buddy.Alias != "")
					return buddy.Alias;
				return hit ["fixme:speakingto"];
			}

			return hit [key];
		}
	}
}
