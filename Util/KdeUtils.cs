//
// KdeUtils.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Diagnostics;
using System.IO;
using Mono.Posix;

namespace Beagle.Util {
	public static class KdeUtils {

		private static string [] icon_sizes = { "128x128", "64x64", "48x48", "32x32", "22x22", "16x16" };
		private static string [] kde_locations = { ExternalStringsHack.KdePrefix, Environment.GetEnvironmentVariable ("KDEDIR"), "/opt/kde3", "/usr" };
		public static string [] KdeLocations {
			get { return kde_locations; }
		}

		// Finds an icon by its name and returns its absolute path, or null if not found.
		public static string LookupIcon (string icon_name) {
			foreach (string kde_dir in KdeLocations) {
				if (kde_dir == null || kde_dir == String.Empty || !Directory.Exists (kde_dir))
					continue;

				string kde_share = Path.Combine (kde_dir, "share");
				string icon_prefix = Path.Combine (kde_share, "icons");
				string icon_theme_hicolor = Path.Combine (icon_prefix, "hicolor");
				string [] icon_themes = { null, null };

				if (! Directory.Exists (icon_theme_hicolor))
					icon_theme_hicolor = null;

				// FIXME: We should probably support svg icons at some point
				if (! icon_name.EndsWith(".png"))
					icon_name = icon_name + ".png";

				// We try up to 2 icon themes: we first try the theme pointed at by the
				// "default.kde" link, and then we try the trusted default "hicolor" theme.
				// We handle the situations if either (or both) of these aren't present, or
				// if default.kde == hicolor.

				string icon_theme_default = Syscall.readlink (Path.Combine (icon_prefix, "default.kde"));
				if (icon_theme_default != null) {
					if (! icon_theme_default.StartsWith ("/"))
						icon_theme_default = Path.Combine (icon_prefix, icon_theme_default);

					if (! Directory.Exists (icon_theme_default) || icon_theme_default == icon_theme_hicolor)
						icon_theme_default = null;
				}

				int i = 0;
				if (icon_theme_default != null)
					icon_themes [i++] = icon_theme_default;
				if (icon_theme_hicolor != null)
					icon_themes [i++] = icon_theme_hicolor;
				if (i == 0)
					continue;

				// Loop through all detected themes
				foreach (string theme in icon_themes) {
					if (theme == null)
						continue;

					// Try the preset icon sizes
					foreach (string size in icon_sizes) {
						string icon_base = Path.Combine (theme, size);
						if (! Directory.Exists (icon_base))
							continue;

						foreach (string icon_subdir in Directory.GetDirectories (icon_base)) {
							string icon_dir = Path.Combine (icon_base, icon_subdir);

							// Check for icon existance
							string icon_path = Path.Combine (icon_dir, icon_name);
							if (File.Exists (icon_path))
								return icon_path;
						}
					}
				}
				// Only search the first valid path that we find
				break; 
			}
			return null;
		}

	}
}

