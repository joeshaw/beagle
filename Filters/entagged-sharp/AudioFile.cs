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
 * Revision 1.5  2005/02/25 15:31:16  kikidonk
 * Big structure change
 *
 * Revision 1.4  2005/02/22 19:38:43  kikidonk
 * Adding missing encoding infos accessor methods
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using Entagged.Audioformats.Util;

namespace Entagged.Audioformats {

	public class AudioFile {
		
		private EncodingInfo info;
		private Tag tag;
		
		public AudioFile(EncodingInfo info, Tag tag) {
			this.info = info;
			this.tag = tag;
		}
		
		public AudioFile(EncodingInfo info) {
			this.info = info;
			this.tag = new Tag();
		}
		
		public int Bitrate {
			get { return info.Bitrate; }
		}

		public int ChannelNumber {
			get { return info.ChannelNumber; }
		}
		
		public string EncodingType {
			get { return info.EncodingType; }
		}

		public string ExtraEncodingInfos {
			get { return info.ExtraEncodingInfos; }
		}

		public int getSamplingRate {
			get { return info.SamplingRate; }
		}

		public int getLength {
			get { return info.Length; }
		}
		
		public bool IsVbr {
			get { return info.Vbr; }
		}
		
		public Tag Tag {
			get { return (tag == null) ? new Tag() : tag; }
		}
		
		public EncodingInfo EncodingInfo
		{
			get {
				return info;
			}
		}
		
		public override string ToString() {
			return "AudioFile --------\n" + 
				info.ToString() + "\n"+ 
				((tag == null) ? "" : 
				tag.ToString()) +
				"\n-------------------";
		}
	}
}
