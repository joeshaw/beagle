//
// OggReader.cs : Reads an ogg tag from the given stream
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

	namespace Ogg {
	
		public class OggPageHeader {
		
			private double absoluteGranulePosition;
			private byte[] checksum;
			private byte headerTypeFlag;
			private bool valid = false;
			private int pageLength = 0;
			private int pageSequenceNumber, streamSerialNumber;

			public OggPageHeader (byte[] b)
			{
				int streamStructureRevision = b[4];
				headerTypeFlag = b[5];
				if (streamStructureRevision == 0) {
					this.absoluteGranulePosition = 0;
					for (int i = 0; i < 8; i++)
						this.absoluteGranulePosition += u(b[i + 6]) * (2 << (8 * i));

					streamSerialNumber = u(b[14]) + (u(b[15]) << 8) + 
						(u(b[16]) << 16) + (u(b[17]) << 24);

					pageSequenceNumber = u(b[18]) + (u(b[19]) << 8) + 
						(u(b[20]) << 16) + (u(b[21]) << 24);

					checksum = new byte[]{b[22], b[23], b[24], b[25]};

					byte[] segmentTable = new byte[b.Length - 27];
					for (int i = 0; i < segmentTable.Length; i++) {
						segmentTable[i] = b[27 + i];
						this.pageLength += u(b[27 + i]);
					}

					valid = true;
				}
			}

			public double AbsoluteGranulePosition {
				get {
					return this.absoluteGranulePosition;
				}
			}

			public byte[] CheckSum {
				get {
					return checksum;
				}
			}

			public byte HeaderType {
				get {
					return headerTypeFlag;
				}
			}

			public int PageLength {
				get {
					return pageLength;
				}
			}
			
			public int PageSequence {
				get {
					return pageSequenceNumber;
				}
			}
			
			public int SerialNumber {
				get {
					return streamSerialNumber;
				}
			}


			public bool IsValid {
				get {
					return valid;
				}
			}
			
			private int u (int i) {
				return i & 0xFF;
			}
		}
	}

	public class OggTagReader {
	
		private ASCIIEncoding ascii = new ASCIIEncoding ();
		private UTF8Encoding utf = new UTF8Encoding ();
		
		public Tag Read (Stream fs)
		{
			long oldPos = 0;
			//----------------------------------------------------------

			Tag tag = new Tag ();
			
			//Check wheter we have an ogg stream---------------
			fs.Seek (0, SeekOrigin.Begin);
			byte[] b = new byte[4];
			fs.Read (b, 0, 4);
			
			string ogg = ascii.GetString (b, 0, 4);
			if (ogg != "OggS")
				throw new Exception ("OggS Header could not be found, not an ogg stream");
			//--------------------------------------------------
			
			//Parse the tag ------------------------------------
			fs.Seek (0, SeekOrigin.Begin);

			//Supposing 1st page = codec infos
			//			2nd page = comment+decode info
			//...Extracting 2nd page
			
			//1st page to get the length
			b = new byte[4];
			oldPos = fs.Position;
			fs.Seek (26, SeekOrigin.Begin);
			int pageSegments = fs.ReadByte ()&0xFF; //unsigned
			fs.Seek (oldPos, SeekOrigin.Begin);
			
			b = new byte[27 + pageSegments];
			fs.Read (b, 0, 27+pageSegments);

			Ogg.OggPageHeader pageHeader = new Ogg.OggPageHeader (b);

			fs.Seek (pageHeader.PageLength, SeekOrigin.Current);

			//2nd page extraction
			oldPos = fs.Position;
			fs.Seek (26, SeekOrigin.Current);
			pageSegments = fs.ReadByte ()&0xFF; //unsigned
			fs.Seek (oldPos, SeekOrigin.Begin);
			
			b = new byte[27 + pageSegments];
			fs.Read (b, 0, 27+pageSegments);
			pageHeader = new Ogg.OggPageHeader (b);

			b = new byte[7];
			fs.Read (b, 0, 7);
			
			b = new byte[4];
			fs.Read (b, 0, 4);

			int vendorStringLength = getNumber (b, 0, 3);

			b = new byte[vendorStringLength];
			fs.Read (b, 0,vendorStringLength );

			string vendorString = utf.GetString (b, 0, vendorStringLength);

			b = new byte[4];
			fs.Read (b, 0, 4);

			int userComments = getNumber (b, 0, 3);

			Hashtable ht = new Hashtable (10);
			for (int i = 0; i < userComments; i++) {
				b = new byte[4];
				fs.Read (b, 0, 4);

				int commentLength = getNumber (b, 0, 3);

				b = new byte[commentLength];
				fs.Read (b, 0, commentLength);

				string comment = utf.GetString (b, 0, commentLength);

				string[] splitComment = comment.Split (new char[] {'='});
				if (splitComment.Length>1)
					ht[splitComment[0]] = splitComment[1];
			}

			byte isValid = (byte) fs.ReadByte ();

			if (isValid == 0)
				throw new Exception ("Error: The OGG Stream isn't valid, could not extract the tag");
			
			foreach (DictionaryEntry en in ht) {
				string key = ( (string) en.Key).ToUpper ();
				string val = (string) en.Value;
				
				if (val.Trim () != "") {
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
				}
			}
				
			tag.Vendor = vendorString;
				
			return tag;
		}
		//Computes a (end-start) bytes long number 
		private int getNumber (byte[] b, int start, int end)
		{
			int number = 0;
			for (int i = 0; i< (end-start+1); i++) {
				number += ( (b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
	}
}
