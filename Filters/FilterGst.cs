//
// FilterGst.cs
//
// Copyright (C) 2004 Adam Lofts
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
using System.Collections;
using Gst;
using Beagle.Util;

namespace Beagle.Filters {

	public class FilterGst : Beagle.Daemon.Filter {

		static bool has_inited = false;
		static ArrayList mime_cache;

		private static void InitGst() {
			Gst.Application.Init("Beagle");

			mime_cache = new ArrayList();
			
			Element decoder;
			
			decoder = ElementFactory.Make("mad",null);
			if (decoder != null)
				mime_cache.Add("audio/x-mp3");

			decoder = ElementFactory.Make("vorbisdec",null);
			if (decoder != null)
				mime_cache.Add("application/ogg");

			decoder = ElementFactory.Make("flacdec",null);
			if (decoder != null)
				mime_cache.Add("application/x-flac");
			
			decoder = ElementFactory.Make("asfdemux",null);
			if (decoder != null)
				mime_cache.Add("video/x-ms-asf");
		}

		public FilterGst ()
		{
			if (!has_inited) {
				InitGst();
				has_inited = true;
			}

			foreach(string mime_type in mime_cache) {
				AddSupportedMimeType (mime_type);
			}
		}

		bool keep_looking;

		protected override void DoPullProperties ()
		{
			Pipeline pipe = new Pipeline(null);

			FileSrc src = ElementFactory.Make ("filesrc", null) as FileSrc;
			Spider spider = ElementFactory.Make ("spider", null) as Spider;
			FakeSink sink = ElementFactory.Make ("fakesink", null) as FakeSink;

			pipe.Add(src);
			pipe.Add(spider);
			pipe.Add(sink);
	
			src.Link(spider);
		
			Caps caps = Caps.FromString("audio/x-raw-int");
			spider.LinkFiltered(sink, caps);

			src.Location = FileInfo.FullName;
			sink.SignalHandoffs = true;
		
			spider.FoundTag += FoundTagHandler;
			pipe.Error += ErrorHandler;
			sink.Handoff += HandoffHandler;

			pipe.SetState(ElementState.Playing);
			keep_looking = true;
			while (pipe.Iterate() && keep_looking) { /* wait here! */ }
			pipe.SetState(ElementState.Null);

			Finished ();
		}

		void HandoffHandler (object o, HandoffArgs args) {
			keep_looking = false; 
		}
		void ErrorHandler (object o, ErrorArgs args) {
			Logger.Log.Warn("FilterGst error: {0}", args.Debug);
			keep_looking = false; 
		}
		void FoundTagHandler (object o, FoundTagArgs args) {
			TagList list = args.TagList;
			list.Foreach(new TagForeachFunc(TagForeachFunc));
		}
		void TagForeachFunc (TagList list, string tag) {
			string val_str;
			uint val_uint;

			switch (tag) {
				//Strings
				case "comment":
				case "title":
				case "genre":
				case "artist":
				case "album":
				list.GetString(tag, out val_str);
				AddProperty (Beagle.Property.New ("fixme:" + tag, val_str));
				break;

				case "track-number":
				list.GetUint(tag, out val_uint);
				AddProperty (Beagle.Property.New ("fixme:tracknumber", val_uint));
				break;
			}
		}
	}
}

/* Stolen from Gstreamer gsttag.h - All possible tags?

#define GST_TAG_TITLE			"title"
#define GST_TAG_ARTIST			"artist"
#define GST_TAG_ALBUM			"album"
#define GST_TAG_DATE			"date"
#define GST_TAG_GENRE			"genre"
#define GST_TAG_COMMENT			"comment"
#define GST_TAG_TRACK_NUMBER		"track-number"
#define GST_TAG_TRACK_COUNT		"track-count"
#define GST_TAG_ALBUM_VOLUME_NUMBER	"album-disc-number"
#define GST_TAG_ALBUM_VOLUME_COUNT	"album-disc-count"
#define GST_TAG_LOCATION		"location"
#define GST_TAG_DESCRIPTION		"description"
#define GST_TAG_VERSION			"version"
#define GST_TAG_ISRC			"isrc"
#define GST_TAG_ORGANIZATION		"organization"
#define GST_TAG_COPYRIGHT		"copyright"
#define GST_TAG_CONTACT			"contact"
#define GST_TAG_LICENSE			"license"
#define GST_TAG_PERFORMER		"performer"
#define GST_TAG_DURATION		"duration"
#define GST_TAG_CODEC			"codec"
#define GST_TAG_VIDEO_CODEC		"video-codec"
#define GST_TAG_AUDIO_CODEC		"audio-codec"
#define GST_TAG_BITRATE			"bitrate"
#define GST_TAG_NOMINAL_BITRATE		"nominal-bitrate"
#define GST_TAG_MINIMUM_BITRATE		"minimum-bitrate"
#define GST_TAG_MAXIMUM_BITRATE		"maximum-bitrate"
#define GST_TAG_SERIAL			"serial"
#define GST_TAG_ENCODER			"encoder"
#define GST_TAG_ENCODER_VERSION		"encoder-version"
#define GST_TAG_TRACK_GAIN		"replaygain-track-gain"
#define GST_TAG_TRACK_PEAK		"replaygain-track-peak"
#define GST_TAG_ALBUM_GAIN  		"replaygain-album-gain"
#define GST_TAG_ALBUM_PEAK		"replaygain-album-peak"
*/
