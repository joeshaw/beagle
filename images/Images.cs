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
			} else {
				Assembly assembly = Assembly.GetExecutingAssembly ();
				stream = assembly.GetManifestResourceStream (name);
			}

			return stream;
		}

		static public Stream GetStream (string name)
		{
			Stream stream;

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
	}
}
