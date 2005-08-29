/***************************************************************************
 *  Copyright 2005 Raphaël Slinckx <raphael@slinckx.net> 
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW: 
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),  
 *  to deal in the Software without restriction, including without limitation  
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,  
 *  and/or sell copies of the Software, and to permit persons to whom the  
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in 
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER 
 *  DEALINGS IN THE SOFTWARE.
 */

/*
 * $Log$
 * Revision 1.1  2005/08/29 20:09:43  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.Collections;
using System.Text;

namespace Entagged.Audioformats.Util {
	public class Utils {
		private static Encoding utf = Encoding.UTF8;
		public static string GetExtension(string f) {
			string name = f.ToLower ();
			int i = name.LastIndexOf( "." );
			if(i == -1)
				return "";
			
			return name.Substring( i + 1 );
		}
		
		public static byte[] GetUTF8Bytes(string s) {
			return utf.GetBytes(s);
		}
		
		public static long GetLongNumber(byte[] b, int start, int end) {
			long number = 0;
			for(int i = 0; i<(end-start+1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
		
		public static int GetNumber( byte[] b, int start, int end) {
			int number = 0;
			for(int i = 0; i<(end-start+1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}

		public static string[] FieldListToStringArray(IList taglist)
		{
			string[] ret = new string[taglist.Count];
			int i = 0;
			foreach (string field in taglist)
				ret[i++] = field;
			return ret;
		}

		public static int[] FieldListToIntArray(IList taglist)
		{
			int[] ret = new int[taglist.Count];
			int i = 0;
			foreach (string field in taglist)
				if (field.Length > 0)
					ret[i++] = Convert.ToInt32(field);
			return ret;
		}

		public static Tag CombineTags(params Tag[] tags)
		{
			Tag ret = new Tag();

			foreach (Tag tag in tags) {
				if (tag == null)
					continue;

				foreach (TagTextField artist in tag.Artist)
					ret.AddArtist (artist.Content);

				foreach (TagTextField album in tag.Album)
					ret.AddAlbum (album.Content);

				foreach (TagTextField title in tag.Title)
					ret.AddTitle (title.Content);

				foreach (TagTextField track in tag.Track)
					ret.AddTrack (track.Content);

				foreach (TagTextField trackcount in tag.TrackCount)
					ret.AddTrackCount (trackcount.Content);

				foreach (TagTextField year in tag.Year)
					ret.AddYear (year.Content);

				foreach (TagTextField comment in tag.Comment)
					ret.AddComment (comment.Content);

				foreach (TagTextField genre in tag.Genre)
					ret.AddGenre (genre.Content);
			}

			return ret;
		}

		// Splits (e.g.) "1/6" into track 1 of 6.
		public static void SplitTrackNumber(string content, out string num, out string count)
		{
			string[] split = content.Split(new char[] {'/'}, 2);
			if (split.Length == 1) {
				num = content;
				count = null;
			} else {
				num = split[0];
				count = split[1];
			}
		}
		
	}
}
