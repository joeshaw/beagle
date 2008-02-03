//
// Ontologies.cs : Translate between Xesam and Beagle ontologies
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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
using System.Collections.Generic;

namespace Beagle {
	namespace Xesam {
		class Ontologies {
			private static Dictionary<string, string> fields_mapping;
			private static Dictionary<string, string> sources_mapping;
			private static Dictionary<string, string> contents_mapping;
			private static string[] fields_supported = null;
			private static string[] sources_supported = null;
			private static string[] contents_supported = null;

			static Ontologies ()
			{
				InitializeFieldsMapping ();
				InitializeSourcesMapping ();
				InitializeContentsMapping ();
			}

			private static void InitializeFieldsMapping ()
			{
				fields_mapping = new Dictionary<string, string> ();

				fields_mapping.Add ("dc:title", "title");
				fields_mapping.Add ("xesam:title", "title");

				fields_mapping.Add ("dc:author", "author");
				fields_mapping.Add ("xesam:author", "author");

				fields_mapping.Add ("dc:creator", "creator");
				fields_mapping.Add ("xesam:creator", "creator");

				fields_mapping.Add ("dc:date", "date");

				fields_mapping.Add ("xesam:width", "fixme:width");
				fields_mapping.Add ("xesam:height", "fixme:height");

				fields_mapping.Add ("xesam:pageCount", "fixme:page-count");

				fields_mapping.Add ("mime", "mimetype");
				fields_mapping.Add ("xesam:mimeType", "mimetype");

				fields_mapping.Add ("uri", "uri");
				fields_mapping.Add ("url", "uri");
				fields_mapping.Add ("xesam:url", "uri");

				fields_mapping.Add ("xesam:fileExtension", "beagle:FilenameExtension");
				fields_mapping.Add ("fileExtension", "beagle:FilenameExtension");

				fields_mapping.Add ("snippet", "snippet");
			}

			private static void InitializeSourcesMapping ()
			{
				sources_mapping = new Dictionary<string, string> ();

				sources_mapping.Add ("xesam:ArchivedFile", "filetype:archive");
				sources_mapping.Add ("xesam:File", "type:File");
				sources_mapping.Add ("xesam:MessageboxMessage","type:MailMessage");
			}

			private static void InitializeContentsMapping ()
			{
				contents_mapping = new Dictionary<string, string> ();

				contents_mapping.Add ("xesam:Archive", "filetype:archive");
				contents_mapping.Add ("xesam:Audio", "filetype:audio");
				contents_mapping.Add ("xesam:Bookmark", "type:Bookmark");
				contents_mapping.Add ("xesam:Contact", "type:Contact");
				contents_mapping.Add ("xesam:Document", "( filetype:document or filetype:documentation )");
				contents_mapping.Add ("xesam:Documentation", "filetype:documentation");
				contents_mapping.Add ("xesam:Email", "type:MailMessage");
				contents_mapping.Add ("xesam:IMMessage", "type:IMLog");
				contents_mapping.Add ("xesam:Image", "filetype:image");
				contents_mapping.Add ("xesam:Media", "( filetype:audio or filetype:video )");
				contents_mapping.Add ("xesam:Message", "( type:MailMessage or type:IMLog )");
				contents_mapping.Add ("xesam:RSSMessage", "type:FeedItem");
				contents_mapping.Add ("xesam:SourceCode", "filetype:source");
				contents_mapping.Add ("xesam:TextDocument", "filetype:document");
				contents_mapping.Add ("xesam:Video", "filetype:video");
				contents_mapping.Add ("xesam:Visual", "( filetype:image or filetype:video )");
				contents_mapping.Add ("xesam:Alarm", "type:Calendar");
				contents_mapping.Add ("xesam:Event", "type:Calendar");
				contents_mapping.Add ("xesam:FreeBusy", "type:Calendar");
				contents_mapping.Add ("xesam:Task", "type:Task");
			}

			public static string[] GetSupportedXesamFields ()
			{
				if (fields_supported == null) {
					List<string> ret = new List<string> ();

					foreach (string field in fields_mapping.Keys)
						ret.Add (field);

					fields_supported = ret.ToArray ();
				}

				return fields_supported;
			}

			public static string XesamToBeagleField (string xesamField) {
				if (fields_mapping.ContainsKey (xesamField))
					return fields_mapping [xesamField];
				else {
					Console.Error.WriteLine ("Unsupported field: {0}", xesamField);
					return xesamField.Replace (':', '-');
				}
			}

			public static string[] GetSupportedXesamSources ()
			{
				if (sources_supported == null) {
					List<string> ret = new List<string> ();

					foreach (string field in sources_mapping.Keys)
						ret.Add (field);

					sources_supported = ret.ToArray ();
				}

				return sources_supported;
			}

			public static string XesamToBeagleSource (string xesamSource)
			{
				if (sources_mapping.ContainsKey (xesamSource))
					return sources_mapping [xesamSource];
				else {
					Console.Error.WriteLine ("Unsupported source: {0}", xesamSource);
					return String.Empty;
				}
			}


			public static string[] GetSupportedXesamContents ()
			{
				if (contents_supported == null) {
					List<string> ret = new List<string> ();

					foreach (string field in contents_mapping.Keys)
						ret.Add (field);

					contents_supported = ret.ToArray ();
				}

				return contents_supported;
			}

			public static string XesamToBeagleContent (string xesamContent)
			{
				if (contents_mapping.ContainsKey (xesamContent))
					return contents_mapping [xesamContent];
				else {
					Console.Error.WriteLine ("Unsupported content type: {0}", xesamContent);
					return String.Empty;
				}
			}
		}
	}
}
