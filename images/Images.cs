//
// Images.cs
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
using System.Reflection;
using Beagle.Util;

using Gnome;

namespace Beagle {
	
	public class Images {

		// This class is fully static.
		private Images () { }

		static private Stream GetStreamInner (string name)
		{
			Stream stream = null;

			if (name.StartsWith ("file://")) {
				name = name.Substring ("file://".Length);
				if (File.Exists (name))
					stream = File.OpenRead (name);
			} else if (name.StartsWith ("/")) {
				if (File.Exists (name))
					stream = File.OpenRead (name);
			} else {
				Assembly assembly = Assembly.GetExecutingAssembly ();
				stream = assembly.GetManifestResourceStream (name);
			}

			return stream;
		}

		static public Stream GetStream (string name)
		{
			Stream stream;

			if (name == null || name.Length == 0)
				return null;

			stream = GetStreamInner (name);
			if (stream == null)
				stream = GetStreamInner (name + ".png");
			if (stream == null)
				stream = GetStreamInner (name + ".jpg");
			if (stream == null)
				Console.WriteLine ("Couldn't get stream for image '{0}'", name);
			return stream;
		}

		static public Gdk.Pixbuf GetPixbuf (string name)
		{
			Stream s = GetStream (name);
			return s != null ? new Gdk.Pixbuf (s) : null;
		}

		static public Gdk.Pixbuf GetPixbuf (string name, int maxWidth, int maxHeight)
		{
			Gdk.Pixbuf pixbuf = GetPixbuf (name);
			if (pixbuf == null)
				return null;
			
			double scaleWidth = maxWidth / (double)pixbuf.Width;
			double scaleHeight = maxHeight / (double)pixbuf.Height;

			double s = Math.Min (scaleWidth, scaleHeight);
			if (s >= 1.0)
				return pixbuf;

			int w = (int) Math.Round (s * pixbuf.Width);
			int h = (int) Math.Round (s * pixbuf.Height);
			
			return pixbuf.ScaleSimple (w, h, Gdk.InterpType.Bilinear);
		}

		static public Gtk.Widget GetWidget (string name)
		{
			Gdk.Pixbuf pixbuf = GetPixbuf (name);
			return pixbuf != null ? new Gtk.Image (pixbuf) : null;
		}
		
		static public Gtk.Widget GetWidget (string name, int maxWidth, int maxHeight)
		{
			Gdk.Pixbuf pixbuf = GetPixbuf (name, maxWidth, maxHeight);
			return pixbuf != null ? new Gtk.Image (pixbuf) : null;
		}

		static public string GetHtmlSourceForStock (string stockid,
							    int size)
		{
			int base_size;
			IconTheme icon_theme = new IconTheme ();
			string path = icon_theme.LookupIcon (stockid, size, IconData.Zero, out base_size);

			if (path != null && path != "") {
				return "file://" + path;
			}
			return null;
		}

		static public string GetHtmlSource (byte[] binary_data, 
						    string mime_type) 
		{
			string base64_string = 
				System.Convert.ToBase64String(binary_data, 
							      0,
							      binary_data.Length);
			
			string data = "data:" + mime_type + ";base64," + base64_string;

			return data;
		}

		static public string GetHtmlSource (string name, 
						    string mime_type) 
		{
			if (name == null || name.Length == 0) {
				return null;
			} else if (name.StartsWith ("file://")) {
				return name;
			} else if (name.StartsWith ("/")) {
				return StringFu.PathToQuotedFileUri (name);
			} else {
				Stream stream;

				if (mime_type == null || mime_type == "")
					throw new ArgumentException ();

				// FIXME: it's probably worth caching these,
				// since they'll probably be repeated a lot

				stream = GetStream (name);
				if (stream == null)
					return null;

				byte[] binary_data = new Byte[stream.Length];
				stream.Read(binary_data, 0, (int) stream.Length);
				stream.Close ();
				return GetHtmlSource (binary_data, mime_type);
			}
		} 
	}
}
