//
// FilterFlac.cs
//
// Copyright (C) Raphaël Slinckx
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

using BUF = Beagle.Util.FlacReader;

namespace Beagle.Filters {

	public class FilterFlac : Beagle.Daemon.Filter {
		
		private BUF.FlacTagReader reader = new BUF.FlacTagReader ();
		
		public FilterFlac ()
		{
			AddSupportedMimeType ("audio/x-flac");
		}

		protected override void DoPullProperties ()
		{
			BUF.Tag tag = null;
			try {
				tag = reader.read (Stream);
			} catch (Exception e) {
				Finished();
				return;
			}
			
			// FIXME: Do we need to check for non-null empty values ?
			AddProperty (Beagle.Property.New ("fixme:artist",  tag.Artist));
			AddProperty (Beagle.Property.New ("fixme:album",   tag.Album));
			AddProperty (Beagle.Property.New ("fixme:song",    tag.Title));
			AddProperty (Beagle.Property.New ("fixme:comment", tag.Comment));
			AddProperty (Beagle.Property.NewKeyword ("fixme:track", tag.Track));
			AddProperty (Beagle.Property.NewKeyword ("fixme:year", tag.Year));
			AddProperty (Beagle.Property.NewKeyword ("fixme:genre", tag.Genre));
			
			Finished ();
		}
	}
}
