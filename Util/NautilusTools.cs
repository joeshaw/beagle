//
// NautilusTools.cs
//
// Copyright (C) 2004 Joe Gasiorek
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
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Beagle.Util {

	public class NautilusTools {

		private NautilusTools () { } // This class is static
		
		[DllImport ("libgnomeui-2")]
			extern static string gnome_vfs_escape_string (string uri);
			
		public static string GetEmblem (string path) {

			// Allow us to pass in file:// Uris.
			if (path.StartsWith ("file://"))
				path = path.Substring ("file://".Length);
			
			FileInfo info = new FileInfo (path);
			StringBuilder newpath = new StringBuilder (Environment.GetEnvironmentVariable ("HOME"));
			newpath.Append ("/.nautilus/metafiles/file:%2F%2F");
		
			path = Path.GetDirectoryName (path);
			path = gnome_vfs_escape_string (path);
		
			newpath.Append (path);
			newpath.Append (".xml");
				
			XmlDocument doc = new XmlDocument ();
			StreamReader sr = null;
			//Console.WriteLine ("FILE:  {0}", newpath.ToString ());
			try {
				sr = new StreamReader(newpath.ToString ());
				doc.Load (sr);
				} catch (Exception e) {
					//Console.WriteLine ("Nautlius: {0}, {1}", newpath.ToString (), e.Message);
					return null;
			}
			XmlNodeList nodes = doc.SelectNodes ("/directory/file");
			foreach (XmlNode node in nodes) {
				//Console.WriteLine ("{0} :: {1}", info.Name, node.Attributes[0].Value);	
				if (info.Name.CompareTo (node.Attributes[0].Value) == 0) {
					if (node.FirstChild.Name.CompareTo("keyword") == 0) {
						//Console.WriteLine ("{0}", node.FirstChild.Attributes[0].Value);
						return node.FirstChild.Attributes[0].Value;
					}
				}
			}
			return null;
	 	}
	 	
	}
}
