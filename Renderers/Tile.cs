//
// Tile.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Text;

namespace Beagle {

	public class Tile {

		private Hit hit;
		private string templateName = null;
		private string html = null;

		public Tile (string templateName, Hit hit) 
		{
			this.templateName = templateName;
			this.hit = hit;
		}

		public Hit Hit {
			get { return hit; }
		}
		public string Html {
			get {
				if (html == null)
					html = BuildHtml (hit);
				return html;
			}
		}
		
		virtual protected string BuildHtml (Hit hit)
		{
			StringBuilder htmlBuilder = new StringBuilder ("");
			StreamReader sr = DataBarn.GetText (templateName);
			string line;
			
			while ((line = sr.ReadLine ()) != null)
				TransformLineOfHtml (line, htmlBuilder);
			
			return htmlBuilder.ToString ();
		}

		//
		// The ugly details of building the Html
		//

		private string FormatDate (DateTime dt)
		{
			TimeSpan age = DateTime.Now - dt;
			// FIXME: Saner date formatting
			if (age.TotalHours < 18)
				return dt.ToShortTimeString ();
			if (age.TotalDays < 180)
				return dt.ToString ("MMM d, h:mm tt");
			return dt.ToString ("MMM d, yyyy");
		}

		private string FormatFileLength (long len)
		{
			const long oneMb = 1024*1024;

			if (len < 0)
				return null;

			if (len < 1024)
				return String.Format ("{0} bytes", len);

			if (len < oneMb)
				return String.Format ("{0:0.0} kb", len/(double)1024);

			return String.Format ("{0:0.0} Mb", len/(double)oneMb);
		}

		private string ExpandKey (string key)
		{
			// This allows you to get a @ via @@
			if (key == "")
				return "@";

			string lowerKey = key.ToLower ();

			if (lowerKey.StartsWith ("file:") && ! hit.IsFile)
				return null;

			switch (lowerKey) {
			case "timestamp":
				return FormatDate (hit.Timestamp);
			case "uri":
				return hit.Uri;
			case "type":
				return hit.Type;
			case "mimetype":
				return hit.MimeType;
			case "source":
				return hit.Source;
			case "score":
				return hit.Score.ToString ();
			case "file:path":
				return hit.Path;
			case "file:filename":
				return hit.FileName;
			case "file:directoryname":
				return hit.DirectoryName;
			case "file:length":
				return FormatFileLength (hit.FileInfo.Length);
			case "file:creationtime":
				return FormatDate (hit.FileInfo.CreationTime);
			case "file:lastwritetime":
				return FormatDate (hit.FileInfo.LastWriteTime);
			}

			string val = hit [key];

			return val;
		}

		private void TransformLineOfHtml (string html, StringBuilder target)
		{
			StringBuilder newHtml = new StringBuilder ("");
			int i = 0;
			while (i < html.Length) {
				int j = html.IndexOf ('@', i);
				if (j == -1)
					break;
				int k = html.IndexOf ('@', j+1);
				if (k == -1)
					break;

				newHtml.Append (html.Substring (i, j-i));

				string key = html.Substring (j+1, k-j-1);
				string expansion = ExpandKey (key);
				// Drop lines w/ a failed expansion
				if (expansion == null)
					return;
				newHtml.Append (expansion);

				i = k+1;
			}

			if (i < html.Length)
				newHtml.Append (html.Substring (i));

			target.Append (newHtml.ToString ());
		}

	}

}
