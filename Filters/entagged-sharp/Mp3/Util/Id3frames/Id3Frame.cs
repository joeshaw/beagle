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
 * Revision 1.1  2005/08/29 20:09:41  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.3  2005/02/08 12:54:40  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.Text;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3;

namespace Entagged.Audioformats.Mp3.Util.Id3Frames {
	public abstract class Id3Frame {
		protected byte[] flags;
		protected byte version;
		
		public Id3Frame() {
			this.version = Id3Tag.ID3V23;
			CreateDefaultFlags();
		}
		
		public Id3Frame(byte[] raw, byte version)
		{
		    byte[] rawNew;
			if(version == Id3Tag.ID3V23) {
				byte size = 2;
				
				if((raw[1]&0x80) == 0x80) {
					//Compression zlib, 4 bytes uncompressed size.
					size += 4;
				}
				
				if((raw[1]&0x80) == 0x40) {
					//Encryption method byte
					size += 1;
				}
				
				if((raw[1]&0x80) == 0x20) {
					//Group identity byte
					size += 1;
				}
				
				this.flags = new byte[size];
				for(int i = 0; i<size; i++)
					this.flags[i] = raw[i];
				rawNew = raw;
			} else {
				CreateDefaultFlags();
				rawNew = new byte[this.flags.Length + raw.Length];
				Copy(this.flags, rawNew, 0);
				Copy(raw, rawNew, this.flags.Length);
			}
			
			this.version = version;
			
			Populate(rawNew);
		}
		
		private void CreateDefaultFlags() {
			this.flags = new byte[2];
			this.flags[0] = 0;
			this.flags[1] = 0;
		}
		
		public byte[] Flags {
			get { return this.flags; }
		}
		
		public byte[] RawContent {
			get { return Build(); }
		}
		
		
		public abstract bool IsBinary { 
			set;
			get;
		}
		public abstract bool IsEmpty { 
			get;
		}
		
		public abstract string Id {
			get;
		}
		protected abstract void Populate(byte[] raw);
		protected abstract byte[] Build();
		public abstract bool IsCommon {
			get;
		}
		public abstract void CopyContent(TagField field);
		
		protected int IndexOfFirstNull(byte[] b, int offset) {
			for(int i = offset; i<b.Length; i++)
				if(b[i] == 0)
					return i;
			return -1;
		}
		
		protected void Copy(byte[] src, byte[] dst, int dstOffset) {
			for(int i = 0; i<src.Length; i++)
				dst[i+dstOffset] = src[i];
		}
		
		protected byte[] IdBytes {
			get { return Encoding.GetEncoding("ISO-8859-1").GetBytes(Id); }
		}

		protected byte[] GetSize(int size) {
			byte[] b = new byte[4];
			b[0] = (byte)( ( size >> 24 ) & 0xFF );
			b[1] = (byte)( ( size >> 16 ) & 0xFF );
			b[2] = (byte)( ( size >>  8 ) & 0xFF );
			b[3] = (byte)(   size         & 0xFF );
			return b;
		}
		
		protected string GetString(byte[]b, int offset, int length, string encoding) {
			return Encoding.GetEncoding(encoding).GetString(b, offset, length);
		}
	
		protected byte[] GetBytes(string s, string encoding) {
			return System.Text.Encoding.GetEncoding(encoding).GetBytes(s);
		}
	}
}
