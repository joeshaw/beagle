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
using System.Text;
using System.Collections;

using Beagle.Util;

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
			StringBuilder sb;
			ICollection content = null;
			string strTemp;
			
			content = Hit["fixme:title"];
			Template["Title"] = Hit.GetValueAsString ("fixme:splitname");
			Template["Subtitles"] = "abc";
			int i = 0;
			sb = new StringBuilder();
			foreach (string str in content) {
				//  Use the first title in the list as Title of the tile
				if ((i == 0) && (str.Trim().Length > 0))
					Template["Title"] = str;
				else {
					sb.Append (str);
					sb.Append (" - ");
				}
				i++;
			}
			if (sb.Length >= 3) {
				sb.Length = sb.Length -3;
				sb.Append (".");
			}
			if (sb.Length > 1)
				Template["Subtitles"] = sb.ToString();

			//Setup the year
			content = Hit["fixme:year"];
			Template["Year"] = " ";
			if (content.Count > 0) {
				strTemp = StringFu.GetListValueAsString (content, "(", ")", ',');
				if (strTemp.Length > 3)
					Template ["Year"] = strTemp;
			}
			
			//Setup the track nb
			content = Hit["fixme:tracknumber"];
			bool hasTrack = false;
			Template["Track"] = " ";
			if (content.Count == 0)
				hasTrack = false;
			else {
				strTemp = StringFu.GetListValueAsString (content, "Track ", "", ',');
				if (strTemp.Length > ("Track "+" ").Length)
					Template["Track"] = strTemp;
				hasTrack = true;	
			}

			//Setup the Album
			content = Hit["fixme:album"];
			Template["Album"] = "Unknown Album";
			if (content.Count > 0) {
				string strOn; 
				strOn = hasTrack ? "on " : "On ";
				strTemp = StringFu.GetListValueAsString (content, strOn, "", ',');
				if (strTemp.Length > (strOn+" ").Length)
					Template["Album"] = strTemp;
			}
			
			
			//Setup artists
			content = Hit["fixme:artist"];
			Template["Artists"] = "Unknown Artist";
			if (content.Count > 0) {
				strTemp = StringFu.GetListValueAsString (content, "", "", ',');
				if (strTemp.Length > 1)
					Template["Artists"] = strTemp;
			}
			
			//Setup genre
			content = Hit["fixme:genre"];
			Template["Genre"] = " ";
			if (content.Count > 0) {
				strTemp = StringFu.GetListValueAsString (content, "Filed under ", ".", ',');
				if (strTemp.Length > ("Filed under "+"."+" ").Length)
					Template["Genre"] = strTemp;
			}
		}
		
		[TileAction]
		public void Enqueue ()
		{
			EnqueueMedia (Hit);
		}

		protected void EnqueueMedia (Hit hit)
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
