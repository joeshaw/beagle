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
using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Conversations", Rank=900, Emblem="emblem-im-log.png", Color="#e5f5ef",
		    Type="IMLog")]
	public class TileImLog : TileFromTemplate {

		Hit hit;

		public TileImLog (Hit _hit) : base ("template-im-log.html")
		{
			hit = _hit;
		}

		private string niceTime (string str)
		{
			DateTime date = BU.StringFu.StringToDateTime (str);

			DateTime now = DateTime.Now;
			string short_time = date.ToShortTimeString ();

			if (date.Year == now.Year) {
				if (date.DayOfYear == now.DayOfYear)
					return String.Format ("Today, {0}", short_time);
				else if (date.DayOfYear == now.DayOfYear - 1)
					return String.Format ("Yesterday, {0}", short_time);
				else if (date.DayOfYear > now.DayOfYear - 6)
					return String.Format ("{0} days ago, {1}",
							      now.DayOfYear - date.DayOfYear,
							      short_time);
				else
					return date.ToString ("MMMM d, h:mm tt");
			}

			return date.ToString ("MMMM d yyyy, h:mm tt");
		}
		
		public string niceDuration (string end_str, string start_str)
		{
			DateTime end_time   = BU.StringFu.StringToDateTime (end_str);
			DateTime start_time = BU.StringFu.StringToDateTime (start_str);

			TimeSpan span = end_time - start_time;

			string span_str = ""; 

			if (span.Hours > 0) {
				if (span.Hours == 1)
					span_str = "1 hour";
				else
					span_str = String.Format ("{0} hours", span.Hours);

				if (span.Minutes > 0)
					span_str += ", ";
			}

			if (span.Minutes > 0) {
				if (span.Minutes == 1)
					span_str += "1 minute";
				else
					span_str += String.Format ("{0} minutes", span.Minutes);
			}
					
			
			return span_str;
		}


		override protected string ExpandKey (string key)
		{
			if (key == "Uri")
				return hit.Uri.ToString ();
			if (key == "nice_starttime")
				return niceTime (hit ["fixme:starttime"]);
			if (key == "nice_duration")
				return niceDuration (hit ["fixme:endtime"],
						     hit ["fixme:starttime"]);

			return hit [key];
		}
	}
}
