//
// FilterPng.cs
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
using BU = Beagle.Util;

namespace Beagle.Filters {
	
	public class FilterPng : Filter {

		public FilterPng ()
		{
			AddSupportedMimeType ("image/png");
		}
		
		protected override void DoOpen (Stream stream)
		{
			//  8 bytes of signature
			// IHDR chunk:
			//  4 byte chunk length
			//  4 byte chunk type
			// 13 bytes of chunk data
			// total: 29 bytes

			byte[] buffer = new byte [29];
			stream.Read (buffer, 0, 29);

			// Check the signature --- no harm in being paranoid
			if (buffer [0] != 137 || buffer [1] != 80
			    || buffer [2] != 78 || buffer [3] != 71
			    || buffer [4] != 13 || buffer [5] != 10
			    || buffer [6] != 26 || buffer [7] != 10) {
				Console.WriteLine ("Bad signature!");
				return;
			}

			if (buffer [12] != 73 || buffer [13] != 72
			    || buffer [14] != 68 || buffer [15] != 82) {
				Console.WriteLine ("First chunk is not IHDR!");
				return;
			}

			int width = buffer [18] * 256 + buffer [19];
			int height = buffer [22] * 256 + buffer [23];

			this ["_Width"] = width.ToString ();
			this ["_Height"] = height.ToString ();

			this ["_BitDepth"] = buffer [24].ToString ();

			string colorType = null;
			switch (buffer [25]) {
			case 0:
				colorType = "Greyscale";
				break;
			case 2:
				colorType = "Truecolor";
				break;
			case 3:
				colorType = "Indexed";
				break;
			case 4:
				colorType = "Greyscale with alpha";
				break;
			case 6:
				colorType = "Truecolor with alpha";
				break;
			}
			this ["_ColorType"] = colorType;
				
		}
		
		protected override void DoPull () { }
	}
}
