//
// TileNote.cs
//
// Copyright (C) 2004 Christopher Orr
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
using System.Diagnostics;
using System.IO;

using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Notes", Rank=1000, Emblem="emblem-note.png", Color="#f5f5fe",
		    Type="Note")]
	public class TileNote : TileFromHitTemplate {
		public TileNote (Hit _hit) : base (_hit,
						   "template-note.html")
		{
		}

		[TileAction]
		public void OpenNote ()
		{
			string args = String.Format ("--open-note {0} --highlight-search \"{1}\"",
						     Hit.Uri, String.Join (" ", Query.Text));
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "tomboy";
			p.StartInfo.Arguments = args;

			try {
			    p.Start ();
			} catch (Exception e) {
			    Console.WriteLine ("Could not invoke Tomboy to open note: " + e);
			}
		}
	}
	
}
