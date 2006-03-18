//
// ImLog.cs
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
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Mono.Unix.Native;

namespace Beagle.Util {

	public enum ImClient {
		Gaim,
		Kopete,
	}

	public abstract class ImLog {

		public delegate void Sink (ImLog imLog);

		public string Client;
		public FileInfo File;
		public TextReader TextReader;
		public string Protocol;

		public DateTime StartTime;
		public DateTime EndTime;

		public string SpeakingTo;
		public string Identity;

		private Hashtable speakerHash = new Hashtable ();
		
		public class Utterance {
			private long timestamp;

			public DateTime Timestamp {
				get { return NativeConvert.ToDateTime (timestamp); }
				set { timestamp = NativeConvert.FromDateTime (value); }
			}
			
			public String Who;
			public String Text;
		}
		private ArrayList utterances = new ArrayList ();

		//////////////////////////

		protected ImLog (string client, FileInfo file, TextReader reader)
		{
			Client = client;
			TextReader = reader;
			File = file;
		}

		public ICollection Speakers {
			get { return speakerHash.Keys; }
		}

		public IList Utterances {
			get { return utterances; }
		}

		protected void AddUtterance (DateTime timestamp, string who, string text)
		{
			Utterance utt = new Utterance ();
			utt.Timestamp = timestamp;
			utt.Who = who;
			utt.Text = text.Trim ();

			if (StartTime.Ticks == 0 || StartTime > timestamp)
				StartTime = timestamp;

			if (EndTime.Ticks == 0 || EndTime < timestamp)
				EndTime = timestamp;

			speakerHash [who] = true;

			utterances.Add (utt);
		}

		protected void AppendToPreviousUtterance (string text)
		{
			if (utterances.Count > 0) {
				Utterance utt = (Utterance) utterances [utterances.Count - 1];
				utt.Text += "\n" + text;
			}
		}

		protected void ClearUtterances ()
		{
			utterances.Clear ();
		}

		protected abstract void Load ();
	}

	///////////////////////////////////////////////////////////////////////////////

	//
	// Gaim Logs
	//

	public class GaimLog : ImLog {

		public const string MimeType = "beagle/x-gaim-log";

		private static string StripTags (string line, StringBuilder builder)
		{
			int first = line.IndexOf ('<');
			if (first == -1)
				return line;
			
			builder.Length = 0;

			int i = 0;
			while (i < line.Length) {
				
				int j;
				if (first == -1) {
					j = line.IndexOf ('<', i);
				} else {
					j = first;
					first = -1;
				}
				
				int k = -1;
				if (j != -1) {
					k = line.IndexOf ('>', j);
					
					// If a "<" is unmatched, preserve it, and the
					// rest of the line
					if (k == -1)
						j = -1;
				}
				
				if (j == -1) {
					builder.Append (line, i, line.Length - i);
					break;
				}
				
				builder.Append (line, i, j-i);
				
				i = k+1;
			}
			
			return builder.ToString ();
		}

		///////////////////////////////////////

		public GaimLog (FileInfo file, TextReader reader) : base ("gaim", file, reader)
		{
			string filename = file.Name;

			// Parse what we can from the file path
			try {
				string str;

				// Character at position 17 will be either a dot, indicating the beginning
				// of the extension for old gaim logs, or a plus or minus indicating a
				// timezone offset for new gaim logs.
				if (filename [17] == '+' || filename [17] == '-') {
					// New gaim 2.0.0 format, including timezone.
					//
					// Ugly hack time: DateTime's format specifiers only know how to
					// deal with timezones in the format "+HH:mm" and not "+HHmm",
					// which is how UNIX traditionally encodes them.  I have no idea
					// why; it would make RFC 822/1123 parsing a hell of a lot easier.
					// Anyway, in this case, we're going to insert a colon in there so
					// that DateTime.ParseExact can understand it.
					//
					// 2006-02-21-160424-0500EST.html
					//                     ^
					//                     offset 20

					str = filename.Substring (0, 20) + ':' + filename.Substring (20, 2);
					StartTime = DateTime.ParseExact (str, "yyyy-MM-dd.HHmmsszzz", null);
				} else if (filename [17] == '.') {
					// Older gaim format.
					//
					// 2006-02-21-160424.html

					str = Path.GetFileNameWithoutExtension (filename);
					StartTime = DateTime.ParseExact (str, "yyyy-MM-dd.HHmmss", null);
				} else {
					throw new FormatException ();
				}
			} catch (Exception) {
				Logger.Log.Warn ("Could not parse date/time from filename '{0}'", file.Name);
				StartTime = DateTime.Now;
			}

			// Gaim likes to represent many characters in hex-escaped %xx form
			SpeakingTo = StringFu.HexUnescape (file.Directory.Name);
			Identity = StringFu.HexUnescape (file.Directory.Parent.Name);

			Protocol = file.Directory.Parent.Parent.Name;

			Load ();
		}

		// Return true if a new utterance is now available,
		// and false if the previous utterance was changed.
		private void ProcessLine (string line)
		{
			if (line.Length == 0)
				return;

			if (line [0] != '(') {
				AppendToPreviousUtterance (line);
				return;
			}
			int j = line.IndexOf (')');
			if (j == -1) {
				AppendToPreviousUtterance (line);
				return;
			}

			// Gaim 2.0 hack
			// The new version of Gaim adds AM or PM right after the time
			// 1.x: (19:07:07)
			// 2.0: (19:07:07 AM)
			// This is a nasty workaround :-)

			string whenStr = line.Substring (1, j-1);
			int hour = 0, minute, second;

			if (whenStr [whenStr.Length-1] == 'M') {
				// Handle AM and PM
				if (whenStr [whenStr.Length-2] == 'P')
					hour = 12;
								
				whenStr = line.Substring (1, j-4);
			}

			string[] whenSplit = whenStr.Split (':');
			try {
				hour += int.Parse (whenSplit [0]);
				minute = int.Parse (whenSplit [1]);
				second = int.Parse (whenSplit [2]);
			} catch {
				// If something goes wrong, this line probably
				// spills over from the previous one.
				AppendToPreviousUtterance (line);
				return;
			}

			line = line.Substring (j+2);

			// FIXME: this is wrong --- since we just get a time,
			// the date gets set to 'now'
			DateTime when = new DateTime (StartTime.Year,
						      StartTime.Month,
						      StartTime.Day,
						      hour, minute, second);
			
			// Try to deal with time wrapping around.
			while (when < EndTime)
				when = when.AddDays (1);

			int i = line.IndexOf (':');
			if (i == -1)
				return;
			string alias = line.Substring (0, i);
			string text = line.Substring (i+2);

			AddUtterance (when, alias, text);

			return;
		}

		protected override void Load ()
		{
			string line;

			ClearUtterances ();
			StringBuilder builder;
			builder = new StringBuilder ();

			line = TextReader.ReadLine (); // throw away first line
			if (line == null)
				return;

			// Could the second line ever start w/ < in a non-html log?
			// I hope not!
			bool isHtml = line.Length > 0 && line [0] == '<';
				
			while ((line = TextReader.ReadLine ()) != null) {
				if (isHtml)
					line = StripTags (line, builder);
			
				ProcessLine (line);
			}
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	//
	// Kopete Logs
	//
	public class KopeteLog : ImLog {

		public const string MimeType = "beagle/x-kopete-log";

		public KopeteLog (FileInfo file, TextReader reader) : base ("kopete", file, reader)
		{
			// FIXME: Artificially split logs into conversations depending on the
			// amount of time elapsed betweet messages?
			
			// Figure out the protocol from the parent.parent foldername
			Protocol = file.Directory.Parent.Name.Substring (0, file.Directory.Parent.Name.Length - 8).ToLower ().ToLower ();
			Identity = file.Directory.Name;

			// FIXME: This is not safe for all kinds of file/screennames
			string filename = Path.GetFileNameWithoutExtension (file.Name);
			SpeakingTo = filename.Substring (0, filename.LastIndexOf ('.'));

			Load ();
		}
		
		private const string date_format = "yyyy M d H:m:s";

		protected override void Load ()
		{
			ClearUtterances ();

			XmlReader reader;
			DateTime base_date = DateTime.MinValue;

			try {
				reader = new XmlTextReader (File.Open(
									     FileMode.Open,
									     FileAccess.Read,
									     FileShare.Read));
			} catch (Exception e) {
				Console.WriteLine ("Could not open '{0}'", File.FullName);
				Console.WriteLine (e);
				return;
			}
			
			while (reader.Read ()) {
				if (reader.NodeType != XmlNodeType.Element)
					continue;
				
				switch (reader.Name) {
				case "date":
					base_date = new DateTime (Convert.ToInt32 (reader.GetAttribute ("year")),
								  Convert.ToInt32 (reader.GetAttribute ("month")),
								  1);
					break;
					
				case "msg":
					// Parse the timestamp of the message
					string timestamp = String.Format ("{0} {1} {2}",
									  base_date.Year,
									  base_date.Month,
									  reader.GetAttribute ("time"));

					DateTime msg_date = DateTime.MinValue;

					try {
						msg_date = DateTime.ParseExact (timestamp,
										date_format,
										null);
					} catch (Exception ex) {
						Logger.Log.Error ("Couldn't parse Kopete timestamp: {0}", timestamp);
						break;
					}
					
					string who = reader.GetAttribute ("nick");
					if (who == null || who == "")
						who = reader.GetAttribute ("from");
					if (who == null || who == "")
						break;
					
					// Advance to the text node for the actual message
					reader.Read ();
					
					AddUtterance (msg_date, who, reader.Value);
					break;
				}
			}
			
			reader.Close ();
		}

	}
}

