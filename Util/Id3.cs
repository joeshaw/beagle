//
// Id3.cs
//
// Copyright (C) 2004 Novell, Inc.
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

namespace Beagle.Util {

	public class Id3Info {
		string version = null;
		string artist  = null;
		string album   = null;
		string song    = null;
		string comment = null;
		int    track   = -1;
		int    year    = -1;
		bool   hasPic  = false;

		public Id3Info (string _version)
		{
			version = _version;
		}

		static private string CleanString (string str)
		{
			int i = str.IndexOf ('\0');
			if (i != -1)
				str = str.Substring (0, i);
			str = str.Trim ();
			return str;
		}

		public string Version {
			get { return version; }
		}

		public string Artist {
			get { return artist; }
			set { artist = CleanString (value); }
		}

		public string Album {
			get { return album; }
			set { album = CleanString (value); }
		}

		public string Song {
			get { return song; }
			set { song = CleanString (value); }
		}

		public string Comment {
			get { return comment; }
			set { comment = CleanString (value); }
		}

		public int Track {
			get { return track; }
			set { track = (value > 0) ? value : -1; }
		}

		public int Year {
			get { return year; }
			set { year = (value > 1) ? value : -1; }
		}

		public bool HasPicture {
			get { return hasPic; }
			set { hasPic = value; }
		}
	} 

	public class Id3v1 {
		
		static public Id3Info Read (Stream stream)
		{
			if (! stream.CanSeek)
				return null;

			stream.Seek (-128, SeekOrigin.End);
			byte[] buffer = new byte [128];
			stream.Read (buffer, 0, 128);

			if (buffer [0] != 'T' || buffer [1] != 'A' || buffer [2] != 'G')
				return null;

			Id3Info info = new Id3Info ("ID3v1");
			Encoding encoding = new ASCIIEncoding ();

			info.Song    = encoding.GetString (buffer,  3, 30);
			info.Artist  = encoding.GetString (buffer, 33, 30);
			info.Album   = encoding.GetString (buffer, 63, 30);
			info.Comment = encoding.GetString (buffer, 97, 28);

			// ID3v1.1 allows for a track number embedded at the end of the
			// comment field.
			if (buffer [125] == 0 && buffer [126] > 0)
				info.Track = buffer [126];

			try {
				info.Year = int.Parse (encoding.GetString (buffer, 93,  4));
			} catch { }
			

			return info;
		}
	}

	public class Id3v2 {

		static int SyncSafe4 (byte[] buffer, int offset)
		{
			int val = 0;
			val += buffer [offset    ] << 21;
			val += buffer [offset + 1] << 14;
			val += buffer [offset + 2] << 7;
			val += buffer [offset + 3];
			return val;
		}

		static int SyncSafe3 (byte[] buffer, int offset)
		{
			int val = 0;
			val += buffer [offset    ] << 14;
			val += buffer [offset + 1] << 7;
			val += buffer [offset + 2];
			return val;
		}

		static public Id3Info Read (Stream stream)
		{
			if (! stream.CanSeek)
				return null;

			// First, look for the header
			byte[] header = new byte [10];
			stream.Seek (0, SeekOrigin.Begin);
			stream.Read (header, 0, 10);

			if (header[0] != 'I' || header[1] != 'D' || header[2] != '3')
				return null;

			int versionMajor = header[3];
			int versionMinor = header[4];
			int headerFlags  = header[5];

			bool flagUnsync         = (headerFlags & 128) != 0;
			bool flagExtendedHeader = (headerFlags & 64) != 0;
			bool flagExperimental   = (headerFlags & 32) != 0;
			bool flagFooter         = (headerFlags & 16) != 0;

//			if (flagUnsync)
//				Console.WriteLine ("Unsync flag is set!");

			int totalSize = SyncSafe4 (header, 6);
			int pos = 0;

			// If we have an extended header, just skip it.
			if (flagExtendedHeader) {
				byte[] exthead = new byte [4];
				stream.Read (exthead, 0, 4);
				int extsize = SyncSafe4 (exthead, 0);
				// The extended header size includes those first four bytes.
				stream.Seek (extsize - 4, SeekOrigin.Current);
				pos += extsize;
			}

			Id3Info info = new Id3Info (string.Format ("ID3v2.{0}.{1}",
								   versionMajor,
								   versionMinor));

			Encoding ascii = new ASCIIEncoding ();

			// Read frames
			while (pos < totalSize) {

				if (versionMajor == 2) {
					stream.Read (header, 0, 6);
					pos += 6;
				} else {
					stream.Read (header, 0, 10);
					pos += 10;
				}

				if (header [0] == 0)
					break;

				string id;
				int size, flags;

				if (versionMajor == 2) {
					id = ascii.GetString (header, 0, 3);
					size = SyncSafe3 (header, 3);
					flags = 0;
					pos += 6;
				} else {
					id = ascii.GetString (header, 0, 4);
					size  = SyncSafe4 (header, 4);
					flags = (header [8] << 8) | header [9];
					pos += 10;
				}

				bool flagCompressed = (flags & 8) != 0;
				bool flagEncrypted  = (flags & 4) != 0;
				
				if (flagCompressed || flagEncrypted) {
					// We don't want to deal with that crap, so
					// just skip the frame.
					stream.Seek (size, SeekOrigin.Current);
					continue;
				}
				
				// If the frame size is obviously wrong, just bail out.
				if (pos + size >= totalSize)
					break;

				byte[] frameData = new byte [size];
				stream.Read (frameData, 0, size);

				ProcessFrame (info, id, frameData);
			}
			
			return info;
		}

		static private string FrameString (byte[] frameData)
		{
			int frameEncoding = frameData [0];
			Encoding encoding;
			if (frameEncoding == 0) {
				// Scrub out 8-bit characters
				for (int i = 1; i < frameData.Length; ++i)
					if (frameData [i] > 127)
						frameData [i] = (byte) '?';
				encoding = new ASCIIEncoding ();
			} else if (frameEncoding == 1 || frameEncoding == 2) {
				encoding = new UnicodeEncoding ();
			} else if (frameEncoding == 3) {
				encoding = new UTF8Encoding ();
			} else {
				return null;
			}
			return encoding.GetString (frameData, 1, frameData.Length-1);
		}

		static private void ProcessFrame (Id3Info info, string id, byte[] frameData)
		{
			switch (id) {

			case "TAL": 
			case "TALB":
				info.Album = FrameString (frameData);
				break;

			case "TP1":
			case "TPE1":
				info.Artist = FrameString (frameData);
				break;

			case "TT2":
			case "TIT2":
				info.Song = FrameString (frameData);
				break;

			case "COM":
			case "COMM":
				info.Comment = FrameString (frameData);
				break;

			case "TRK":
			case "TRCK":
				try {
					info.Track = int.Parse (FrameString (frameData));
				} catch { }
				break;

			case "TYE":
			case "TYER":
				try {
					info.Year = int.Parse (FrameString (frameData));
				} catch { }
				break;

			case "APIC":
				info.HasPicture = true;
				break;
			}
		}
	}
}
