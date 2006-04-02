//
// FilterGif.cs
//
// Copyright (C) 2006 Alexander Macdonald <alex@alexmac.cc>
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterGif : FilterImage {

		enum GifBytes {
			Trailer = 0x3b,
			ImageBlockSeparator = 0x2c,

			BlockIntroducer = 0x21,
			BlockTerminator = 0x00,
		
			GraphicControlExtension = 0xf9,
			CommentExtension = 0xfe,
			PlaintextExtension = 0x01,
			ApplicationExtension = 0xff
		}

		public FilterGif () : base ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("image/gif"));
		}

		protected override void PullImageProperties ()
		{
			try {
				/* Parse the GIF Header to get the version and overall size */
				byte [] data = new byte [13];
				Stream.Read (data, 0, data.Length);

				ushort width = EndianConverter.ToUInt16 (data, 6, true);
				ushort height = EndianConverter.ToUInt16 (data, 8, true);

				char [] gif_version = { (char) data [3], (char) data [4], (char) data [5] };

				AddProperty (Beagle.Property.NewUnsearched ("gif:version", new string (gif_version)));
				
				Width = width;
				Height = height;

				/*
				 * Everything from here onwards is for parsing the GIF Stream
				 * and extracting comments and plaintext and working out how
				 * many frames there are and how many times they are supposed
				 * to loop.
				*/
				
				GifBytes gb;
				int b, num_frames = 0, ct_size;

				if ((data [10] & 0x80) == 0x80) {
					/* A Global Color Table exists */
					ct_size = (int) (3 * Math.Pow(2, (data[10] & 0x07) + 1));
					//System.Console.WriteLine("ct_size: " + ct_size);
					Stream.Seek (ct_size, SeekOrigin.Current);
				}

				while ((gb = (GifBytes) Stream.ReadByte ()) != GifBytes.Trailer) {
					switch (gb) {
						case GifBytes.ImageBlockSeparator:
							num_frames++;

							//System.Console.WriteLine("Start of Image Block : " + Stream.Position);

							Stream.Seek (8, SeekOrigin.Current);
							b = Stream.ReadByte ();

							if ((b & 0x80) == 0x80) {
								/* A Local Color Table exists */
								//System.Console.WriteLine("-- Image Block has local color table");
								ct_size = (int) (3 * Math.Pow(2, (b & 0x07) + 1));
								//System.Console.WriteLine("ct_size: " + ct_size);
								Stream.Seek (ct_size, SeekOrigin.Current);
							}

							Stream.ReadByte ();
							while ((b = Stream.ReadByte ()) != 0x0) {
								//System.Console.WriteLine("-- Image Data Block size: " + b + " pos: " + Stream.Position);
								Stream.Seek (b, SeekOrigin.Current);
							}

							//System.Console.WriteLine("-- Image Block end: " + Stream.Position);
							break;

						case GifBytes.BlockIntroducer:
							//System.Console.WriteLine("Start of Extension : " + Stream.Position);
							break;

						case GifBytes.GraphicControlExtension:
							//System.Console.WriteLine("-- Graphic Control Extension : " + Stream.Position);
							Stream.Seek (6, SeekOrigin.Current);
							break;

						case GifBytes.PlaintextExtension:
							//System.Console.WriteLine("Plaintext Extension: " + Stream.Position);

							Stream.Seek (13, SeekOrigin.Current);

							while ((b = Stream.ReadByte ()) != 0x0) {
								//System.Console.WriteLine("-- Plaintext Data Block size: " + b + " pos: " + Stream.Position);
								char [] cbuffer = new char [b];

								for (int i = 0; i < b; i++)
									cbuffer [i] = (char) Stream.ReadByte ();

								AppendText (new string (cbuffer));
								//System.Console.WriteLine("-- Plaintext Data: " + new string(cbuffer));
							}

							//System.Console.WriteLine("-- Plaintext Extension End: " + Stream.Position);
							break;

						case GifBytes.CommentExtension:
							//System.Console.WriteLine("Comment Extension: " + Stream.Position);

							while ((b = Stream.ReadByte ()) != 0x0) {
								//System.Console.WriteLine("-- Comment Data Block size: " + b + " pos: " + Stream.Position);
								char [] cbuffer = new char [b];

								for (int i = 0; i < b; i++) {
									cbuffer[i] = (char) Stream.ReadByte ();
								}

								AppendText (new string (cbuffer));
								//System.Console.WriteLine("-- Comment Data: " + new string(cbuffer));
							}

							//System.Console.WriteLine("-- Comment Extension End: " + Stream.Position);
							break;

						case GifBytes.ApplicationExtension:
							//System.Console.WriteLine("Application Extension: " + Stream.Position);

							Stream.ReadByte ();

							char [] cbuffer = new char [11];
							for (int i = 0; i < 11; i++) {
								cbuffer [i] = (char) Stream.ReadByte ();
							}

							string application = new string (cbuffer);

							if (application == "NETSCAPE2.0") {
								//System.Console.WriteLine("-- Application: 'NETSCAPE2.0'>");

								Stream.ReadByte ();
								Stream.ReadByte ();

								b = Stream.ReadByte();

								if (b == 0)
									AddProperty (Beagle.Property.NewUnsearched ("gif:loopcount", "infinite"));
								else
									AddProperty (Beagle.Property.NewUnsearched ("gif:loopcount", b));

								Stream.ReadByte();
							} else {
								//unknown extension...
								while ((b = Stream.ReadByte ()) != 0x0) {
									//System.Console.WriteLine("-- Application Data Block size: " + b + " pos: " + Stream.Position);
									Stream.Seek (b, SeekOrigin.Current);
								}
							}
							//System.Console.WriteLine("-- Application Extension End: " + Stream.Position);
							break;

						default:
							break;
					}	
				}
				AddProperty (Beagle.Property.NewUnsearched ("gif:numframes", num_frames));
			} catch (Exception) {
				//System.Console.WriteLine("-- Exception! " + Stream.Position);
				Error ();
			}
		}
	}
}
