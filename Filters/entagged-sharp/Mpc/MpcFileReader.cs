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
 * Revision 1.2  2005/12/11 23:52:14  dsd
 * 2005-12-11  Daniel Drake  <dsd@gentoo.org>
 *
 * 	* Filters/entagged-sharp: Resync. Includes some bugfixes and adds support
 * 	for ID3v2 v2.4, and ASF/WMA files.
 * 	* Filters/FilterMusic.cs: Register ASF/WMA mimetype.
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System.IO;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mpc.Util;
using Entagged.Audioformats.Ape.Util;

namespace Entagged.Audioformats.Mpc
{
	[SupportedMimeType ("audio/mpc")]
	[SupportedMimeType ("audio/x-mpc")]
	[SupportedMimeType ("audio/mp+")]
	[SupportedMimeType ("audio/x-mp+")]
	[SupportedMimeType ("entagged/mpc")]
	[SupportedMimeType ("entagged/mp+")]
	public class MpcFileReader : AudioFileReader 
	{	
		private MpcInfoReader ir = new MpcInfoReader();
		private ApeTagReader tr = new ApeTagReader();
		
		protected override EncodingInfo GetEncodingInfo(Stream raf, 
			string mime)  
		{
			return ir.Read(raf);
		}
		
		protected override Tag GetTag(Stream raf, string mime)  
		{
			return tr.Read(raf);
		}
	}
}

