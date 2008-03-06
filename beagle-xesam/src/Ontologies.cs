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
			private static Dictionary<string, List<string>> fields_mapping;
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

			static void AddField (string xesam_field, string beagle_field)
			{
				if (!fields_mapping.ContainsKey (xesam_field))
					fields_mapping.Add (xesam_field, new List<string> ());

				// This is O(n), but probably worth it since 'n' is small
				if (!fields_mapping [xesam_field].Contains (beagle_field))
					fields_mapping [xesam_field].Add (beagle_field);
			}

			private static void InitializeFieldsMapping ()
			{
				fields_mapping = new Dictionary<string, List<string>> ();

				// General fields
				AddField ("dc:title", "dc:title");
				AddField ("xesam:title", "dc:title");

				// FIXME: "dc:author" is not valid Dublin Core
				AddField ("dc:author", "dc:author");
				AddField ("xesam:author", "dc:author");
				AddField ("xesam:author", "dc:creator");

				AddField ("dc:creator", "dc:creator");
				AddField ("xesam:creator", "dc:creator");

				AddField ("dc:date", "date");
				AddField ("xesam:sourceModified", "date");

				AddField ("mime", "mimetype");
				AddField ("xesam:mimeType", "mimetype");

				AddField ("uri", "uri");
				AddField ("url", "uri");
				AddField ("xesam:url", "uri");

				// File fields
				AddField ("xesam:name", "beagle:ExactFilename");
				AddField ("xesam:fileExtension", "beagle:FilenameExtension");
				AddField ("xesam:storageSize", "fixme:filesize");

				// Document fields
				AddField ("xesam:wordCount", "fixme:word-count");
				AddField ("xesam:pageCount", "fixme:page-count");

				// EMail fields
				AddField ("xesam:subject", "dc:title");
				AddField ("xesam:author", "fixme:from");
				AddField ("xesam:to", "fixme:to");
				AddField ("xesam:cc", "fixme:cc");
				AddField ("xesam:id", "fixme:msgid");
				AddField ("xesam:mailingList", "fixme:mlist");
				// FIXME: BCC fields seem to be ignored by the filters
				//AddField ("xesam:bcc", "fixme:bcc");

				// Image fields
				AddField ("xesam:width", "fixme:width");
				AddField ("xesam:height", "fixme:height");
				AddField ("xesam:pixelDataBitDepth", "fixme:depth");

				// Audio (and Video) fields
				AddField ("xesam:album", "fixme:album");
				AddField ("xesam:artist", "fixme:artist");
				AddField ("xesam:composer", "fixme:composer");
				AddField ("xesam:performer", "fixme:performer");
				AddField ("xesam:genre", "fixme:genre");
				AddField ("xesam:trackNumber", "fixme:tracknumber");
				AddField ("xesam:trackCount", "fixme:trackcount");
				AddField ("xesam:discNumber", "fixme:discnumber");
				// xesam doesn't have this yet
				//AddField ("xesam:discCount", "fixme:disccount");
				AddField ("xesam:audioBitrate", "fixme:bitrate");
				AddField ("xesam:audioChannels", "fixme:channels");
				AddField ("xesam:audioSampleRate", "fixme:samplerate");
				AddField ("xesam:mediaDuration", "fixme:duration");
				AddField ("xesam:comment", "fixme:comment");

				// Video feilds
				AddField ("xesam:audioCodec", "fixme:audio:codec");
				AddField ("xesam:audioBitrate", "fixme:audio:bitrate");
				AddField ("xesam:audioSampleRate", "fixme:audio:samplerate");
				AddField ("xesam:videoCodec", "fixme:video:codec");
				AddField ("xesam:frameRate", "fixme:fps");
				AddField ("xesam:height", "fixme:video:height");
				AddField ("xesam:width", "fixme:video:width");

				// HTML fields
				// FIXME: This only works with HTML with <meta> tags. What about other sources?
				AddField ("xesam:generator", "meta:generator");
				AddField ("xesam:description", "meta:description");

				AddField ("xesam:remoteServer", "fixme:host");

				AddField ("snippet", "snippet");
			}

			private static void InitializeSourcesMapping ()
			{
				sources_mapping = new Dictionary<string, string> ();

				sources_mapping.Add ("xesam:ArchivedFile", "filetype:archive");
				sources_mapping.Add ("xesam:File", "type:File");
				sources_mapping.Add ("xesam:Filelike", "type:File");
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

			public static string[] XesamToBeagleField (string xesamField) {
				if (fields_mapping.ContainsKey (xesamField))
					return fields_mapping [xesamField].ToArray();
				else {
					Console.Error.WriteLine ("Unsupported field: {0}", xesamField);
					return new string[] { "property:" + xesamField };
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
