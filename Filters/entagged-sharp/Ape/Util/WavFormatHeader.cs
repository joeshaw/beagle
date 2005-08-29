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
 * Revision 1.1  2005/08/29 20:09:37  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

namespace Entagged.Audioformats.Ape.Util {
	public class WavFormatHeader {
		
		private bool isValid = false;
		
		private int channels,sampleRate,bytesPerSecond,bitrate;

		public WavFormatHeader( byte[] b ) {
			string fmt = new string(System.Text.Encoding.ASCII.GetChars(b,0,3));

			if(fmt == "fmt" && b[8]==1) {
				channels = b[10];

				sampleRate = u(b[15])*16777216 + u(b[14])*65536 + u(b[13])*256 + u(b[12]);

				bytesPerSecond = u(b[19])*16777216 + u(b[18])*65536 + u(b[17])*256 + u(b[16]);

				bitrate = u(b[22]);
				
				isValid = true;
			}
			
		}

		public bool Valid {
			get { return isValid; }
		}
		
		public int ChannelNumber {
			get { return channels; }
		}
		
		public int SamplingRate {
			get { return sampleRate; }
		}
		
		public int BytesPerSecond {
			get { return bytesPerSecond; }
		}
		
		public int Bitrate {
			get { return bitrate; }
		}

		private int u( int n ) {
			return n & 0xff ;
		}
		
		public override string ToString() {
			string s = "RIFF-WAVE Header:\n";
			s += "Is valid?: " + isValid;
			return s;
		}
	}
}
