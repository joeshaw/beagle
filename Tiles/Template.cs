//
// Template.cs
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

using System.IO;
using System;
using System.Collections;
using System.Reflection;
using BU = Beagle.Util;

namespace Beagle.Tile {

	public class Template {

		private ArrayList lines = new ArrayList ();

		private bool dirty = true;
		private string string_html;

		private Hashtable values = new Hashtable ();
		
		private void Setup (Stream stream)
		{
			StreamReader sr = new StreamReader (stream);
			string line;
			while ((line = sr.ReadLine ()) != null)
				lines.Add (line);
			dirty = true;
		}	       

		public Template (string template_resource)
		{
			// We look for the resource in the assembly that contains
			// the type's definition.
			Assembly assembly = Assembly.GetAssembly (this.GetType ());
			Stream stream = assembly.GetManifestResourceStream (template_resource);
			
			if (stream == null)
				throw new Exception (String.Format ("No such resource: {0}", template_resource));
			Setup (stream);
		}

		public Template (Stream stream)
		{
			Setup (stream);
		}

		public void AddValues (IDictionary values)
		{
			foreach (string key in values.Keys)
				this.values[key] = values[key];
			dirty = true;
		}

		public void AddHit (Hit hit)
		{
			values["Uri"] = hit.Uri.ToString ();
			values["MimeType"] = hit.MimeType;
			values["Source"] = hit.Source;
			values["Score"] = hit.Score.ToString ();
			values["ScoreRaw"] = hit.ScoreRaw.ToString ();
			values["ScoreMultiplier"] = hit.ScoreMultiplier.ToString ();
			values["Revision"] = hit.Revision.ToString ();
			values["Timestamp"] = BU.StringFu.DateTimeToString (hit.Timestamp);
			values["Path"] = hit.Path;
			values["FileName"] = hit.FileName;
			values["DirectoryName"] = hit.DirectoryName;
			if (hit.FileInfo != null) 
				values["FolderName"] = hit.FileInfo.Directory.Name;
			else if (hit.DirectoryInfo != null)
				values["FolderName"] = hit.DirectoryInfo.Parent.Name;
			AddValues (hit.Properties);
		}

		public IDictionary Values {
			get { return values; }
		}

		public string this [string key] {
			get { return (string) values[key]; }
			set {
				if (value == null) {
					if (values.Contains (key)) 
						values.Remove (key);
					return;
				}
				values[key] = value as string;
				dirty = true;
			}
		}

		private string GetValue (string key)
		{
			int pos;
			string formatter;
			string subkey;

			pos = key.IndexOf ('%');
			
			if (pos >= 0) {
				formatter = key.Substring (0, pos);
				subkey = key.Substring (pos + 1);
			} else {
				formatter = null;
				subkey = key;
			}

			switch (formatter) {
			case "" :
				if (values.Contains (subkey)) 
					return (string)values[subkey];
				else 
					return null;
			case "image" :
				return Images.GetHtmlSource (subkey, "image/png"); // FIXME: other mime types 
			case "stock" :
				string path = BU.Icon.LookupName (subkey, 16);
				if (path != null) {
					string mime = BU.GnomeIconLookup.GetFileMimeType (path);
					if (mime != null && mime != "") 
						return Images.GetHtmlSource 
							(path, mime);
				}

				return null;
			case "date" :
				try {
					return BU.StringFu.DateTimeToPrettyString (BU.StringFu.StringToDateTime ((string)values [subkey]));
				} catch {
					return (string)values[subkey];
				}
			default :
				if (values.Contains (key))
					return BU.StringFu.EscapeStringForHtml ((string)values[subkey], false);
				else
					return null;
			}
		}

		private string Build ()
		{
			StringWriter writer = new StringWriter ();
			foreach (string line in lines) {
				string new_line = "";
				if (line.Length < 1) {
					writer.WriteLine ("");
					continue;
				}

				string [] fragments = line.Split ('@');
				for (int i = 0; i < fragments.Length;  i++) {
					if (i % 2 == 1) {
						string value = GetValue (fragments[i]);
						if (value == null) {
							new_line = "";
							break;
						}
						new_line = new_line + value;
					} else {
						new_line = new_line + fragments[i];
					}
				}
				if (new_line != "") 
					writer.WriteLine (new_line + "\n");
			}

			return writer.ToString ();
		}

		public override string ToString ()
		{
			if (dirty) {
				string_html = Build ();
			}

			return string_html;
		} 
	}
}        
