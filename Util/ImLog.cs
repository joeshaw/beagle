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

	public class ImLog {

		public delegate void Sink (ImLog imLog);

		public Sink loader; // semi-private

		public string LogFile;
		public long   LogOffset;

		public string Protocol;

		public DateTime StartTime;
		public DateTime EndTime;
		public DateTime Timestamp;

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

		public ImLog (string protocol, string file, long offset)
		{
			Protocol = protocol;
			LogFile = file;
			LogOffset = offset;
		}

		public ImLog (string protocol, string file) : this (protocol, file, -1)
		{ }

		public string Uri {
			get {
				// Hacky
				String when = StartTime.ToString ("yyyy.MM.dd.HH.mm.ss");
				return String.Format ("imlog://{0}/{1}/{2}/{3}",
						      Protocol, Identity, SpeakingTo,
						      when);
			}							    
		}


		public ICollection Speakers {
			get { return speakerHash.Keys; }
		}

		public IList Utterances {
			get { Load (); return utterances; }
		}

		public void AddUtterance (DateTime timestamp,
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

		public void AppendToPreviousUtterance (string text)
		{
			if (utterances.Count > 0) {
				Utterance utt = (Utterance) utterances [utterances.Count - 1];
				utt.Text += "\n" + text;
			}
		}

		public void Load ()
		{
			if (loader != null)
				loader (this);
			loader = null;
		}
	}
	
	///////////////////////////////////////////////////////////////////////////////

	public abstract class ImLogScanner {

		abstract public void Scan (string path, ImLog.Sink sink);

	}
	
	public class GaimLogScanner : ImLogScanner {

		private void Crawl (DirectoryInfo info, ImLog.Sink sink)
		{
			foreach (DirectoryInfo subdir in info.GetDirectories ())
				Scan (subdir.FullName, sink);

			foreach (FileInfo file in info.GetFiles ()) {
				if (file.Extension == ".txt" || file.Extension == ".html")
					ScanNewStyleLog (file, sink);
				else if (file.Extension == ".log")
					ScanOldStyleLog (file, sink);
			}
		}

		override public void Scan (string path, ImLog.Sink sink)
		{
			// Crawl the user's .gaim directory, looking for logs
			DirectoryInfo info = new DirectoryInfo (path);
			if (info.Exists)
				Crawl (info, sink);
		}

		private void ScanNewStyleLog (FileInfo file, ImLog.Sink sink)
		{
			ImLog log = new ImLog ("gaim", file.FullName);
			
			log.Timestamp = file.LastWriteTime;
			
			string startStr = Path.GetFileNameWithoutExtension (file.Name);
			log.StartTime = DateTime.ParseExact (startStr,
							     "yyyy-MM-dd.HHmmss",
							     CultureInfo.CurrentCulture);

			string path = file.FullName;
			path = Path.GetDirectoryName (path);
			log.SpeakingTo = Path.GetFileName (path);
			path = Path.GetDirectoryName (path);
			log.Identity = Path.GetFileName (path);

			log.loader = this.LoadLog;

			sink (log);
		}


		private void ScanOldStyleLog (FileInfo file, ImLog.Sink sink)
		{
			StreamReader sr = file.OpenText ();
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
					ImLog log = new ImLog ("gaim", file.FullName, offset);
					log.loader = this.LoadLog;
					log.StartTime = NewConversationTime (line);
					log.Identity = "_OldGaim_";
					log.SpeakingTo = speakingTo;

					sink (log);
				}
				
				offset = newOffset;
			}
		}

		//////////////////////////

		private static string StripTags (string line)
		{
			int i, j;
			while (true) {
				i = line.IndexOf ('<');
				if (i == -1)
					break;
				j = line.IndexOf ('>', i);
				line = line.Substring (0, i) + line.Substring (j+1);
			}
			return line;
		}

		private bool IsNewConversation (string line)
		{
			int i = line.IndexOf ("--- New Conv");
			return 0 <= i && i < 5;
		}

		static private string REGEX_DATE =
		"Conversation @ \\S+\\s+(\\S+)\\s+(\\d+)\\s+(\\d+):(\\d+):(\\d+)\\s+(\\d+)";
		
		static private Regex dateRegex = new Regex (REGEX_DATE,
							    RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static private DateTimeFormatInfo dtInfo = new DateTimeFormatInfo ();

		private DateTime NewConversationTime (string line)
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

		private void ProcessLine (ImLog log, string line)
		{
			if (! line.StartsWith ("(")) {
				log.AppendToPreviousUtterance (line);
				return;
			}
			int j = line.IndexOf (')');
			if (j == -1) {
				log.AppendToPreviousUtterance (line);
				return;
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
				log.AppendToPreviousUtterance (line);
				return;
			}

			line = line.Substring (j+1).Trim ();

			// FIXME: this is wrong --- since we just get a time,
			// the date gets set to 'now'
			DateTime when = new DateTime (log.StartTime.Year,
						      log.StartTime.Month,
						      log.StartTime.Day,
						      hour, minute, second);

			// Try to deal with time wrapping around.
			while (when < log.EndTime)
				when = when.AddDays (1);

			int i = line.IndexOf (':');
			if (i == -1)
				return;
			string alias = line.Substring (0, i);
			string text = line.Substring (i+1).Trim ();

			log.AddUtterance (when, alias, text);
		}

		private void LoadLog (ImLog log)
		{
			StreamReader sr;
			string line;

			try {
				FileStream fs = new FileStream (log.LogFile,
								FileMode.Open, FileAccess.Read);
				if (log.LogOffset > 0)
					fs.Seek (log.LogOffset, SeekOrigin.Begin);
				sr = new StreamReader (fs);
			} catch (Exception e) {
				// If we can't open the file, just fail.
				Console.WriteLine ("Could not open '{0}' (offset={1})",
						   log.LogFile, log.LogOffset);
				Console.WriteLine (e);
				return;
			}

			line = sr.ReadLine (); // throw away first line
			if (line == null)
				return;

			// Could the second line ever start w/ < in a non-html log?
			// I hope not!
			bool isHtml = line.StartsWith ("<");
			
			while ((line = sr.ReadLine ()) != null) {
				if (isHtml)
					line = StripTags (line);
				
				if (IsNewConversation (line))
					break;

				ProcessLine (log, line);
			}
		}
	}

}
