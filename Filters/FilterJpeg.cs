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
using Beagle.Util;

namespace Beagle.Filters {
	
	public class FilterJpeg : Beagle.Daemon.Filter {

		public FilterJpeg ()
		{
			AddSupportedMimeType ("image/jpeg");
		}

		// FIXME: This is not particularly efficient
		protected override void DoPullProperties ()
		{
			JpegHeader header = new JpegHeader (CurrentFileInfo.FullName);
			byte [] data = header.GetRawExif ();
			if (data == null || data.Length == 0)
				return;
			ExifData exif = new ExifData (data, (uint) data.Length);
			if (exif == null)
				return;
			
			string str;
			
			str = exif.LookupString (ExifTag.UserComment);
			if (str != null && str != "")
				AddProperty (Beagle.Property.New ("exif:UserComment", str));

			str = exif.LookupString (ExifTag.ImageDescription);
			if (str != null && str != "")
				AddProperty (Beagle.Property.New ("exif:ImageDescription", str));

			str = exif.LookupString (ExifTag.PixelXDimension);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:PixelXDimension", str));

			str = exif.LookupString (ExifTag.PixelYDimension);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:PixelYDimension", str));

			str = exif.LookupString (ExifTag.ISOSpeedRatings);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:ISOSpeedRatings", str));

			str = exif.LookupString (ExifTag.ShutterSpeedValue);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:ShutterSpeedValue", str));

			str = exif.LookupString (ExifTag.ExposureTime);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:ExposureTime", str));

			str = exif.LookupString (ExifTag.FNumber);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:FNumber", str));

			str = exif.LookupString (ExifTag.ApertureValue);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:ApertureValue", str));

			str = exif.LookupString (ExifTag.FocalLength);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:FocalLength", str));

			str = exif.LookupString (ExifTag.Flash);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:Flash", str));

			str = exif.LookupString (ExifTag.Model);
			if (str != null && str != "")
				AddProperty (Beagle.Property.NewKeyword ("exif:Model", str));

			str = exif.LookupString (ExifTag.Copyright);
			if (str != null && str != "")
				AddProperty (Beagle.Property.New ("exif:Copyright", str));

			str = exif.LookupString (ExifTag.DateTime);
			if (str != null && str != "") {
				DateTime dt;
				dt = ExifData.DateTimeFromString (str);
				AddProperty (Beagle.Property.NewDate ("exif:DateTime", dt));
			}
		}
	}
}
