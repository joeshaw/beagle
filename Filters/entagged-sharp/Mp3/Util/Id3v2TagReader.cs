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
 * Revision 1.1  2005/08/29 20:09:40  dsd
 * 	* Filters/entagged-sharp/: Import entagged-sharp
 * 	* Filters/FilterMusic.cs, Filters/Makefile.am, configure.in: New
 * 	entagged-sharp-based audio file filter. Remove gst-sharp stuff.
 *
 * Revision 1.4  2005/02/08 12:54:41  kikidonk
 * Added cvs log and header
 *
 */

using System;
using System.IO;
using System.Text;
using Entagged.Audioformats.Util;
using Entagged.Audioformats.Exceptions;

namespace Entagged.Audioformats.Mp3.Util {
	public class Id3v2TagReader {
		
		bool[] ID3Flags;
		Id3v2TagSynchronizer synchronizer = new Id3v2TagSynchronizer();
		
		Id3v23TagReader v23 = new Id3v23TagReader();
		
		public bool Read(Id3Tag tag, Stream mp3Stream)
		{
			byte[] b = new byte[3];

			mp3Stream.Read(b, 0, b.Length);
			mp3Stream.Seek(0, SeekOrigin.Begin);

			string ID3 = Encoding.ASCII.GetString(b);

			if (ID3 != "ID3")
				return false;

			//Begins tag parsing ---------------------------------------------
			mp3Stream.Seek(3, SeekOrigin.Begin);
			//----------------------------------------------------------------------------
			//Version du tag ID3v2.xx.xx
			string versionHigh=mp3Stream.ReadByte() +"";
			string versionID3 =versionHigh+ "." + mp3Stream.ReadByte();
			//------------------------------------------------------------------------- ---
			//D?tection de certains flags (A COMPLETER)
			this.ID3Flags = ProcessID3Flags( (byte) mp3Stream.ReadByte() );
			//----------------------------------------------------------------------------
			
	//			On extrait la taille du tag ID3
			int tagSize = ReadSyncsafeInteger(mp3Stream);
			//System.err.println("TagSize: "+tagSize);
			
	//			------------------NEWNEWNWENENEWNENWEWN-------------------------------
			//Fill a byte buffer, then process according to correct version
			b = new byte[tagSize+2];
			mp3Stream.Read(b, 0, b.Length);
			ByteBuffer bb = new ByteBuffer(b);
			
			if (ID3Flags[0]==true) {
			    //We have unsynchronization, first re-synchronize
			    bb = synchronizer.synchronize(bb);
			}
			
			if (versionHigh == "2")
				v23.Read(tag, bb, ID3Flags, Id3Tag.ID3V22);
			else if (versionHigh == "3")
			    v23.Read(tag, bb, ID3Flags, Id3Tag.ID3V23);
			else
				return false;
			
			return true;
		}
		
		private bool[] ProcessID3Flags(byte b)
		{
			bool[] flags = new bool[] { false, false, false, false };
			if (b == 0)
				return flags;

			// Synchronisation
			if ((b & 128) == 128)
				flags[0] = true;
			
			// Extended header
			if ((b & 64) == 64)
				flags[1] = true;
			
			// Experimental indicator
			if ((b & 32) == 32)
				flags[2] = true;

			// Footer present
			if ((b & 16) == 16)
				flags[3] = true;

			return flags;
		}
		
		
		private int ReadSyncsafeInteger(Stream mp3Stream)
		{
			int value = 0;

			value += (mp3Stream.ReadByte()& 0xFF) << 21;
			value += (mp3Stream.ReadByte()& 0xFF) << 14;
			value += (mp3Stream.ReadByte()& 0xFF) << 7;
			value += mp3Stream.ReadByte()& 0xFF;

			return value;
		}
	}
}
