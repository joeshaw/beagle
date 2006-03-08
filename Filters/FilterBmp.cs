//
// FilterBmp.cs
//
// Copyright (C) 2006 Alexander Macdonald <alex@alexmac.cc>
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

namespace Beagle.Filters
{
	public class FilterBMP : FilterImage
	{
		enum BitmapCompressionTypes {
			None,
			RunLength8Bit,
			RunLength4Bit,
			RGBBitmapWithMask
		};

		struct BitmapHeader {
			public ushort type;                          // Magic identifier
			public uint size;                            // File size in bytes
			//public ushort reserved1, reserved2;        // unused
			public uint offset;                          // Offset to image data, bytes
		};
		
		struct BitmapInfoHeader {
			public uint size;                            // Header size in bytes
			public int width,height;                     // width and height of image
			public ushort planes;                        // Number of colour planes
			public ushort bits;                          // Bits per pixel
			public BitmapCompressionTypes compression;   // Compression type
			public uint imagesize;                       // Image size in bytes
			public int xresolution,yresolution;          // Pixels per meter
			public uint ncolors;                         // Number of colours
			public uint importantcolors;                 // Important colours
		};

		public FilterBMP () : base ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("image/bmp"));
			PreLoad = false;
		}

		protected override void PullImageProperties ()
		{
			byte [] data = new byte [54];
			Stream.Read (data, 0, data.Length);
			
			// Read the Header
			// We're not using it for anything, so we might as well not parse it.
			/*
			BitmapHeader bmh;
			bmh.type        = EndianConverter.ToUInt16 (data, 0, true);
			bmh.size        = EndianConverter.ToUInt32 (data, 2, true);
			bmh.reserved1   = EndianConverter.ToUInt16 (data, 6, true);
			bmh.reserved2   = EndianConverter.ToUInt16 (data, 8, true);
			bmh.offset      = EndianConverter.ToUInt32 (data, 10, true);
			*/
			
			/* Read the Info Header */
			BitmapInfoHeader bmih;
			bmih.size               = EndianConverter.ToUInt32 (data, 14, true);
			bmih.width              = EndianConverter.ToInt32  (data, 18, true);
			bmih.height             = EndianConverter.ToInt32  (data, 22, true);
			bmih.planes             = EndianConverter.ToUInt16 (data, 26, true);
			bmih.bits               = EndianConverter.ToUInt16 (data, 28, true);
			bmih.compression        = (BitmapCompressionTypes) EndianConverter.ToUInt32 (data, 30, true);
			bmih.imagesize          = EndianConverter.ToUInt32 (data, 34, true);
			bmih.xresolution        = EndianConverter.ToInt32  (data, 38, true);
			bmih.yresolution        = EndianConverter.ToInt32  (data, 42, true);
			bmih.ncolors            = EndianConverter.ToUInt32 (data, 46, true);
			bmih.importantcolors    = EndianConverter.ToUInt32 (data, 50, true);

			AddProperty (Beagle.Property.NewKeyword ("exif:PixelXDimension", bmih.width));
			AddProperty (Beagle.Property.NewKeyword ("exif:PixelYDimension", bmih.height));
			AddProperty (Beagle.Property.NewKeyword ("exif:Planes", bmih.planes));
			AddProperty (Beagle.Property.NewKeyword ("exif:Depth", bmih.bits));
			
			switch	(bmih.compression) {
				case BitmapCompressionTypes.None:
					AddProperty (Beagle.Property.NewKeyword ("exif:Compression", "none"));
					break;
				case BitmapCompressionTypes.RunLength8Bit:
					AddProperty (Beagle.Property.NewKeyword ("exif:Compression", "8bit Runlength"));
					break;
				case BitmapCompressionTypes.RunLength4Bit:
					AddProperty (Beagle.Property.NewKeyword ("exif:Compression", "4bit Runlength"));
					break;
				case BitmapCompressionTypes.RGBBitmapWithMask:
					AddProperty (Beagle.Property.NewKeyword ("exif:Compression", "RGB bitmap with mask"));
					break;
				default:
					AddProperty (Beagle.Property.NewKeyword ("exif:Compression", "unknown"));
					break;
			}
			
			AddProperty (Beagle.Property.NewKeyword ("exif:XResolution",     bmih.xresolution));
			AddProperty (Beagle.Property.NewKeyword ("exif:YResolution",     bmih.yresolution));
			AddProperty (Beagle.Property.NewKeyword ("exif:NumberOfColors",  bmih.ncolors));
			AddProperty (Beagle.Property.NewKeyword ("exif:ImportantColors", bmih.importantcolors));
		}
	}
}
