//
// NautilusTools.cs
//
// Copyright (C) 2004 Joe Gasiorek
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
using System.Text;
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Beagle.Util {

	public class NautilusTools {

		private class XmlDocCacheItem {
			public XmlDocument doc;
			public DateTime timestamp;
		}

		static private Hashtable cache = new Hashtable ();

		private NautilusTools () { } // This class is static

		static private string GetMetaFileName (string path)
		{
			string nautilusDir = Environment.GetEnvironmentVariable ("HOME") +
				"/.nautilus/metafiles/file:%2F%2F";

			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);
			path = Path.GetDirectoryName (Path.GetFullPath (path));
			path = path.Replace ("/", "%2F");

			string name = nautilusDir + path + ".xml";

			// If the filename is too long, ignore it.
			if (Path.GetFileName (name).Length > 255)
				return null;

			return File.Exists (name) ? name : null;
		}

		static public DateTime GetMetaFileTime (string path)
		{
			path = GetMetaFileName (path);
			return path != null ? File.GetLastWriteTime (path) : new DateTime ();
		}

		static private XmlNode GetMetaFileNode (string path)
		{
			string metaFile = GetMetaFileName (path);
			if (metaFile == null)
				return null;

			DateTime lastWrite = File.GetLastWriteTime (metaFile);

			string name = Path.GetFileName (path);

			XmlDocCacheItem cached = (XmlDocCacheItem) cache [metaFile];
			XmlDocument doc;
			if (cached == null || lastWrite > cached.timestamp) {
				doc = new XmlDocument ();
				doc.Load (new StreamReader (metaFile));

				cached = new XmlDocCacheItem ();
				cached.doc = doc;
				cached.timestamp = lastWrite;
				cache [metaFile] = cached;

			} else {
				doc = cached.doc;
			}

			string xpath = String.Format ("/directory/file[@name=\"{0}\"]", StringFu.HexEscape (name));
			return doc.SelectSingleNode (xpath);
		}

		static public string GetEmblem (string path)
		{
			XmlNode node = GetMetaFileNode (path);
			if (node == null)
				return null;
			XmlNode subnode = node.SelectSingleNode ("keyword");
			if (subnode == null)
				return null;

			XmlNode attr = subnode.Attributes.GetNamedItem ("name");
			return attr != null ? attr.Value : null;
		}

		static public string GetNotes (string path)
		{
			XmlNode node = GetMetaFileNode (path);
			if (node == null)
				return null;
			XmlNode attr = node.Attributes.GetNamedItem ("annotation");
			return attr != null ? attr.Value : null;
		}
	}
}
