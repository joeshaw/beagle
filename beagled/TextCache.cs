//
// TextCache.cs
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
using System.Collections;
using System.IO;

using Beagle.Util;

namespace Beagle.Daemon {

	public class TextCache {

		const string filename_prefix = "00_";
		static string path_prefix;

		static TextCache () 
		{
			path_prefix = Path.Combine (PathFinder.RootDir, "TextCache");
			if (! Directory.Exists (path_prefix))
				Directory.CreateDirectory (path_prefix);

			// Create our checksum directories.
			for (int i = 0; i < 256; ++i) {
				string subdir = Path.Combine (path_prefix, i.ToString ());
				if (! Directory.Exists (subdir))
					Directory.CreateDirectory (subdir);
			}
		}

		private static string UriToFilename (Uri uri)
		{
			string name = uri.ToString ();
			name = name.Replace ("/", "%2F");
			return filename_prefix + name;
		}

		private static uint StringToChecksum (string str)
		{
			int N = str.Length;
			uint checksum = 1;
			for (int i = 0; i < N; ++i)
				checksum = 59 * checksum + (uint) str [i];
			// xor the uint's four byte together
			checksum = (checksum & 0xff) ^ (checksum >> 8);
			checksum = (checksum & 0xff) ^ (checksum >> 8);
			checksum = (checksum & 0xff) ^ (checksum >> 8);
			checksum = checksum & 0xff;
			return checksum;
		}

		private static string UriToPath (Uri uri)
		{
			string name = UriToFilename (uri);
			uint checksum = StringToChecksum (name);
			return Path.Combine (path_prefix, Path.Combine (checksum.ToString (), name));
		}

		public static TextReader GetReader (Uri uri)
		{
			string path = UriToPath (uri);

			FileStream stream;
			try {
				stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (FileNotFoundException ex) {
				return null;
			}
			
			StreamReader reader;
			reader = new StreamReader (stream);
			return reader;
		}

		public static TextWriter GetWriter (Uri uri)
		{
			string path = UriToPath (uri);

			FileStream stream;
			stream = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.Read);

			StreamWriter writer;
			writer = new StreamWriter (stream);
			return writer;
		}

		public static void Delete (Uri uri)
		{
			string path = UriToPath (uri);
			File.Delete (path);
		}
	}
}
