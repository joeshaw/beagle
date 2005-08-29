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
	public class CommId3Frame : TextId3Frame {
		
		private string shortDesc;
		private string lang;
		
		/*
		 * 0,1| frame flags
		 * 2| encoding
		 * 3,4,5| lang
		 * 6,..,0x00(0x00)| short descr
		 * x,..| actual comment
		 */
		
		public CommId3Frame(string content) : base("COMM", content) {
			this.shortDesc = "";
			this.lang = "eng";
		}
		
		public CommId3Frame(byte[] rawContent, byte version) : base("COMM", rawContent, version) {}
		
		public string Langage {
			get { return this.lang; }
		}
		
		protected override void Populate(byte[] raw)
		{
			this.encoding = raw[flags.Length];
			
			this.lang = System.Text.Encoding.GetEncoding("ISO-8859-1").GetString(raw, flags.Length+1, 3);
			
			this.shortDesc = GetString(raw, flags.Length+4, raw.Length - flags.Length - 4, Encoding);
			
			string[] s = this.shortDesc.Split('\0');
			this.shortDesc = s[0];
			
			this.content = "";
			if(s.Length >= 2)
				this.content = s[1];		
		}
		
		protected override byte[] Build()
		{
			string text = this.shortDesc + "\0" + this.content;
			byte[] data = GetBytes(text, Encoding);
			byte[] lan = System.Text.Encoding.GetEncoding("ISO-8859-1").GetBytes(this.lang);
			
			//the return byte[]
			byte[] b = new byte[4 + 4 + flags.Length + 1 + 3 + data.Length];
			
			int offset = 0;
			Copy(IdBytes, b, offset);        offset += 4;
			Copy(GetSize(b.Length-10), b, offset); offset += 4;
			Copy(flags, b, offset);               offset += flags.Length;
			
			b[offset] = this.encoding;	offset += 1;
			
			Copy(lan, b, offset);		offset += lan.Length;
			
			Copy(data, b, offset);
			
			return b;
		}
		
		public string ShortDescription {
			get { return shortDesc; }
		}
		
		public override bool IsEmpty {
		    get { return this.content == "" && this.shortDesc == ""; }
		}
		
		public override void CopyContent(TagField field)
		{
		    base.CopyContent(field);
		    if(field is CommId3Frame) {
		        this.shortDesc = (field as CommId3Frame).ShortDescription;
		        this.lang = (field as CommId3Frame).Langage;
		    }
		}
		
		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.Append("[").Append(Langage).Append("] ").Append("(").Append(ShortDescription).Append(") ").Append(Content);
			return sb.ToString();
		}
	}
}
