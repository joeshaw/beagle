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

	[HitFlavor (Name="Conversations", Emblem="emblem-im-log.png", Color="#e5f5ef",
		    Type="IMLog")]
	public class TileImLog : TileFromTemplate {

		Hit hit;

		public TileImLog (Hit _hit) : base ("template-im-log.html")
		{
			hit = _hit;
		}

		private string niceTime (string str)
		{
			DateTime dt = BU.StringFu.StringToDateTime (str);
			return dt.ToString ();
		}

		override protected string ExpandKey (string key)
		{
			if (key == "Uri")
				return hit.Uri.ToString ();
			if (key == "nice_starttime")
				return niceTime (hit ["fixme:starttime"]);
			if (key == "nice_endtime")
				return niceTime (hit ["fixme:endtime"]);

			return hit [key];
		}
	}
}
