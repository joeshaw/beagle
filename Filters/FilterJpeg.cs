//
// FilterJpeg.cs
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
using BU = Beagle.Util;

namespace Beagle.Filters {
	
	public class FilterJpeg : Beagle.Daemon.Filter {

		public FilterJpeg ()
		{
			AddSupportedMimeType ("image/jpeg");
		}

		// FIXME: This is not particularly efficient
		protected override void DoPullProperties ()
		{
			Stream stream = CurrentFileInfo.Open (FileMode.Open);

			bool seenSofn = false;
			int x, y, len, marker;

			x = stream.ReadByte ();
			if (x != 0xff)
				return;
			x = stream.ReadByte ();
			if (x != 0xd8) // SOI
				return;

			while (true) {
				// Find next marker
				x = 0;
				while (x != 0xff) {
					x = stream.ReadByte ();
					if (x == -1)
						return;
				}
				do {
					marker = stream.ReadByte ();
				} while (marker == 0xff);
				if (marker == -1)
					return;

				x = stream.ReadByte ();
				if (x == -1)
					return;
				y = stream.ReadByte ();
				if (y == -1)
					return;
				len = (x << 8) | y;

				if (marker == 0xfe) {
					
					byte[] commentData = new byte [len-2];
					stream.Read (commentData, 0, len-2);

					Encoding enc = new ASCIIEncoding ();
					string comment = enc.GetString (commentData);
					AddProperty (Beagle.Property.New ("fixme:comment", comment));
							   
				} else if ((! seenSofn)
				    && 0xc0 <= marker
				    && marker <= 0xcf
				    && marker != 0xc4
				    && marker != 0xcc ) { // SOFn

					int precision = stream.ReadByte ();
					if (precision == -1)
						return;
					
					x = stream.ReadByte ();
					if (x == -1)
						return;
					y = stream.ReadByte ();
					if (y == -1)
						return;
					int height = (x << 8) | y;

					x = stream.ReadByte ();
					if (x == -1)
						return;
					y = stream.ReadByte ();
					if (y == -1)
						return;
					int width = (x << 8) | y;

					int components = stream.ReadByte ();
					if (components == -1)
						return;

					AddProperty (Beagle.Property.NewKeyword ("fixme:bitdepth",
											precision));
					
					AddProperty (Beagle.Property.NewKeyword ("fixme:width",
											width));

					AddProperty (Beagle.Property.NewKeyword ("fixme:height",
											height));

					AddProperty (Beagle.Property.NewKeyword ("fixme:components",
											components));

					seenSofn = true;
				} else {
					// Skip past this segment
					stream.Seek (len - 2, SeekOrigin.Current);
				}
			}
		}
	}
}
