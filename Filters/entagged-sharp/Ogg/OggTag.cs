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
 * Revision 1.1  2005/08/29 20:09:42  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.3  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using System.Text;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Ogg.Util;

namespace Entagged.Audioformats.Ogg {
	public class OggTag : Tag {

		public OggTag()
		{
			AddCommonFieldMapping(CommonField.Comment, "DESCRIPTION");
			AddCommonFieldMapping(CommonField.Track, "TRACKNUMBER");
			AddCommonFieldMapping(CommonField.TrackCount, "TRACKTOTAL");
			AddCommonFieldMapping(CommonField.Year, "DATE");
		}

		public void AddOggField(byte [] raw)
		{
			string field = new string(Encoding.UTF8.GetChars(raw));
			string[] splitField = field.Split('=');
			if (splitField.Length > 1)
				Add(splitField[0], splitField[1]);
			else if (field.IndexOf('=') == -1)
				Add("ERRONEOUS", field);
		}

	}
}
