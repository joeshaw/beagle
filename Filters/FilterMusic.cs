//
// FilterMusic.cs
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

	public class FilterMusic : Filter {

		public FilterMusic ()
		{
			AddSupportedMimeType ("audio/x-mp3");
		}

		protected override void DoPullProperties ()
		{
			Stream stream = CurrentFileInfo.Open (FileMode.Open);

			BU.Id3Info info;

			info = BU.Id3v2.Read (stream);
			if (info == null)
				info = BU.Id3v1.Read (stream);
			if (info == null)
				return;

			AddProperty (Property.NewKeyword ("fixme:id3version", info.Version));

			AddProperty (Property.New ("fixme:artist",  info.Artist));
			AddProperty (Property.New ("fixme:album",   info.Album));
			AddProperty (Property.New ("fixme:song",    info.Song));
			AddProperty (Property.New ("fixme:comment", info.Comment));

			if (info.Track > 0)
				AddProperty (Property.NewKeyword ("fixme:track", info.Track));

			if (info.Year > 0)
				AddProperty (Property.NewKeyword ("fixme:year", info.Year));

			if (info.HasPicture)
				AddProperty (Property.NewBool ("fixme:haspicture", true));
		}
	}
}
