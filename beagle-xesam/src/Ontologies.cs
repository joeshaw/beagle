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

namespace Beagle {
	namespace Xesam {
		class Ontologies {
			public static string XesamToBeagleField (string xesamField) {
				switch (xesamField) {
				case "dc:title":
					goto case "xesam:title";
				case "xesam:title":
					return "title";

				case "dc:author":
					goto case "xesam:author";
				case "xesam:author":
					return "author";

				case "dc:creator":
					goto case "xesam:creator";
				case "xesam:creator":
					return "creator";

				case "dc:date":
					return "date";

				case "mime":
					goto case "xesam:mimeType";
				case "xesam:mimeType":
					return "mimetype";

				case "url":
					goto case "xesam:url";
				case "uri":
					goto case "xesam:url";
				case "xesam:url":
					return "uri";
					
				    case "xesam:fileExtension":
					    goto case "fileExtension";
				    case "fileExtension":
				        return "beagle:FilenameExtension";

				default:
					Console.Error.WriteLine ("Unsupported field: {0}", xesamField);
					return xesamField.Replace (':', '-');
				}
			}

			public static string XesamToBeagleSource (string xesamSource)
			{
				// Note: If you change stuff there, you might need to change the set of
				// supported sources in Searcher.cs
				switch (xesamSource) {
				case "xesam:ArchivedFile":
					return "filetype:archive";
				case "xesam:File":
					return "type:File";
				case "xesam:MessageboxMessage":
					return "type:MailMessage";

				default:
					Console.Error.WriteLine ("Unsupported source: {0}", xesamSource);
					return String.Empty;
				}
			}

			public static string XesamToBeagleContent (string xesamContent)
			{
				// Note: If you change stuff there, you might need to change the set of
				// supported contents in Searcher.cs
				switch (xesamContent) {
				case "xesam:Archive":
					return "filetype:archive";
				case "xesam:Audio":
					return "filetype:audio";
				case "xesam:Bookmark":
					return "type:Bookmark";
				case "xesam:Contact":
					return "type:Contact";
				case "xesam:Document":
					return "( filetype:document or filetype:documentation )";
				case "xesam:Documentation":
					return "filetype:documentation";
				case "xesam:Email":
					return "type:MailMessage";
				case "xesam:IMMessage":
					return "type:IMLog";
				case "xesam:Image":
					return "filetype:image";
				case "xesam:Media":
					return "( filetype:audio or filetype:video )";
				case "xesam:Message":
					return "( type:MailMessage or type:IMLog )";
				case "xesam:RSSMessage":
					return "type:FeedItem";
				case "xesam:SourceCode":
					return "filetype:source";
				case "xesam:TextDocument":
					return "filetype:document";
				case "xesam:Video":
					return "filetype:video";
				case "xesam:Visual":
					return "( filetype:image or filetype:video )";
				case "xesam:Alarm":
				case "xesam:Event":
				case "xesam:FreeBusy":
					return "type:Calendar";
				case "xesam:Task":
					return "type:Task";

				default:
					Console.Error.WriteLine ("Unsupported content type: {0}", xesamContent);
					return String.Empty;
				}
			}
		}
	}
}
