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

namespace Beagle.Util {

	public abstract class ImLog {

		public delegate void Sink (ImLog imLog);

		private bool loaded = false;

		public string LogFile;
		public long   LogOffset;

		public string Protocol;

		public DateTime StartTime;
		public DateTime EndTime;
		public DateTime Timestamp;
		public string   Snippet;

		public string SpeakingTo;
		public string Identity;

		private Hashtable speakerHash = new Hashtable ();
		
		public class Utterance {
			public DateTime Timestamp;
			public String Who;
			public String Text;
		}
		private ArrayList utterances = new ArrayList ();

		//////////////////////////

		protected ImLog (string protocol, string file, long offset)
		{
			Protocol = protocol;
			LogFile = file;
			LogOffset = offset;
		}

		protected ImLog (string protocol, string file) : this (protocol, file, -1)
		{ }

		public Uri Uri {
			get {
				// Hacky
				string when = StartTime.ToString ("yyyy.MM.dd.HH.mm.ss");
				string uriStr = String.Format ("imlog://{0}/{1}/{2}/{3}",
							       Protocol, Identity, SpeakingTo,
							       when);
				return new Uri (uriStr, true);
			}							    
		}

		public string EllipsizedSnippet {
			get {
				string snippet = Snippet;
				// FIXME: We should try to avoid breaking mid-word
				if (snippet != null && snippet.Length > 50)
					snippet = snippet.Substring (0, 50) + "...";
				return snippet;
			}
		}

		public ICollection Speakers {
			get { return speakerHash.Keys; }
		}

		public IList Utterances {
			get { 
				if (! loaded) {
					Load ();
					loaded = true;
				}
				return utterances;
			}
		}

		protected IList RawUtterances {
			get { return utterances; }
		}

		protected void AddUtterance (DateTime timestamp,
					     String   who,
					     String   text)
		{
			Utterance utt = new Utterance ();
			utt.Timestamp = timestamp;
			utt.Who = who;
			utt.Text = text;

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

		private static string StripTags (string line)
		{
			int first = line.IndexOf ('<');
			if (first == -1)
				return line;
			
			StringBuilder builder = new StringBuilder ();
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

		private static bool IsNewConversation (string line)
		{
			int i = line.IndexOf ("--- New Conv");
			return 0 <= i && i < 5;
		}

		static private string REGEX_DATE =
		"Conversation @ \\S+\\s+(\\S+)\\s+(\\d+)\\s+(\\d+):(\\d+):(\\d+)\\s+(\\d+)";
		
		static private Regex dateRegex = new Regex (REGEX_DATE,
							    RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static private DateTimeFormatInfo dtInfo = new DateTimeFormatInfo ();

		private static DateTime NewConversationTime (string line)
		{
			Match m = dateRegex.Match (line);
			if (m.Success) {
				// I'm sure there is an easier way to do this.
				String monthName = m.Groups [1].ToString ();
				int day = int.Parse (m.Groups [2].ToString ());
				int hr = int.Parse (m.Groups [3].ToString ());
				int min = int.Parse (m.Groups [4].ToString ());
				int sec = int.Parse (m.Groups [5].ToString ());
				int yr = int.Parse (m.Groups [6].ToString ());

				int mo = -1;
				for (int i = 1; i <= 12; ++i) {
					if (monthName == dtInfo.GetAbbreviatedMonthName (i)) {
						mo = i;
						break;
					}
				}
				
				if (mo != -1)
					return new DateTime (yr, mo, day, hr, min, sec);
			}

			Console.WriteLine ("Failed on '{0}'", line);
			return new DateTime ();
		}

		///////////////////////////////////////

		private bool TrySnippet ()
		{
			int best_word_count = 0;

			foreach (Utterance utt in RawUtterances) {

				string possible_snippet = utt.Text.Trim ();

				int word_count = StringFu.CountWords (possible_snippet, 15);
				if (word_count > best_word_count) {
					Snippet = possible_snippet;
					best_word_count = word_count;
				}


				if (word_count > 3)
					return true;
			}

			return false;
		}

		// FIXME: The ending timestamp in the log will be inaccurate
		// until Load is called... before that, the ending time will
		// come from the timestamp of the snippet-line.

		private void SetSnippet ()
		{
			LoadWithTermination (new LoadTerminator (TrySnippet));
		}

		///////////////////////////////////////

		private GaimLog (string file, long offset) : base ("gaim", file, offset)
		{ 
			SetSnippet ();
		}

		private GaimLog (string file) : base ("gaim", file)
		{ 
			SetSnippet ();
		}

		// Return true if a new utterance is now available,
		// and false if the previous utterance was changed.
		private bool ProcessLine (string line)
		{
			if (! line.StartsWith ("(")) {
				AppendToPreviousUtterance (line);
				return false;
			}
			int j = line.IndexOf (')');
			if (j == -1) {
				AppendToPreviousUtterance (line);
				return false;
			}
			string whenStr = line.Substring (1, j-1);
			string[] whenSplit = whenStr.Split (':');
			int hour, minute, second;
			try {
				hour   = int.Parse (whenSplit [0]);
				minute = int.Parse (whenSplit [1]);
				second = int.Parse (whenSplit [2]);
			} catch {
				// If something goes wrong, this line probably
				// spills over from the previous one.
				AppendToPreviousUtterance (line);
				return false;
			}

			line = line.Substring (j+1).Trim ();

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
				return false;
			string alias = line.Substring (0, i);
			string text = line.Substring (i+1).Trim ();

			AddUtterance (when, alias, text);

			return true;
		}

		protected delegate bool LoadTerminator ();

		protected override void Load ()
		{
			ClearUtterances ();
			LoadWithTermination (null);
		}

		protected void LoadWithTermination (LoadTerminator terminator)
		{
			FileStream fs;
			StreamReader sr;
			string line;

			try {
				fs = new FileStream (LogFile,
						     FileMode.Open,
						     FileAccess.Read,
						     FileShare.Read);
				if (LogOffset > 0)
					fs.Seek (LogOffset, SeekOrigin.Begin);
				sr = new StreamReader (fs);
			} catch (Exception e) {
				// If we can't open the file, just fail.
				Console.WriteLine ("Could not open '{0}' (offset={1})", LogFile, LogOffset);
				Console.WriteLine (e);
				return;
			}

			line = sr.ReadLine (); // throw away first line
			if (line != null) {

				// Could the second line ever start w/ < in a non-html log?
				// I hope not!
				bool isHtml = line.Length > 0 && line [0] == '<';
				
				while ((line = sr.ReadLine ()) != null) {
					if (isHtml)
						line = StripTags (line);
				
					if (IsNewConversation (line))
						break;
					
					// Only check termination when a new Utterance has become
					// available.
					if (ProcessLine (line)
					    && terminator != null
					    && terminator ())
						break;
				}
			}

			sr.Close ();
			fs.Close ();
		}

		private static void ScanNewStyleLog (FileInfo file, ArrayList array)
		{
			ImLog log = new GaimLog (file.FullName);
			
			log.Timestamp = file.LastWriteTime;
			
			string startStr = Path.GetFileNameWithoutExtension (file.Name);
			log.StartTime = DateTime.ParseExact (startStr,
							     "yyyy-MM-dd.HHmmss",
							     CultureInfo.CurrentCulture);

			log.SpeakingTo = file.Directory.Name;
			log.Identity   = file.Directory.Parent.Name;

			array.Add (log);
		}


		private static void ScanOldStyleLog (FileInfo file, ArrayList array)
		{
			Stream stream;
			stream = new FileStream (file.FullName,
						 FileMode.Open,
						 FileAccess.Read,
						 FileShare.Read);
			StreamReader sr = new StreamReader (stream);
			string line;
			long offset = 0;
			
			string speakingTo = Path.GetFileNameWithoutExtension (file.Name);
			
			line = sr.ReadLine ();
			bool isHtml = line.ToLower ().StartsWith ("<html>");
			offset = line.Length + 1;

			while ((line = sr.ReadLine ()) != null) {
				long newOffset = offset + line.Length + 1;
				if (isHtml)
					line = StripTags (line);
				if (IsNewConversation (line)) {
					ImLog log = new GaimLog (file.FullName, offset);
					log.StartTime = NewConversationTime (line);
					log.Identity = "_OldGaim_"; // FIXME: parse a few lines of the log to figure this out
					log.SpeakingTo = speakingTo;

					array.Add (log);
				}
				
				offset = newOffset;
			}

			sr.Close ();
			stream.Close ();
		}

		public static ICollection ScanLog (FileInfo file)
		{
			ArrayList array = new ArrayList ();
			if (file.Extension == ".txt" || file.Extension == ".html")
				ScanNewStyleLog (file, array);
			else if (file.Extension == ".log")
				ScanOldStyleLog (file, array);
			return array;
		}
	}
}

