//
// FilterSVG.cs
//
// Copyright (C) 2006 Alexander Macdonald <alex@alexmac.cc>
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
using System.Xml;
using System.Text;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {
	public class FilterSvg : Beagle.Daemon.Filter {
		private StringBuilder sb = new StringBuilder ();

		private enum RdfGrabModes {
			TitleMode,
			DescriptionMode,
			DateMode,
			Num
		};
		
		static private string [] rdf_grab_strings = { "title", "description", "date" };
		static private string [] rdf_nongrab_strings = { "creator", "contributor", "publisher", "rights" };

		// List of keys that should be ignored when adding to content.
		// For example, dc:format is the mime type, so it's not interesting text.
		static private string [] ignore_strings = { "format" };
		
		public FilterSvg ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("image/svg+xml"));
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".svg"));
		}

		override protected void DoPullProperties ()
		{
			XmlTextReader reader = new XmlTextReader (Stream);
			reader.XmlResolver = null;
			
			int depth = 0;
			bool grab_text = false, ignore_text = false;;
			string text = "";
			
			try {
				while (reader.Read ()) {
					switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (grab_text)
							break;
						
						if (ArrayFu.IndexOfString (ignore_strings, reader.LocalName) != -1)
							ignore_text = true;
						else if (reader.LocalName == "title" || reader.LocalName == "desc") {
							grab_text = true;
							depth = reader.Depth;
						} else if (reader.LocalName == "RDF")
							PullRdfProperties (reader, reader.Depth);

						break;

					case XmlNodeType.Text:
						text = reader.Value.Trim ();
						if (text.Length == 0 || ignore_text)
							break;
						
						if (grab_text) {
							sb.Append (text);
						} else {
							AppendText (text);
							AppendStructuralBreak ();
						}
						break;

					case XmlNodeType.Comment:
						AppendText (reader.Value.Trim ());
						AppendStructuralBreak ();
						break;

					case XmlNodeType.EndElement:
						if (! (grab_text && depth == reader.Depth))
							break;
						
						grab_text = false;
						ignore_text = false;

						if (reader.LocalName == "title") {
							AddProperty (Property.New ("dc:title", sb.ToString ()));
							sb.Length = 0;
						} else if (reader.LocalName == "desc") {
							AddProperty (Property.New ("dc:description", sb.ToString ()));
							sb.Length = 0;
						}
						break;
					}
				}
				
				Finished ();
			} catch (System.Xml.XmlException e) {
				Logger.Log.Error ("error parsing xml file {0}", FileInfo.FullName);
				Logger.Log.Debug (e);
				Error ();
			}
		}
		
		protected void PullRdfProperties (XmlTextReader reader, int depth)
		{
			int grab_mode = -1, nongrab_mode = -1;
			bool grab_text = false, ignore_text = false, agent_mode = false;

			string text = "";
			
			try {
				while (reader.Read ()) {
					if (depth == reader.Depth)
						return;			
		
					switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (grab_text)
							break;

						if (ArrayFu.IndexOfString (ignore_strings, reader.LocalName) != -1)
							ignore_text = true;
						else if (reader.LocalName == "Agent")
							grab_text = agent_mode = true;
						else {
							for (int i = 0; i < (int) RdfGrabModes.Num; i++) {
								if (reader.LocalName == rdf_grab_strings [i]) {
									grab_text = true;
									grab_mode = i;
									break;
								}
							}

							for (int i = 0; i < (int) RdfGrabModes.Num; i++) {
								if (reader.LocalName == rdf_nongrab_strings [i]) {
									grab_text = false;
									nongrab_mode = i;
									break;
								}
							}
						}
						break;

					case XmlNodeType.Text:
						text = reader.Value.Trim ();
						if (text.Length == 0 || ignore_text)
							break;
						
						if (grab_text) {
							sb.Append(text);
						} else {
							AppendText (text);
							AppendStructuralBreak ();
						}
						break;

					case XmlNodeType.EndElement:
						ignore_text = false;

						if (agent_mode) {
							if (reader.LocalName ==  "Agent") {
								agent_mode = grab_text = false;
								AddProperty (Property.New ("dc:" + rdf_nongrab_strings [nongrab_mode], sb.ToString ()));
								sb.Length = 0;
							}
						} else if (grab_mode >= 0 && reader.LocalName == rdf_grab_strings [grab_mode]) {
							if (grab_mode == (int) RdfGrabModes.DateMode) {
								try {
									AddProperty (Property.NewDate ("dc:date", System.Convert.ToDateTime (sb.ToString ())));
								} catch (FormatException) {
									AddProperty (Property.New ("dc:date", sb.ToString ()));
								}
							} else {
								AddProperty (Property.New ("dc:" + rdf_grab_strings [grab_mode], sb.ToString ()));
							}
							sb.Length = 0;
							grab_mode = -1;
							grab_text = false;
						} else if (nongrab_mode >= 0 && reader.LocalName == rdf_nongrab_strings [nongrab_mode]) {
							nongrab_mode = -1;
							grab_text = false;
						}
						break;
					}
				}
			} catch (System.Xml.XmlException e) {
				Logger.Log.Error ("error parsing embedded RDF {0}", FileInfo.FullName);
				Logger.Log.Debug (e);
				Error ();
			}
		}
	}
}
