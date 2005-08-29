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
 * Revision 1.1  2005/08/29 20:09:40  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.5  2005/02/18 12:31:51  kikidonk
 * Adds a way to know if there was an id3 tag or not
 *
 * Revision 1.4  2005/02/08 12:54:42  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.IO;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Exceptions;
using Entagged.Audioformats.Mp3.Util;

namespace Entagged.Audioformats.Mp3 {
	[SupportedMimeType ("audio/x-mp3")]
	[SupportedMimeType ("audio/mpeg")]
	[SupportedMimeType ("entagged/mp3")]
	public class Mp3FileReader : AudioFileReader {
		
		private Mp3InfoReader ir = new Mp3InfoReader();
		private Id3v2TagReader idv2tr = new Id3v2TagReader();
		private Id3v1TagReader idv1tr = new Id3v1TagReader();
		
		protected override EncodingInfo GetEncodingInfo(Stream raf, string mime) {
			return ir.Read(raf);
		}
		
		protected override Tag GetTag(Stream raf, string mime)  {
			Id3Tag tag = new Id3Tag();	

			idv2tr.Read(tag, raf);
			idv1tr.Read(tag, raf);

			return tag;
		}
	}
}
