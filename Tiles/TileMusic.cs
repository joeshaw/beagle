//
// TileMusic.cs
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
using System.Diagnostics;
using System.IO;

using BU = Beagle.Util;

namespace Beagle.Tile {

	[HitFlavor (Name="Music", Rank=400, Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="audio/x-mp3"),
	 HitFlavor (Name="Music", Rank=400, Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="audio/mpeg"),
	 HitFlavor (Name="Music", Rank=400, Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="application/ogg"),
	 HitFlavor (Name="Music", Rank=400, Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="application/x-flac"),
	 HitFlavor (Name="Music", Rank=400, Emblem="emblem-music.png", Color="#f5f5fe",
		    Type="File", MimeType="video/x-ms-asf")]

	public class TileMusic : TileFile {
		public TileMusic (Hit _hit) : base (_hit, 
						    "template-music.html")
		{
		}

		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();
			
			if (Hit ["fixme:title"] == null)
				Template ["Title"] = Hit ["fixme:splitname"];
			else
				Template ["Title"] = Hit ["fixme:title"];

			if (Hit ["fixme:artist"] == null)
				Template ["Artist"] = "Unknown Artist";
			else
				Template ["Artist"] = Hit ["fixme:artist"];
		}
		
		[TileAction]
		public void Enqueue ()
		{
			EnqueueMedia (Hit);
		}

		protected void EnqueueMedia(Hit hit)
		{
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "totem";
			p.StartInfo.Arguments = "--enqueue " + hit.PathQuoted;

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Error in EnqueueMedia: " + e);
			}
		}
	}
}
