//
// TomboyQueryable.cs
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

using System;
using System.Collections;
using System.IO;
using System.Text;

using Beagle.Daemon;
using Beagle.Util;
using Camel = Beagle.Util.Camel;

namespace Beagle.Daemon.MailQueryable {
	
	[QueryableFlavor (Name="MailQueryable", Domain=QueryDomain.Local)]
	public class MailQueryable : LuceneQueryable {

		public MailQueryable () : base ("Mail",
						Path.Combine (PathFinder.RootDir, "MailIndex"))
		{
			string home = Environment.GetEnvironmentVariable ("HOME");
			DirectoryInfo mail_dir = new DirectoryInfo (Path.Combine (home, ".evolution/mail/local"));

			// Crawl mail
			MailCrawler crawler = new MailCrawler (this);
			crawler.ScheduleCrawl (mail_dir, -1);
			//crawler.StopWhenEmpty ();
			
			FileSystemEventMonitor monitor = new FileSystemEventMonitor ();
			monitor.FileSystemEvent += OnFileSystemEvent;
			monitor.Subscribe (mail_dir, true);

			// FIXME: Set up FileSystemEventMonitor
		}

		private void OnFileSystemEvent (FileSystemEventMonitor monitor,
						FileSystemEventType event_type,
						string old_path,
						string new_path)
		{
			if (event_type == FileSystemEventType.Created || event_type == FileSystemEventType.Changed) {
				FileInfo file = new FileInfo (new_path);

				if (file != null && Path.GetExtension (file.Name) == ".ev-summary")
					this.IndexMbox (file);

			} else if (event_type == FileSystemEventType.Deleted) {
				// FIXME
			}
		}

		public class MailCrawler : Crawler {
			
			MailQueryable queryable;

			public MailCrawler (MailQueryable queryable) : base (queryable.Driver.Fingerprint)
			{
				this.queryable = queryable;
			}

			protected override bool SkipByName (FileSystemInfo info)
			{
				// Directories are ok...
				if (info as DirectoryInfo != null)
					return false;

				if (Path.GetExtension (info.Name) != ".ev-summary")
					return true;

				return false;
			}

			protected override void CrawlFile (FileSystemInfo info)
			{
				FileInfo file = info as FileInfo;

				if (file == null)
					return;

				this.queryable.IndexMbox (file);
			}
		}

		public void IndexMbox (FileInfo summaryInfo)
		{
			string summaryFile = summaryInfo.FullName;
			string mboxFile = Path.ChangeExtension (summaryFile, null);
			string folderName = Path.GetFileNameWithoutExtension (summaryInfo.Name);

			Camel.Summary summary = Camel.Summary.load (summaryFile);

			String dataName = "lastScan-" + folderName;

			DateTime lastTime = new DateTime (0);
			String lastTimeStr = PathFinder.ReadAppDataLine ("MailIndex", dataName);
			if (lastTimeStr != null) {
				try {
					long ticks = long.Parse (lastTimeStr);
					lastTime = new DateTime (ticks);
				} catch { }
			}

			DateTime latestTime = lastTime;

			ASCIIEncoding encoding = new ASCIIEncoding ();
			Stream mboxStream = null;

			// FIXME: Problems with basing everything off of the summary
			// (1) We don't notice when messages are expunged -- they will stay in the index.
			// (2) We don't notice changes in the flags.

			int count = 0, indexedCount = 0;
			foreach (Camel.MBoxMessageInfo mi in summary.messages) {

				if ((count & 15) == 0) {
					Console.WriteLine ("{0}: indexed {1} messages ({2}/{3} {4:###.0}%)",
							   folderName, indexedCount,
							   count, summary.header.count, 
							   100.0 * count / summary.header.count);
				}
				++count;

				if (lastTime < mi.Date) {

					if (latestTime < mi.Date)
						latestTime = mi.Date;

					// If we haven't open the mbox yet, do it now
					if (mboxStream == null)
						mboxStream = new FileStream (mboxFile, FileMode.Open, FileAccess.Read);
					
					// Seek to the beginning of this message
					mboxStream.Seek (mi.from_pos, SeekOrigin.Begin);

					TextReader textReader = new StreamReader (mboxStream, encoding);
					LineReader reader = new LineReader (textReader);

					// Parse an RFC 2822 message from the array of lines
					MailMessage msg = new MailMessage (reader, mi.size);
					
					Console.WriteLine ("From: {0}", mi.from);
					Console.WriteLine ("Subject: {0}", mi.subject);

					Driver.ScheduleAdd (MailToIndexable (folderName, mi, msg));
					++indexedCount;
				}
			}

			Console.WriteLine ("{0}: indexed {1} of {2} messages", folderName, indexedCount, count);

			if (mboxStream != null)
				mboxStream.Close ();

			if (latestTime != lastTime)
				PathFinder.WriteAppDataLine ("MailIndex", dataName, latestTime.Ticks.ToString ());
		}

                private static void BodyToMultiReader (MailBody body, Beagle.Util.MultiReader multi)
                {
                        if (body.Alternatives != null
                            && body.Alternatives.Count > 0) {
                                foreach (MailBody alt in body.Alternatives) {
                                        if (alt != null
                                            && alt.Headers != null
                                            && alt.Headers.ContentType == "text/plain")
                                                BodyToMultiReader (alt, multi);
                                }
                        }
 
                        if (body.Headers == null || body.Headers.ContentType == "text/plain")
                                multi.Add (new Beagle.Util.PullingReader (new Beagle.Util.PullingReader.Pull (body.Pull)));
                }

		private static Indexable MailToIndexable (string folderName, Camel.MessageInfo messageInfo,
							  MailMessage message)
		{
			System.Uri uri = new Uri (String.Format ("email://local@local/{0};uid={1}",
								 folderName, messageInfo.uid));
			Indexable indexable = new Indexable (uri);

			indexable.Timestamp = messageInfo.Date;
			indexable.Type = "MailMessage";
			indexable.MimeType = "text/plain";

			indexable.AddProperty (Property.NewKeyword ("dc:title", messageInfo.subject));

                        indexable.AddProperty (Property.NewKeyword ("fixme:folder",   folderName));
			indexable.AddProperty (Property.NewKeyword ("fixme:subject",  messageInfo.subject));
                        indexable.AddProperty (Property.NewKeyword ("fixme:to",       messageInfo.to));
			indexable.AddProperty (Property.NewKeyword ("fixme:from",     messageInfo.from));
                        indexable.AddProperty (Property.NewKeyword ("fixme:cc",       messageInfo.cc));
			indexable.AddProperty (Property.NewDate    ("fixme:received", messageInfo.received));
                        indexable.AddProperty (Property.NewDate    ("fixme:sentdate", messageInfo.sent));
                        indexable.AddProperty (Property.NewKeyword ("fixme:mlist",    messageInfo.mlist));
                        indexable.AddProperty (Property.NewKeyword ("fixme:flags",    messageInfo.flags));

			if (folderName == "Sent")
				indexable.AddProperty (Property.NewFlag ("fixme:isSent"));

			if (messageInfo.IsAnswered)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnswered"));

			if (messageInfo.IsDeleted)
				indexable.AddProperty (Property.NewFlag ("fixme:isDeleted"));

			if (messageInfo.IsDraft)
				indexable.AddProperty (Property.NewFlag ("fixme:isDraft"));

			if (messageInfo.IsFlagged)
				indexable.AddProperty (Property.NewFlag ("fixme:isFlagged"));

			if (messageInfo.IsSeen)
				indexable.AddProperty (Property.NewFlag ("fixme:isSeen"));

			if (messageInfo.HasAttachments)
				indexable.AddProperty (Property.NewFlag ("fixme:hasAttachments"));

			if (messageInfo.IsAnsweredAll)
				indexable.AddProperty (Property.NewFlag ("fixme:isAnsweredAll"));

			if (message != null) {
				Beagle.Util.MultiReader multi = new Beagle.Util.MultiReader ();

				foreach (MailBody body in message.Bodies)
					BodyToMultiReader (body, multi);

				if (multi.Count > 0)
					indexable.SetTextReader (multi);
			}
			return indexable;
		}

		public class MailHeader {
			String key;
			String value;
		
			public String Key {
				get { return key; }
			}
		
			public String Value {
				get { return value; }
			}
			
			public MailHeader (String line)
			{
				int i = line.IndexOf (":");
				if (i == -1)
					throw new Exception ("Broken header: [" + line + "]");
				key = line.Substring (0, i).Trim ();
				value = line.Substring (i+1).Trim ();
			}
		
			public void Extend (String line)
			{
				value += line;
			}
		}

		public class MailHeaderList {
			
			ArrayList headers = null;

			public ICollection Headers {
				get { return headers == null ? new ArrayList () : headers; }
			}

			public String this [String key] {
				get { 
					key = key.ToLower ();
					foreach (MailHeader header in Headers)
						if (header.Key.ToLower () == key)
							return header.Value;
					return null;
				}
			}

			public String ContentType {
				get { 
					String val = this ["Content-Type"];
					if (val != null) {
						int i = val.IndexOf (';');
						if (i != -1)
							return val.Substring (0, i);
					}
				
					return val;
				}
			}
		
			public String GetContentTypeInfo (String field)
			{
				String ctype = this ["Content-Type"];
				if (ctype == null)
					return null;
			
				String val = null;
				while (true) {
					int i = ctype.IndexOf (';');
					if (i == -1)
						return null;
					ctype = ctype.Substring (i+1).Trim ();
				
					if (ctype.StartsWith (field + "=")) {
						i = ctype.IndexOf (';');
						if (i == -1)
							val = ctype.Substring (field.Length + 1);
						else
							val = ctype.Substring (field.Length + 1,
									       i - field.Length - 1);
						break;
					}
				}
			
				// Strip off quotes
				if (val [0] == '"' && val [val.Length-1] == '"')
					val = val.Substring (1, val.Length-2);
			
				return val;
			}
		
			public String Boundary {
				get { return GetContentTypeInfo ("boundary"); }
			}
		
			private void Add (MailHeader header)
			{
				if (header != null) {
					if (headers == null)
						headers = new ArrayList ();
					headers.Add (header);
				}
			}
		
			public void Parse (LineReader reader)
			{
				MailHeader header = null;
				string line;
			
				while ((line = reader.ReadLine ()) != null) {
					if (line == "") {
						break;
					} else if (header != null && (line [0] == '\t' || line [0] == ' ')) {
						header.Extend (line.Substring (1));
					} else if (line.IndexOf (':') == -1) {
						break;
					} else {
						Add (header);
						header = new MailHeader (line);
					}
				}
				Add (header);
			}
		}

		public class MailBody {
		
			MailHeaderList headers = null;
			ArrayList alternatives = null;
			Queue body = new Queue ();

			public MailHeaderList Headers {
				get { return headers; }
			}

			public ICollection Alternatives { 
				get { return alternatives; }
			}

			public ICollection Body {
				get { return body; }
			}

			public string Pull ()
			{
				if (body.Count > 0)
					return (string) body.Dequeue ();
				return null;
			}

			public String this [String key] {
				get { return headers == null ? null : headers [key];
				}
			}

			public String Boundary {
				get { return headers == null ? null : headers.Boundary; }
			}

			public void Parse (LineReader reader, String boundary)
			{
				string boundaryFirst = null;
				string boundaryLast = null;
				if (boundary != null) {
					headers = new MailHeaderList ();
					headers.Parse (reader);
					boundaryFirst = "--" + boundary;
					boundaryLast = boundaryFirst + "--";
				}

				if (headers != null
				    && headers.ContentType == "multipart/alternative") {
					string subBoundary = headers.Boundary;
					string subBoundaryFirst = "--" + subBoundary;
					string subBoundaryLast = subBoundaryFirst + "--";

					string line;

					// Skip past first sub-boundary
					while ((line = reader.ReadLine ()) != null) {
						if (line == subBoundaryFirst || line == subBoundaryLast) {
							line = null;
							break;
						}
						if (line != "") {
							reader.UnReadLine (line);
							break;
						}
					}

					alternatives = new ArrayList ();
					while ((line = reader.ReadLine ()) != null) {
						if (line == boundaryFirst || line == boundaryLast) {
							break;
						}
						reader.UnReadLine (line);
						MailBody subBody = new MailBody ();
						subBody.Parse (reader, subBoundary);
						alternatives.Add (subBody);
					}

				} else {
					string line;
					while ((line = reader.ReadLine ()) != null) {
						if (line == boundaryFirst || line == boundaryLast)
							break;
						body.Enqueue (line);
					}

					// Gobble trailing whitespace
					while ((line = reader.ReadLine ()) != null) {
						if (line != "") {
							reader.UnReadLine (line);
							break;
						}
					}


				}
			}
		
			public void Describe ()
			{
				if (headers != null)
					Console.WriteLine ("Type: {0}", headers.ContentType);
				if (alternatives != null && alternatives.Count > 0) {
					Console.WriteLine ("Alternatives: {0}", alternatives.Count);
					foreach (MailBody alt in alternatives)
						alt.Describe ();
					Console.WriteLine ("End of Alternatives");
				}
				if (body.Count > 0) {
					foreach (string line in body)
						Console.WriteLine ("[{0}]", line);
					Console.WriteLine ("Lines: {0}", body.Count);
				}
			}
		}

		public class MailMessage {

			// Info from the 'From_' line.
			String fromSender;
			String fromTime;

			MailHeaderList headers = new MailHeaderList ();
			ArrayList bodies = new ArrayList ();

			public MailHeaderList Headers {
				get { return headers; }
			}

			public String this [String key] {
				get { return headers [key]; }
			}

			public ICollection Bodies {
				get { return bodies; }
			}

			public MailMessage (LineReader reader, long size)
			{
				// First, process the From_ line.
				String fromLine = reader.ReadLine ();
				int fromSplit = fromLine.IndexOf (' ', 5);
				fromSender = fromLine.Substring (5, fromSplit-5);
				fromTime = fromLine.Substring (fromSplit+1);

				reader.MaxBytes = size;

				// Next, read the headers
				headers.Parse (reader);

				// Is this a multi-part message?
				String boundary = headers.Boundary;
				bool isMultipart = (boundary != null);

				string line;

				// If this is a multi-part message, skip everything
				// up to the first boundary;
				if (isMultipart) {
					while ((line = reader.ReadLine ()) != null) {
						if (line == "--" + boundary)
							break;
					}
				} 
			
				// Finally, read in the message bodies.
				while ((line = reader.ReadLine ()) != null) {
					reader.UnReadLine (line);
					MailBody body = new MailBody ();
					body.Parse (reader, boundary);
					bodies.Add (body);
				}
			}

		
		}

		public class LineReader {
		
			TextReader reader;
			Stack lineStack = new Stack ();
			long totalBytes = 0;
			long maxBytes = -1;

			public LineReader (TextReader _reader)
			{
				reader = _reader;
			}

			public long MaxBytes {
				get { return maxBytes; }
				set { totalBytes = 0; maxBytes = value; }
			}

			public string ReadLine ()
			{
				string line;

				if (lineStack.Count > 0) {
					line = (string) lineStack.Pop ();
					//Console.WriteLine ("Popped '{0}'", line);
					return line;
				}

				if (maxBytes > 0 && totalBytes >= maxBytes)
					return null;

				line = reader.ReadLine ();
				if (line != null)
					totalBytes += line.Length + 1;
				//Console.WriteLine ("Read '{0}'", line == null ? "(null)" : line);
				return line;
			}

			public void UnReadLine (string line)
			{
				//Console.WriteLine ("Unread '{0}'", line);
				lineStack.Push (line);
			}
		}

	}
}
