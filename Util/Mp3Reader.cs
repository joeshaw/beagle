//
// Mp3Reader.cs : Reads an Id3v1.0,1.1,2.2,2.3 tag from the given stream
//
// Author:
//		Raphaël Slinckx <raf.raf@wol.be>
//
// Copyright 2004 (C) Raphaël Slinckx (ported from http://entagged.sourceforge.net)
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

	namespace Mp3 {
	
		public class Id3v2TagSynchronizer {
		
		    public int synchronize (byte[] b)
		    {
		        int newPtr = 0;
		        int oldPtr = 0;
		    
		        byte cur;    
		        while (oldPtr < b.Length && newPtr < b.Length) {
		            cur = b[oldPtr++];
		            b[newPtr++] = cur;
		            if (((cur&0xFF) == 0xFF) && (oldPtr < b.Length)) { //First part of synchronization
		                cur = b[oldPtr++];
			            if (cur != 0x00) { //If different, we have not a synchronization, so we take this value
			            	b[newPtr++] = cur;
			            }
		            }
		        }
		        
		        //We have finished, retun the length of the new data.
		        return newPtr;
		    }
		}
		
		public class Id3v23TagReader {
		
			public static int ID3V22 = 0;
			public static int ID3V23 = 1;
			
			private Hashtable conversion;
			private ASCIIEncoding ascii = new ASCIIEncoding ();
			private UTF8Encoding utf = new UTF8Encoding ();
			
			public Id3v23TagReader ()
			{
				this.conversion = initConversionTable ();
			}
			
			public Tag Read (byte[] data, int tagSize, byte flags, int version)
			{
				Tag tag = new Tag ();
				
				int elapsedBytes = 0;
				
				bool extended =  ((flags&64) == 64) ? true : false;
				//--------------------------------------------------------------
				if (version == ID3V23 && extended)
					elapsedBytes = processExtendedHeader (data);
				//--------------------------------------------------------------
				Hashtable ht = new Hashtable (10);
				
				int specSize = (version == ID3V22) ? 3 : 4;
				for (int a = 0; a < tagSize; a+=elapsedBytes) {
					string field = ascii.GetString (data, elapsedBytes, specSize);
					
					if (data[elapsedBytes] == 0)
						break;
					elapsedBytes += specSize;
					
					//-------------------------------------
					int frameSize = ReadInteger (data, elapsedBytes, version);
					elapsedBytes += specSize;
					
					if (version == ID3V23) {
						//Frame flags, skipping
						elapsedBytes += 2;
					}
					
					if ((frameSize + elapsedBytes) > tagSize || frameSize <= 0){
						throw new Exception ("Frame size error, skipping file");
					}
					else { 
						if (version == ID3V22) {
							//We try to translte the tag from a 3 letters
							//v22 format to a 4 letter v23-4 format
							field = convertFromId3v22 (field);
							//if the conversion do not succeed,
							//then the frame is lost
						} 

						if (field != "") {
							byte[] content = new byte[frameSize];
							
							for (int i = 0; i<content.Length; i++)
								content[i] = data[i+elapsedBytes];
								
							ht[field] = toSimplestring (field, content);
						}
						elapsedBytes += frameSize;
					}
				}
				//--------------------------------------------------------------

				foreach (DictionaryEntry en in ht) {
					string field = (string) en.Key;
					string content = (string) en.Value;
					
					if (field == "TIT2")
						tag.Title = content;
					else if (field == "TPE1")
						tag.Artist = content;
					else if (field == "TALB")
						tag.Album = content;
					else if (field == "TYER")
						tag.Year = content;
					else if (field == "COMM") {
						tag.Comment = content.Substring (4);
					} else if (field == "TRCK")
						tag.Track = content;
					else if (field == "TCON")
						tag.Genre = content;
				}
				
				return tag;
			}
			
			private Hashtable initConversionTable ()
			{
				Hashtable ht = new Hashtable (64, 1.0f);
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
				
				for (int i = 0; i<v22.Length; i++)
					ht[v22[i]] = v23[i];
					
				return ht;
			}
			
			private string convertFromId3v22 (string field)
			{
				string s = (string) this.conversion[field];
				
				if (s == null)
					return "";
				
				return s;
			}

			//returns the number of bytes to skip, to skip the extended data
			private int processExtendedHeader (byte[] data)
			{
				int extsize = ReadInteger (data,0, ID3V23);
				// The extended header size includes those first four bytes.
				return extsize -4;
			}

			private string toSimplestring (string field, byte[] content)
			{
				//-----------HERE FRAME SPECIFICITIES WILL BE HANDLED !! -----
				//Text frames
				if (field.StartsWith ("T") && !field.StartsWith ("TX")) {
					//Structure:
					//Encoding|string| (Termination)
					
					//Are we null terminated ? x00 or x0000 if ISO or UTF
					int idx = indexOfFirstNull (content, 1);
					int length;
					if (idx != -1)
						length = idx-1;
					else
						length = content.Length-1;
								
					if (content[0] == 1) {
						//UTF-8 encoding
						return utf.GetString (content, 1, length);
					} 
					
					//if content[0] == 0 , we use ISO-8859-1, if not, this is a default. 
					return ascii.GetString (content, 1, length);
				}
				else if (field.StartsWith ("COMM") || field.StartsWith ("WX")
						|| field.StartsWith ("IPLS") || field.StartsWith ("USLT")
						|| field.StartsWith ("TX") || field.StartsWith ("USER")) {
					//Structure:
					//Encoding|string
					
					
					//We get here the custom TXXX and WXXX frames, same constitution
					int length = content.Length - 1;
					
					if (content[0] == 1) {
						//UTF-8 encoding
						return utf.GetString (content, 1, length);
					}
					
					//if content[0] == 0 , we use ISO-8859-1, if not, this is a default.
					return ascii.GetString (content, 1, length);
				}
				else if (field.StartsWith ("UFID") || field.StartsWith ("MCDI") 
						|| field.StartsWith ("ETCO") || field.StartsWith ("MLLT")
						|| field.StartsWith ("SYTC") || field.StartsWith ("RVAD")
						|| field.StartsWith ("EQUA") || field.StartsWith ("RVRB")
						|| field.StartsWith ("APIC") || field.StartsWith ("GEOB")
						|| field.StartsWith ("SYLT") || field.StartsWith ("PCNT")
						|| field.StartsWith ("POPM") || field.StartsWith ("RBUF")
						|| field.StartsWith ("AENC") || field.StartsWith ("LINK")
						|| field.StartsWith ("POSS") || field.StartsWith ("OWNE")
						|| field.StartsWith ("COMR") || field.StartsWith ("ENCR")
						|| field.StartsWith ("GRID") || field.StartsWith ("PRIV")) {
					//Structure:
					//|string|	aka Binary data
					
					//WARNING APIC, GEOB, OWNE, SYLT, COMR use a mix of ISO and UTF encoding
					//They are saved as binary data and, one to further process the contained data
					//has to trasform the string o bytes (using iso) then parse appropriately !
					
					return ascii.GetString (content);
				}
				else if (field.StartsWith ("W") && !field.StartsWith ("WX")) {
					//Structure:
					//|string| (Termination)
					
					int idx = indexOfFirstNull (content, 1);
					int length;
					if (idx != -1)
						length = idx-1;
					else
						length = content.Length-1;
								
					return ascii.GetString (content, 1, length);
				}
				else {
					//We get here for all the frame that are not supported by the ID3 document
					//We simply save the content as binary data !
					Console.Error.Write ("Unknown Tag frame: " + field);
					return ascii.GetString (content);
				}
			}
			
			private int indexOfFirstNull (byte[] b, int offset)
			{
				for (int i = offset; i<b.Length; i++)
					if (b[i] == 0)
						return i;
				return -1;
			}
			
			private int ReadInteger (byte[] bb, int offset, int version) 
			{
				int value = 0;

				if (version == ID3V23)
					value += (bb[offset]& 0xFF) << 24;
				value += (bb[offset+1]& 0xFF) << 16;
				value += (bb[offset+2]& 0xFF) << 8;
				value += (bb[offset+3]& 0xFF);

				return value;
			}
		}
	}

	public class Id3v1TagReader {
	
		public static string[] ID3V1_GENRES = new string[] {"Blues", "Classic Rock", "Country", "Dance", "Disco", "Funk", "Grunge", "Hip-Hop", "Jazz",
			"Metal", "New Age", "Oldies", "Other", "Pop", "R&B", "Rap", "Reggae", "Rock", "Techno", "Industrial", "Alternative",
			"Ska", "Death Metal", "Pranks", "Soundtrack", "Euro-Techno", "Ambient", "Trip-Hop", "Vocal", "Jazz+Funk", "Fusion",
			"Trance", "Classical", "Instrumental", "Acid", "House", "Game", "Sound Clip", "Gospel", "Noise", "AlternRock",
			"Bass", "Soul", "Punk", "Space", "Meditative", "Instrumental Pop", "Instrumental Rock", "Ethnic", "Gothic",
			"Darkwave", "Techno-Industrial", "Electronic", "Pop-Folk", "Eurodance", "Dream", "Southern Rock", "Comedy",
			"Cult", "Gangsta", "Top 40", "Christian Rap", "Pop/Funk", "Jungle", "Native American", "Cabaret", "New Wave",
			"Psychadelic", "Rave", "Showtunes", "Trailer", "Lo-Fi", "Tribal", "Acid Punk", "Acid Jazz", "Polka", "Retro",
			"Musical", "Rock & Roll", "Hard Rock", "Folk", "Folk-Rock", "National Folk", "Swing", "Fast Fusion", "Bebob", "Latin", "Revival",
			"Celtic", "Bluegrass", "Avantgarde", "Gothic Rock", "Progressive Rock", "Psychedelic Rock", "Symphonic Rock", "Slow Rock",
			"Big Band", "Chorus", "Easy Listening", "Acoustic", "Humour", "Speech", "Chanson", "Opera", "Chamber Music", "Sonata",
			"Symphony", "Booty Bass", "Primus", "Porn Groove", "Satire", "Slow Jam", "Club", "Tango", "Samba", "Folklore", "Ballad",
			"Power Ballad", "Rhythmic Soul", "Freestyle", "Duet", "Punk Rock", "Drum Solo", "A capella", "Euro-House", "Dance Hall"};
			
		public Tag Read (FileStream fs)
		{
			byte[] buf = new byte[30];
			ASCIIEncoding ascii = new ASCIIEncoding ();
			Tag tag = new Tag ();
		
			//Check wether the file contains an Id3v1 tag--------------------------------
			fs.Seek (-128, SeekOrigin.End);
			
			fs.Read (buf, 0, 3);
			fs.Seek (0, SeekOrigin.Begin);
			
			string tagS = ascii.GetString (buf, 0, 3);
			if (tagS != "TAG"){
				throw new Exception ("There is no Id3v1 Tag in this file");
			}
			//Parse the tag -)------------------------------------------------
			fs.Seek (-128 + 3, SeekOrigin.End);
			fs.Read (buf, 0, 30);
			string songName = ascii.GetString (buf, 0, 30);
			songName = songName.Trim ();
			//------------------------------------------------
			fs.Read (buf, 0, 30);
			string artist = ascii.GetString (buf, 0, 30);
			artist = artist.Trim ();
			//------------------------------------------------
			fs.Read (buf, 0, 30);
			string album = ascii.GetString (buf, 0, 30);
			album = album.Trim ();
			//------------------------------------------------
			fs.Read (buf, 0, 4);
			string year = ascii.GetString (buf, 0, 4);
			year = year.Trim ();
			//------------------------------------------------
			fs.Read (buf, 0, 30);

			string trackNumber = "";
			string comment;

			if (buf[28] == 0) {
				trackNumber = buf[29].ToString ();

				comment = ascii.GetString (buf, 0, 28);
				comment = comment.Trim ();
			}
			else {
				comment = ascii.GetString (buf, 0, 30);
				comment = comment.Trim ();
			}
			//------------------------------------------------
			fs.Read (buf, 0, 1);
			byte genreByte = buf[0];

			tag.Title = songName;
			tag.Artist = artist;
			tag.Album = album;
			tag.Year = year;
			tag.Comment = comment;
			tag.Track = trackNumber;
			tag.Genre = TranslateGenre (genreByte);
			
			fs.Seek (0, SeekOrigin.Begin);
			
			return tag;
		}
		
		private string TranslateGenre (byte b)
		{
			int i = b & 0xFF;

			if (i == 255 || i > ID3V1_GENRES.Length - 1)
				return "";
				
			return ID3V1_GENRES[i];
		}
	}
	
	public class Id3v2TagReader {
	
		private Mp3.Id3v2TagSynchronizer synchronizer = new Mp3.Id3v2TagSynchronizer ();
		private Mp3.Id3v23TagReader v23 = new Mp3.Id3v23TagReader ();
		private ASCIIEncoding ascii = new ASCIIEncoding ();
		
		public Tag Read (Stream fs)
		{
			Tag tag = null;
			
			fs.Seek (0, SeekOrigin.Begin);
			byte[] buf = new byte[3];
			fs.Read (buf, 0, 3);
			
			string ID3 = ascii.GetString (buf);
			if (ID3 != "ID3") {
				throw new Exception ("There is no Id3v2 Tag in this file");
			}
			//Begins tag parsing ---------------------------------------------
			fs.Seek (3, SeekOrigin.Begin);
			//----------------------------------------------------------------------------
			string versionHigh = fs.ReadByte () + "";
			string versionID3 = versionHigh + "." + fs.ReadByte ();
			//------------------------------------------------------------------------- ---
			byte flags = (byte) fs.ReadByte (); //ID3 Flags, skipping
			//----------------------------------------------------------------------------
			int tagSize = ReadSyncsafeInt (fs);
			//-----------------------------------------------------------------
			//Fill a byte buffer, then process according to correct version
			buf = new byte[tagSize+2];
			fs.Read (buf, 0, buf.Length);
			
			bool unsynch = ((flags&128) == 128) ? true : false;
			if (unsynch) {
			    //We have unsynchronization, first re-synchronize
			    tagSize = synchronizer.synchronize (buf);
			}
			
			if (versionHigh == "2") {
				tag = v23.Read (buf, tagSize, flags, Mp3.Id3v23TagReader.ID3V22);
			}
			else if (versionHigh == "3") {
			    tag = v23.Read (buf, tagSize, flags, Mp3.Id3v23TagReader.ID3V23);
			}
			else if (versionHigh == "4") {
			    //tag = v24.Read (raf, ID3Flags);
				throw new Exception ("Cannot read ID3v2.4 tags right now !");
			}
			else {
				throw new Exception ("Cannot read unknown version: ID3v2."+versionHigh);
			}
			
			return tag;
		}
		
		private int ReadSyncsafeInt (Stream fs)
		{
			int val = 0;

			val += (fs.ReadByte ()& 0xFF) << 21;
			val += (fs.ReadByte ()& 0xFF) << 14;
			val += (fs.ReadByte ()& 0xFF) << 7;
			val += fs.ReadByte ()& 0xFF;

			return val;
		}
	}
	
}
