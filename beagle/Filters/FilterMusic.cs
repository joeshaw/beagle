//
// FilterMusic.cs : This is our interface to entagged-sharp's AudioFileWrapper
//                  interface.
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
using Beagle.Daemon;
using Beagle.Util;
using Entagged;

namespace Beagle.Filters {

	[PropertyKeywordMapping (Keyword="album", PropertyName="fixme:album", IsKeyword=false, Description="Album name of the music")]
	[PropertyKeywordMapping (Keyword="artist", PropertyName="fixme:artist", IsKeyword=false, Description="Artist of the music")]
	[PropertyKeywordMapping (Keyword="genre", PropertyName="fixme:genre", IsKeyword=true, Description="Genre of the music")]
	public class FilterMusic : Beagle.Daemon.Filter {
	
		public FilterMusic ()
		{
			// 1: Added duration and bitrate property
			SetVersion (1);
			SetFileType ("audio");
		}

		protected override void RegisterSupportedTypes ()
		{
			// APE / Monkeys Audio
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".ape"));

			// FLAC
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-flac"));

			// MP3
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-mp3"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/mpeg"));

			// MPC / Musepack / MPEG+
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".mpc"));
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".mp+"));

			// M4A / Apple Audio Codec
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-m4a"));
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".m4p"));

			// OGG Vorbis
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/ogg"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-vorbis+ogg"));

			// Tracker / Amiga Audio
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-s3m"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-it"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-mod"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-xm"));

			// ASF / WMA
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-ms-wma"));
		}

		private string GetEntaggedMimeType ()
		{
			if (Extension != null && Extension.Length > 0)
				return "entagged/" + Extension.Substring (1);
			else
				return MimeType;
		}

		protected override void DoPullProperties ()
		{
			AudioFile tag;
			
			try {
				tag = new AudioFile (Stream, GetEntaggedMimeType ());
			} catch (Exception e) {
				Logger.Log.Warn (e, "Exception filtering music");
				Finished();
				return;
			}

			foreach (string artist in tag.Artists)
				AddProperty (Beagle.Property.New ("fixme:artist", artist));

			foreach (string album in tag.Albums)
				AddProperty (Beagle.Property.New ("fixme:album", album));

			foreach (string title in tag.Titles)
				AddProperty (Beagle.Property.New ("fixme:title", title));

			foreach (string comment in tag.Comments)
				AddProperty (Beagle.Property.New ("fixme:comment", comment));

			foreach (int track in tag.TrackNumbers)
				AddProperty (Beagle.Property.NewUnsearched ("fixme:tracknumber", track));

			foreach (int track in tag.TrackCounts)
				AddProperty (Beagle.Property.NewUnsearched ("fixme:trackcount", track));

			foreach (int year in tag.Years)
				AddProperty (Beagle.Property.NewUnsearched ("fixme:year", year));

			foreach (string genre in tag.Genres)
				AddProperty (Beagle.Property.NewKeyword ("fixme:genre", genre));

			AddProperty (Beagle.Property.NewUnsearched ("fixme:duration", tag.Duration));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:bitrate", tag.Bitrate));

			Finished ();
		}
	}
}
