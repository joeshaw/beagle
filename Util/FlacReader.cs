//
// FlacReader.cs : Reads a flac tag from a given stream
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

	namespace Flac {
		
		public class MetadataBlockHeader {
			
			public const int STREAMINFO=0, PADDING=1, APPLICATION=2, SEEKTABLE=3, VORBIS_COMMENT=4, CUESHEET=5, UNKNOWN=6;
			private int blockType, dataLength;
			private bool lastBlock;
			private byte[] data;
			
			public MetadataBlockHeader (byte[] b) {

				lastBlock = ( (b[0] & 0x80) >> 7 ) == 1;
				
				int type = b[0] & 0x7F;
				switch (type) {
					case 0: blockType = STREAMINFO; break;
					case 1: blockType = PADDING; break;
					case 2: blockType = APPLICATION; break;
					case 3: blockType = SEEKTABLE; break;
					case 4: blockType = VORBIS_COMMENT; break;
					case 5: blockType = CUESHEET; break;
					default: blockType = UNKNOWN; break;
				}
				
				dataLength = (u (b[1])<<16) + (u (b[2])<<8) + (u (b[3]));
				
				data = new byte[4];
				data[0] = (byte) (data[0] & 0x7F);
				for (int i = 1; i < 4; i ++) {
					data[i] = b[i];
				}
			}
		
			public int DataLength {
				get {
					return dataLength;
				}
			}
			
			public int BlockType {
				get {
					return blockType;
				}
			}
			
			public string BlockTypeString {
				get {
					switch (blockType) {
						case 0: return "STREAMINFO";
						case 1: return "PADDING";
						case 2: return "APPLICATION";
						case 3: return "SEEKTABLE";
						case 4: return "VORBIS_COMMENT";
						case 5: return "CUESHEET";
						default: return "UNKNOWN-RESERVED";
					}
				}
			}
			
			public bool IsLastBlock {
				get {
					return lastBlock;
				}
			}
			
			public byte[] Data {
				get {
					return data;
				}
			}
			
			private int u (int i) {
				return i & 0xFF;
			}
		}
	}

	public class FlacTagReader {
		
		private ASCIIEncoding ascii = new ASCIIEncoding ();
		private UTF8Encoding utf = new UTF8Encoding ();
		
		public Tag Read (Stream fs) 
		{
			//Begins tag parsing-------------------------------------
			if (fs.Length < 4) {
				//Empty File
				throw new Exception ("Error: File empty");
			}
			fs.Seek (0, SeekOrigin.Begin);

			//FLAC Header string
			byte[] b = new byte[4];
			fs.Read (b, 0, 4);
			string flac = ascii.GetString (b, 0, 4);
			if (flac != "fLaC")
				throw new Exception ("fLaC Header not found, not a flac file");
			
			// Seems like we hava a valid stream
			bool isLastBlock = false;
			while (!isLastBlock) {
				b = new byte[4];
				fs.Read (b, 0, 4);
				Flac.MetadataBlockHeader mbh = new Flac.MetadataBlockHeader (b);
			
				switch (mbh.BlockType) {
					//We got a vorbis comment block, parse it
					case Flac.MetadataBlockHeader.VORBIS_COMMENT:
						//We have it, so no need to go further
						return HandleVorbisComment (mbh, fs);
					
					//This is not a vorbis comment block, we skip to next block
					default:
						fs.Seek (mbh.DataLength, SeekOrigin.Current);
						break;
				}

				isLastBlock = mbh.IsLastBlock;
				mbh = null;
			}
			// FLAC not found...
			throw new Exception ("FLAC Tag could not be found or read..");
		}
		
		private Tag HandleVorbisComment (Flac.MetadataBlockHeader mbh, Stream fs) 
		{
			Tag tag = new Tag ();
			byte[] b = new byte [mbh.DataLength];
			fs.Read (b, 0, b.Length);
			
			int pos = 0;
			int vendorstringLength = getNumber (b, 0, 3);
			pos += 4;
			
			string vendorstring = utf.GetString (b, 4, vendorstringLength);
			pos += vendorstringLength;
		
			int userComments = getNumber (b, pos, pos + 3);
			pos += 4;
			
			Hashtable ht = new Hashtable (10);
		
			for (int i = 0; i < userComments; i++) {
				int commentLength = getNumber (b, pos, pos + 3);
				pos += 4;
				
				string comment = utf.GetString (b, pos, commentLength);
				pos += commentLength;
				
				string[] splitComment = comment.Split (new char[] {'='});
				if (splitComment.Length > 1)
					ht [splitComment [0]] = splitComment [1];
			}
			
			foreach (DictionaryEntry en in ht) {
				string key = ((string) en.Key).ToUpper ();
				string val = (string) en.Value;
				
				if (val.Trim () != "" ) {
					if (key == "TITLE")
						tag.Title = val;
					else if (key == "ARTIST")
						tag.Artist = val;
					else if (key == "ALBUM")
						tag.Album = val;
					else if (key == "DATE")
						tag.Year = val;
					else if (key == "COMMENT" || key == "DESCRIPTION")
						tag.Comment = val;
					else if (key == "TRACK" || key == "TRACKNUMBER")
						tag.Track = val;
					else if (key == "GENRE")
						tag.Genre = val;
					else {
						Console.Error.Write ("FlacTagReader: Warning: Unknown field: " + key + " | " + val);
					}
				}
			}
				
			tag.Vendor = vendorstring;
				
			return tag;
		}
		
		//Computes a (end-start) bytes long number 
		private int getNumber (byte[] b, int start, int end) {
			int number = 0;
			for (int i = 0; i< (end-start + 1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
	}
}
