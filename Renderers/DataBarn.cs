//
// DataBarn.cs
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Reflection;

using BU = Beagle.Util;

namespace Beagle {

	public class DataBarn {

		// Don't let anyone instantiate a DataBarn.
		private DataBarn () { }

		static public Stream GetStream (string name)
		{
			if (name.StartsWith ("file://")) {
				string path = name.Substring ("file://".Length);

				if (! File.Exists (path)) {
					Console.WriteLine ("Can't get file '{0}'", path);
					return null;
				}
				
				return new FileStream (path, FileMode.Open, FileAccess.Read);
			}

			if (name.StartsWith ("mime-icon:")) {
				string mimeType = name.Substring ("mime-icon:".Length);
				Gtk.IconSize size = (Gtk.IconSize) 48;
				string path = BU.GnomeIconLookup.LookupMimeIcon (mimeType, size);
				return new FileStream (path, FileMode.Open, FileAccess.Read);
			}

			// Otherwise, assume the data is attached to the
			// assembly as a resource.

			Assembly assembly = Assembly.GetExecutingAssembly ();
			Stream s = assembly.GetManifestResourceStream (name);
			if (s == null)
				Console.WriteLine ("Couldn't get resource '{0}'", name);
			return s;
		}

		static public StreamReader GetText (string name)
		{
			// FIXME: could try to sanity check this by name,
			// warning if (for example) you tried to get a .png file
			// as text.
			Stream s = GetStream (name);
			return s != null ? new StreamReader (s) : null;
		}

		static public Gdk.Pixbuf GetPixbuf (string name)
		{
			Stream s = GetStream (name);
			return s != null ? new Gdk.Pixbuf (s) : null;
		}

		static public Gtk.Widget GetImageWidget (string name)
		{
			Gdk.Pixbuf pixbuf = GetPixbuf (name);
			return pixbuf != null ? new Gtk.Image (pixbuf) : null;
		}
	}
}
