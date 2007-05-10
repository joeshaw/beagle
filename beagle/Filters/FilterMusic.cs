//
// FilterMusic.cs : This is our interface to taglib-sharp's interface.
// Copyright (C) 2007 Debajyoti Bera <dbera.web@gmail.com>
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
using TagLib;

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
			foreach(string type in SupportedMimeType.AllMimeTypes)
				AddSupportedFlavor (FilterFlavor.NewFromMimeType (type));
		}

		private string GetTaglibMimeType ()
		{
			if (Extension != null && Extension.Length > 0)
				return "taglib/" + Extension.Substring (1);
			else
				return MimeType;
		}

		internal class FilterMusicFileAbstraction : TagLib.File.IFileAbstraction
		{
			private Stream stream;

			public FilterMusicFileAbstraction(Stream stream)
			{
				this.stream = stream;
			}
			
			public string Name {
				get { return null; }
			}

			public System.IO.Stream ReadStream {
				get { return stream; }
			}

			public System.IO.Stream WriteStream {
				get { throw new Exception ("Not supported"); }
			}
		}

		protected override void DoPullProperties ()
		{
			TagLib.File file;
			
			try {
				file = TagLib.File.CreateReadOnly (Stream, GetTaglibMimeType ());
			} catch (Exception e) {
				Logger.Log.Warn (e, "Exception filtering music");
				Finished();
				return;
			}

			TagLib.Tag tag = file.Tag;

			AddProperty (Beagle.Property.New ("fixme:album", tag.Album));
			AddProperty (Beagle.Property.New ("dc:title", tag.Title));

			foreach (string artist in tag.AlbumArtists)
				AddProperty (Beagle.Property.New ("fixme:artist", artist));

			foreach (string performer in tag.Performers)
				AddProperty (Beagle.Property.New ("fixme:performer", performer));

			foreach (string composer in tag.Composers)
				AddProperty (Beagle.Property.New ("fixme:composer", composer));

			foreach (string genre in tag.Genres)
				AddProperty (Beagle.Property.New ("fixme:genre", genre));

			AddProperty (Beagle.Property.New ("fixme:comment", tag.Comment));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:tracknumber", tag.Track));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:trackcount", tag.TrackCount));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:disknumber", tag.Disc));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:diskcount", tag.DiscCount));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:year", tag.Year));

			foreach (TagLib.ICodec codec in file.Properties.Codecs) {
				TagLib.IAudioCodec acodec = codec as TagLib.IAudioCodec;
				
				if (acodec != null && (acodec.MediaTypes & TagLib.MediaTypes.Audio) != TagLib.MediaTypes.Unknown)
				{
					AddProperty (Beagle.Property.NewUnsearched ("fixme:bitrate", acodec.AudioBitrate));
					AddProperty (Beagle.Property.NewUnsearched ("fixme:samplerate", acodec.AudioSampleRate));
					AddProperty (Beagle.Property.NewUnsearched ("fixme:channels", acodec.AudioChannels));
					// One codec is enough
					break;
				}
				// FIXME: Get data from IVideoCodec too
                	}

                	if (file.Properties.MediaTypes != TagLib.MediaTypes.Unknown)
				AddProperty (Beagle.Property.NewUnsearched ("fixme:duration", file.Properties.Duration));

			// FIXME: Store embedded picture and lyrics

			Finished ();
		}
	}
}
