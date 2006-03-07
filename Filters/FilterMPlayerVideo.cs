//
// FilterMPlayerVideo.cs
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
using System.Diagnostics;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterMPlayerVideo : Beagle.Daemon.Filter {
	
		private int width = -1;
		private int height = -1;
		private float aspect = 0.0f;
		private float fps = 0.0f;
		private int length_seconds = -1;
		private int audio_channels = 0;

		public FilterMPlayerVideo ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/dl"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/dv"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/fli"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/gl"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/mpeg"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/mp4"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/quicktime"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/mp4v-es"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/parityfec"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/pointer"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.fvt"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.motorola.video"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.motorola.videop"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.mpegurl"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.mts"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.nokia.interleaved-multimedia"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/vnd.vivo"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-la-asf"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-mng"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-ms-asf"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-ms-wm"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-ms-wmv"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-ms-wmx"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-ms-wvx"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-msvideo"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("video/x-sgi-movie"));
			
			PreLoad = false;
		}
		
		private string AspectString(float aspect) {
			// work out what the human form of the aspect ratio is (e.g 16x9)
			float rounded_aspect = (float) Math.Round (aspect, 3);
				
			// FIXME: comparing floats is a bit dodgy, but i round the input
			// so it might be ok
			if (rounded_aspect == 1.778f)
				return "16x9";
			else if (rounded_aspect == 1.6f)
				return "16x10";
			else if (rounded_aspect == 1.333f)
				return "4x3";
			else if (rounded_aspect == 1.25f)
				return "5x4";
			else if (rounded_aspect == 0.0f)
				return "unknown";
			else
				return "other";
		}

		protected override void DoPullProperties ()
		{
			// create new external process
			Process pc = new Process ();
			pc.StartInfo.FileName = "mplayer";
			pc.StartInfo.Arguments = " -vo dummy -identify \"" + FileInfo.FullName+"\"";
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.RedirectStandardError = true;
			pc.StartInfo.UseShellExecute = false;
			
			try {
				pc.Start ();
			} catch (System.ComponentModel.Win32Exception) {
				Log.Warn ("Error: mplayer not found or unable to run");
				Error ();
				return;
			}

			StreamReader pout = pc.StandardOutput;
			string str;
			string name = "";
			
			while ((str = pout.ReadLine ()) != null) {
				if (!str.StartsWith ("ID_"))
					continue;

				string[] tokens = str.Split ('=');

				if (tokens.Length != 2)
					continue;

				if (str.StartsWith ("ID_CLIP_INFO_NAME"))
					name = tokens [1].ToLower ();
				else if (str.StartsWith ("ID_CLIP_INFO_VALUE")) {
					switch (name) {

					case "":
						break;

					case "name":
						AddProperty (Beagle.Property.NewKeyword ("dc:title", tokens [1]));
						break;

					case "language":
						AddProperty (Beagle.Property.NewKeyword ("dc:language", tokens [1]));
						break;

					case "copyright":
						AddProperty (Beagle.Property.NewKeyword ("dc:copyright", tokens [1]));
						break;

					case "comments":
						AddProperty (Beagle.Property.NewKeyword ("dc:description", tokens [1]));
						break;

					default:
						AddProperty (Beagle.Property.NewKeyword ("fixme:info:" + name, tokens [1]));
						break;
					}
				} else {
					switch (tokens [0]) {

					case "ID_VIDEO_WIDTH":
						width = Convert.ToInt32 (tokens [1]);
						break;

					case "ID_VIDEO_HEIGHT":
						height = Convert.ToInt32 (tokens [1]);
						break;

					case "ID_VIDEO_ASPECT":
						aspect = Convert.ToSingle (tokens [1]);
						break;

					case "ID_VIDEO_FPS":
						fps = Convert.ToSingle (tokens [1]);
						break;

					case "ID_VIDEO_FORMAT":
						AddProperty (Beagle.Property.NewKeyword ("fixme:video:codec", tokens [1]));
						break;

					case "ID_LENGTH":
						length_seconds = (int) Convert.ToSingle (tokens [1]);
						break;

					case "ID_AUDIO_NCH":
						audio_channels = Convert.ToInt32 (tokens [1]);
						break;

					case "ID_AUDIO_CODEC":
						AddProperty (Beagle.Property.NewKeyword ("fixme:audio:codec", tokens [1]));
						break;

					case "ID_DEMUXER":
						AddProperty (Beagle.Property.NewKeyword ("fixme:video:container", tokens [1]));
						break;
					}
				}
			}
			
			pout.Close ();
			pc.WaitForExit ();
			pc.Close ();
			
			// If an aspect ratio wasn't set in the file then work out the
			// pixel aspect ratio
			if (aspect <= 0.0f) {
				if (width > 0 && height > 0)
					aspect = (float) width / (float) height;
			}
			
			if (aspect <= 0.0f)
				AddProperty (Beagle.Property.NewKeyword ("fixme:video:aspect", aspect));

			AddProperty (Beagle.Property.NewKeyword ("fixme:video:aspect", AspectString (aspect)));

			if (width > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:video:width", width));

			if (height > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:video:height", height));	
			
			if (fps > 0.0f)
				AddProperty (Beagle.Property.NewKeyword ("fixme:video:fps", fps));
			
			if (length_seconds > 0)
				AddProperty (Beagle.Property.NewKeyword ("dc:extent", length_seconds));
			
			switch (audio_channels) {

			case 0:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "none"));
				break;
			case 1:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "mono"));
				break;
			case 2:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "stereo"));
				break;
			case 5:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "4.1"));
				break;
			case 6:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "5.1"));
				break;
			case 7:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "6.1"));
				break;
			case 8:
				AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channel_setup", "7.1"));
				break;
			}
			
			AddProperty (Beagle.Property.NewKeyword ("fixme:audio:channels", audio_channels));

			Finished ();
		}
	}
}

