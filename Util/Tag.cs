//
// Tag.cs : Uniform interface for all audio tag types. It handles the most common
//          fields used in music files.
//
// Author:
//		Raphaël Slinckx <raf.raf@wol.be>
//
// Copyright 2004 (C) Raphaël Slinckx
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
using System.Text;
using System.Collections;

namespace Beagle.Util.AudioUtil {

	public class Tag {
	
		protected Hashtable fields;

		public Tag ()
		{
			fields = new Hashtable ();
			fields["TITLE"] = "";
			fields["ALBUM"] = "";
			fields["ARTIST"] = "";
			fields["GENRE"] = "";
			fields["TRACK"] = "";
			fields["YEAR"] = "";
			fields["COMMENT"] = "";
			fields["VENDOR"] = "";
		}
		
		public string Title {
			get {
				return (string) fields["TITLE"];
			}
			set {
				if(value == null)
					fields["TITLE"] = "";
				fields["TITLE"] = value;
			}
		}
		
		public string Album {
			get {
				return (string) fields["ALBUM"];
			}
			set {
				if(value == null)
					fields["ALBUM"] = "";
				fields["ALBUM"] = value;
			}
			
		}
		
		public string Artist {
			get {
				return (string) fields["ARTIST"];
			}
			set {
				if(value == null)
					fields["ARTIST"] = "";
				fields["ARTIST"] = value;
			}
			
		}
		
		public string Genre {
			get {
				return (string) fields["GENRE"];
			}
			set {
				if(value == null)
					fields["GENRE"] = "";
				fields["GENRE"] = value;
			}
		}
		
		public string Track {
			get {
				return (string) fields["TRACK"];
			}
			set {
				if(value == null)
					fields["TRACK"] = "";
				fields["TRACK"] = value;
			}
		}
		
		public string Year {
			get {
				return (string) fields["YEAR"];
			}
			set {
				if(value == null)
					fields["YEAR"] = "";
				fields["YEAR"] = value;
			}
		}
		
		public string Comment {
			get {
				return (string) fields["COMMENT"];
			}
			set {
				if(value == null)
					fields["COMMENT"] = "";
				fields["COMMENT"] = value;
			}
		}
		
		public string Vendor {
			get {
				return (string) fields["VENDOR"];
			}
			set {
				if(value == null)
					fields["VENDOR"] = "";
				fields["VENDOR"] = value;
			}
		}
		
		public override string ToString ()
		{
			string s = "Tag content:\n";
			foreach (DictionaryEntry en in fields) {
				s += "\t";
				s += en.Key;
				s += " : ";
				s += en.Value;
				s += "\n";
			}
			return s.Substring (0, s.Length-1);
		}
		
		public void Merge (Tag tag)
		{
			if( Title.Trim () == "")
				Title = tag.Title;
			if( Artist.Trim () == "")
				Artist = tag.Artist;
			if( Album.Trim () == "")
				Album = tag.Album;
			if( Year.Trim () == "")
				Year = tag.Year;
			if( Comment.Trim () == "")
				Comment = tag.Comment;
			if( Track.Trim () == "")
				Track = tag.Track;
			if( Genre.Trim () == "")
				Genre = tag.Genre;
		}
	}
}
