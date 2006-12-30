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
 * $Log: Id3v23TagReader.cs,v $
 * Revision 1.1  2005/08/29 20:09:40  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.4  2005/02/13 17:22:17  kikidonk
 * Support for APIC
 *
 * Revision 1.3  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.Collections;
using System.Text;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Mp3.Util.Id3Frames;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v23TagReader {
		
		private Hashtable conversion;
		
		public Id3v23TagReader() {
			InitConversionTable();
		}
		
		public void Read(Id3Tag tag, ByteBuffer data, bool[] ID3Flags, byte version)
		{
			int tagSize = data.Limit;
			byte[] b;
			//----------------------------------------------------------------------------
			//Traitement en cas d'Header Etendu==true (A COMPLETER)
			if (version == Id3Tag.ID3V23 && ID3Flags[1])
				ProcessExtendedHeader(data);
			//----------------------------------------------------------------------------
			//Extraction des champs de texte
			int specSize = (version == Id3Tag.ID3V22) ? 3 : 4;
			for (int a = 0; a < tagSize; a++) {
				// Frame name
				b = new byte[specSize];
				
				if(data.Remaining <= specSize)
					break;

				data.Get(b);
				string field = Encoding.ASCII.GetString(b);
				if (b[0] == 0)
					break;

				//La longueur du texte contenu dans la Frame
				int frameSize = ReadInteger(data, version);

				// Ignore empty frames
				if ((frameSize > data.Remaining) || frameSize <= 0)
					break;

				//string field = Encoding.ASCII.GetString(b);
				if (field == "" || field.Length < 4)
					continue;
				if (field != "COMM" && (field[0] != 'T' || field[1] == 'X'))
					continue;

				b = new byte[ frameSize + ((version == Id3Tag.ID3V23) ? 2 : 0) ];
				data.Get(b);

				if(version == Id3Tag.ID3V22)
					field = ConvertFromId3v22(field);

				// FIXME: We only deal with text/comment frames right now
				if (field == "COMM") {
					TextId3Frame f = new CommId3Frame(b, version);
					tag.AddComment(f.Content);
				} else if (field[0] == 'T' && field[1] != 'X') {
					TextId3Frame f = new TextId3Frame(field, b, version);
					switch (field) {
					case "TCON": // genre
						tag.AddGenre(TranslateGenre(f.Content));
						break;

					case "TRCK": // track number
						string num, count;
						Utils.SplitTrackNumber(f.Content, out num, out count);
						if (num != null)
							tag.AddTrack(num);
						if (count != null)
							tag.AddTrackCount(count);
						break;

					default:
						tag.Add(field, f.Content);
						break;
					}
				}
			}
		}

		private string TranslateGenre(string content)
		{
			if (content == null || content.Length == 0)
				return null;

			// Written as "Name" ?
			if (content[0] != '(')
				return content;

			int pos = content.IndexOf(')');
			if (pos == -1)
				return content;

			// Written as "(id)" ?
			if (pos == content.Length - 1) {
				int num = Convert.ToInt32(content.Substring(1, content.Length - 2));
				return TagGenres.Get(num);
			}

			// Written as "(id)Name" ?
			return content.Substring(pos + 1);
		}

		private void InitConversionTable()
		{

			//TODO: APIC frame must update the mime-type to be converted ??
			//TODO: LINK frame (2.3) has a frame ID of 3-bytes making
			//      it incompatible with 2.3 frame ID of 4bytes, WTF???
			
			this.conversion = new Hashtable();
			string[] v22 = {
					"BUF", "CNT", "COM", "CRA",
					"CRM", "ETC", "EQU", "GEO",
					"IPL", "LNK", "MCI", "MLL",
					"PIC", "POP", "REV", "RVA",
					"SLT", "STC", "TAL", "TBP",
					"TCM", "TCO", "TCR", "TDA",
					"TDY", "TEN","TFT", "TIM",
					"TKE", "TLA", "TLE", "TMT",
					"TOA", "TOF", "TOL", "TOR",
					"TOT", "TP1", "TP2", "TP3",
					"TP4", "TPA", "TPB", "TRC",
					"TRD", "TRK", "TSI", "TSS",
					"TT1", "TT2", "TT3", "TXT",
					"TXX", "TYE", "UFI", "ULT",
					"WAF", "WAR", "WAS", "WCM",
					"WCP", "WPB", "WXX"
				};
			string[] v23 = {
					"RBUF", "PCNT", "COMM", "AENC",
					"", "ETCO", "EQUA", "GEOB",
					"IPLS", "LINK", "MCDI", "MLLT",
					"APIC", "POPM", "RVRB", "RVAD",
					"SYLT", "SYTC", "TALB", "TBPM",
					"TCOM", "TCON", "TCOP", "TDAT",
					"TDLY", "TENC", "TFLT", "TIME",
					"TKEY", "TLAN", "TLEN", "TMED",
					"TOPE", "TOFN", "TOLY", "TORY",
					"TOAL", "TPE1", "TPE2", "TPE3",
					"TPE4", "TPOS", "TPUB", "TSRC",
					"TRDA", "TRCK", "TSIZ", "TSSE",
					"TIT1", "TIT2", "TIT3", "TEXT",
					"TXXX", "TYER", "UFID", "USLT",
					"WOAF", "WOAR", "WOAS", "WCOM",
					"WCOP", "WPUB", "WXXX"			
				};
			
			for(int i = 0; i<v22.Length; i++)
				this.conversion[v22[i]] = v23[i];
		}
		
		private string ConvertFromId3v22(string field)
		{
			string s = this.conversion[field] as string;
			
			if(s == null)
				return "";
			
			return s;
		}

		//Process the Extended Header in the ID3v2 Tag, returns the number of bytes to skip
		private int ProcessExtendedHeader(ByteBuffer data)
		{
			//TODO Verify that we have an syncsfe int
			byte[] exthead = new byte [4];
			data.Get(exthead);
			int extsize = ReadInteger(data, Id3Tag.ID3V23);
			// The extended header size includes those first four bytes.
			data.Position = data.Position + extsize;
			return extsize;
		}

		private int ReadInteger(ByteBuffer bb, int version)
		{
			int value = 0;

			if(version == Id3Tag.ID3V23)
				value += (bb.Get()& 0xFF) << 24;
			value += (bb.Get()& 0xFF) << 16;
			value += (bb.Get()& 0xFF) << 8;
			value += (bb.Get()& 0xFF);

			return value;
		}
	}
}
