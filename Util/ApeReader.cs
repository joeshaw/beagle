//
// ApeReader.cs : Reads an Ape tag from the given stream
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

	public class ApeTagReader {
	
		private ASCIIEncoding ascii = new ASCIIEncoding ();
		private UTF8Encoding utf = new UTF8Encoding ();
		
		public Tag Read (Stream fs)
		{
			Tag tag = new Tag ();
			
			//Check wether the file contains an APE tag--------------------------------
			fs.Seek (-32, SeekOrigin.End);
			
			byte[] b = new byte[8];
			fs.Read (b, 0, 8);
			
			string tagS = ascii.GetString (b, 0, 8);
			if (tagS != "APETAGEX"){
				throw new Exception ("There is no APE Tag in this file");
			}
			//Parse the tag -)------------------------------------------------
			//Version
			b = new byte[4];
			fs.Read (b, 0, 4);
			int version = getNumber (b, 0, 3);
			if (version != 2000) {
				throw new Exception ("APE Tag other than version 2.0 are not supported");
			}
			
			//Size
			b = new byte[4];
			fs.Read (b, 0, 4);
			int tagSize = getNumber (b, 0, 3);

			//Number of items
			b = new byte[4];
			fs.Read (b, 0, 4);
			int itemNumber = getNumber (b, 0, 3);
			
			//Tag Flags
			b = new byte[4];
			fs.Read (b, 0, 4);
			//TODO handle these
			
			fs.Seek (-tagSize, SeekOrigin.End);
			
			for (int i = 0; i < itemNumber; i++) {
				//Content length
				b = new byte[4];
				fs.Read (b, 0, 4);
				int contentLength = getNumber (b, 0, 3);
				
				//Item flags
				b = new byte[4];
				fs.Read (b, 0, 4);
				//TODO handle these
				bool binary = ((b[0]&0x06) >> 1) == 1;
				
				int j = 0;
				while (fs.ReadByte () != 0)
					j++;
				fs.Seek (-j-1, SeekOrigin.Current);
				int fieldSize = j;
				
				//Read Item key
				b = new byte[fieldSize];
				fs.Read (b, 0, fieldSize);
				fs.ReadByte ();
				string field = ascii.GetString (b, 0, fieldSize);
				
				//Read Item content
				b = new byte[contentLength];
				fs.Read (b, 0, contentLength);
				string content;
				if (!binary)
					content = utf.GetString (b, 0, contentLength);
				else
					content = ascii.GetString (b, 0, contentLength);
							
				if (field.ToLower () == "title")
					tag.Title = content;
				else if (field.ToLower () == "artist")
					tag.Artist = content;
				else if (field.ToLower () == "album")
					tag.Album = content;
				else if (field.ToLower () == "year")
					tag.Year = content;
				else if (field.ToLower () == "comment") {
					tag.Comment = content;
				} else if (field.ToLower () == "track")
					tag.Track = content;
				else if (field.ToLower () == "genre")
					tag.Genre = content;
			}
			
			return tag;
		}
		
		//Computes a (end-start) bytes long number 
		private int getNumber (byte[] b, int start, int end)
		{
			int number = 0;
			for (int i = 0; i< (end-start+1); i++) {
				number += ((b[start+i]&0xFF) << i*8);
			}
			
			return number;
		}
	}
}
