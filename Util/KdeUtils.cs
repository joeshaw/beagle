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
		private static string kde_directory = String.Empty;

		// Returns the kde prefix directory, or null if kde-config
		// could not be found.
		public static string KdePrefix {
			get {
				if (kde_directory != String.Empty)
					return kde_directory;

				Process kde_config = new Process ();
				kde_config.StartInfo.FileName = "kde-config";
				kde_config.StartInfo.Arguments = "--prefix";
				kde_config.StartInfo.RedirectStandardInput = false;
				kde_config.StartInfo.RedirectStandardOutput = true;
				kde_config.StartInfo.UseShellExecute = false;

				try {
					kde_config.Start ();
				} catch (System.ComponentModel.Win32Exception) {
					Logger.Log.Info ("Couldn't find kde-config, not dealing with KDE-related info.");
					kde_directory = null;
					return kde_directory;
				}

				StreamReader pout = kde_config.StandardOutput;
				kde_directory = pout.ReadLine ();
				pout.Close ();
	
				// FIXME: Remove WaitForExit call once the zombie process bug (74870) is fixed
				kde_config.WaitForExit ();
				kde_config.Close ();

				if (! Directory.Exists (kde_directory))
					kde_directory = null;

				return kde_directory;
			}
		}

		public static string KdeSharePrefix {
			get { return Path.Combine (KdePrefix, "share"); }
		}

		// Finds an icon by its name and returns its absolute path, or null if not found.
		public static string LookupIcon (string icon_name) {
			string icon_prefix = Path.Combine (KdeSharePrefix, "icons");
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
				return null;

			// Loop through all detected themes
			foreach (string theme in icon_themes) {
				if (theme == null)
					continue;

				// Try the preset icon sizes
				foreach (string size in icon_sizes) {
					string icon_dir = Path.Combine (Path.Combine (theme, size), "apps");
					if (! Directory.Exists (icon_dir))
						continue;

					// Check for icon existance
					string icon_path = Path.Combine (icon_dir, icon_name);
					if (File.Exists (icon_path))
						return icon_path;
				}
			}

			return null;
		}

	}
}

