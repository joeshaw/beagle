 //
// IndexMail.cs
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Beagle;
using Camel = Beagle.Util.Camel;

namespace IndexMailTool {


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


	//////////////////////////////////////////////////////////////////


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
		
		public int Parse (IList lines, int i)
		{
			MailHeader header = null;
			
			while (i < lines.Count) {
				String line = (String) lines [i];
				++i;
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
			
			return i;
		}
	}


	//////////////////////////////////////////////////////////////////


	public class MailBody {
		
		MailHeaderList headers = null;
		ArrayList alternatives = null;
		ArrayList body = new ArrayList ();

		public MailHeaderList Headers {
			get { return headers; }
		}

		public ICollection Alternatives { 
			get { return alternatives; }
		}

		public ICollection Body {
			get { return body; }
		}

		public String this [String key] {
			get { return headers == null ? null : headers [key];
			}
		}

		public String Boundary {
			get { return headers == null ? null : headers.Boundary; }
		}

		public int Parse (IList lines, int i, String boundary)
		{
			String boundaryFirst = null;
			String boundaryLast = null;
			if (boundary != null) {
				headers = new MailHeaderList ();
				i = headers.Parse (lines, i);
				boundaryFirst = "--" + boundary;
				boundaryLast = boundaryFirst + "--";
			}

			if (headers != null
			    && headers.ContentType == "multipart/alternative") {
				String subBoundary = headers.Boundary;
				String subBoundaryFirst = "--" + subBoundary;
				String subBoundaryLast = subBoundaryFirst + "--";

				// Skip past first sub-boundary
				while (i < lines.Count) {
					String line = (String) lines [i];
					if (line == subBoundaryFirst || line == subBoundaryLast) {
						++i;
						break;
					}
					if (line != "")
						break;
					++i;
				}

				alternatives = new ArrayList ();
				while (i < lines.Count) {
					String line = (String) lines [i];
					if (line == boundaryFirst || line == boundaryLast) {
						++i;
						break;
					}
					MailBody subBody = new MailBody ();
					i = subBody.Parse (lines, i, subBoundary);
					alternatives.Add (subBody);
				}

			} else {
				while (i < lines.Count) {
					String line = (String) lines [i];
					++i;
					if (line == boundaryFirst || line == boundaryLast)
						break;
					body.Add (line);
				}

				// Gobble trailing whitespace
				while (i < lines.Count) {
					if ((String) lines [i] != "")
						break;
					++i;
				}


			}

			return i;
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
			if (body.Count > 0)
				Console.WriteLine ("Lines: {0}", body.Count);
		}
	}


	//////////////////////////////////////////////////////////////////	
	
	
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

		public MailMessage (IList lines)
		{
			// First, process the From_ line.
			String fromLine = (String) lines [0];
			int fromSplit = fromLine.IndexOf (' ', 5);
			fromSender = fromLine.Substring (5, fromSplit-5);
			fromTime = fromLine.Substring (fromSplit+1);

			// Next, read the headers
			int i = headers.Parse (lines, 1);

			// Is this a multi-part message?
			String boundary = headers.Boundary;
			bool isMultipart = (boundary != null);

			// If this is a multi-part message, skip everything
			// up to the first boundary;
			if (isMultipart) {
				while (i < lines.Count) {
					String line = (String) lines [i];
					++i;
					if (line == "--" + boundary)
						break;
				}
			} else {
				// Otherwise, just skip the blank line after the
				// mail headers.
				++i;
			}
			
			// Finally, read in the message bodies.
			while (i < lines.Count) {
				MailBody body = new MailBody ();
				i = body.Parse (lines, i, boundary);
				bodies.Add (body);
			}
		}

		
	}

	//////////////////////////////////////////////////////////////////	

	public class IndexableMail : Indexable {

		public IndexableMail (String accountId,
				      String folderName,
				      Camel.MessageInfo messageInfo,
				      MailMessage message)
		{
			Uri = String.Format ("email://{0}/{1};uid={2}",
					     accountId, folderName, messageInfo.uid);
			Type = "MailMessage";
			MimeType = null;

			Timestamp = messageInfo.Date;

			// Assemble the metadata
			this ["Folder"] = folderName;
			this ["Subject"] = messageInfo.subject;
			this ["To"] =  messageInfo.to;
			this ["From"] = messageInfo.from;
			this ["Cc"] = messageInfo.cc;
			this ["Received"] = messageInfo.received.ToString ();
			this ["SentDate"] = messageInfo.sent.ToString ();
			this ["Mlist"] = messageInfo.mlist;

			this ["_Flags"] = Convert.ToString (messageInfo.flags);
			
			if (messageInfo.IsAnswered)
				this ["_IsAnswered"] = "1";
			if (messageInfo.IsDeleted)
				this ["_IsDeleted"] = "1";
			if (messageInfo.IsDraft)
				this ["_IsDraft"] = "1";
			if (messageInfo.IsFlagged)
				this ["_IsFlagged"] = "1";
			if (messageInfo.IsSeen)
				this ["_IsSeen"] = "1";
			if (messageInfo.HasAttachments)
				this ["_HasAttachments"] = "1";
			if (messageInfo.IsAnsweredAll)
				this ["_IsAnsweredAll"] = "1";

			// Assemble the content, if we have any
			if (message != null) {
				StringBuilder contentBuilder = new StringBuilder ();
				foreach (MailBody body in message.Bodies)
					AddContent (body, contentBuilder);
				Content = contentBuilder.ToString ();
			}
		}

		void AddContent (MailBody body, StringBuilder contentBuilder)
		{
			if (body.Alternatives != null
			    && body.Alternatives.Count > 0) {
				foreach (MailBody alt in body.Alternatives)
					if (alt != null 
					    && alt.Headers != null 
					    && alt.Headers.ContentType == "text/plain")
						AddContent (alt, contentBuilder);
			}

			if (body.Headers == null
			    || body.Headers.ContentType == "text/plain") {
				foreach (String line in body.Body) {
					// Trim off >-quoting
					String x = line.Trim ();
					while (x.Length > 0 && x [0] == '>')
						x = x.Substring (1).Trim ();
					if (x.Length > 0) {
						contentBuilder.Append (x);
						contentBuilder.Append (" ");
					}
				}
			}
		}

	}

	//////////////////////////////////////////////////////////////////	

	public class MailScanner {

		IndexDriver driver = new IndexDriver ();
		ArrayList toIndex = new ArrayList ();
		int count = 0;

		private void Schedule (Indexable indexable)
		{
			toIndex.Add (indexable);
			++count;
			if (toIndex.Count > 1000) {
				driver.Add (toIndex);
				driver.Optimize ();
				toIndex.Clear ();
			}
		}

		public void Flush ()
		{
			if (toIndex.Count > 0)
				driver.Add (toIndex);
		}

		//////////////////////////

		public void MessageStatus (String str)
		{
			Console.Write ("\x1b[1G{0}\x1b[0K", str); // terminal-fu!
		}

		public void MessageStatus (String format, params object[] args)
		{
			MessageStatus (String.Format (format, args));
		}

		public void MessageFinished (String str)
		{
			Console.WriteLine ("\x1b[1G{0}\x1b[0K", str); // terminal-fu!
		}

		public void MessageFinished (String format, params object[] args)
		{
			MessageFinished (String.Format (format, args));
		}

		//////////////////////////

		public void ScanMbox (String folderName, String mboxFile, String summaryFile)
		{
			Camel.Summary summary = Camel.Summary.load (summaryFile);

			String dataName = "lastScan-" + folderName;

			DateTime lastTime = new DateTime (0);
			String lastTimeStr = PathFinder.ReadAppDataLine ("IndexMail", dataName);
			if (lastTimeStr != null)
				lastTime = DateTime.Parse (lastTimeStr);

			DateTime latestTime = lastTime;

			ASCIIEncoding encoding = new ASCIIEncoding ();
			Stream mboxStream = null;

			/* FIXME: Problems with basing everything off of the summary
			   (1) We don't notice when messages are expunged -- they will stay in the index.
			   (2) We don't notice changes in the flags.
			*/

			int count = 0, indexedCount = 0;
			foreach (Camel.MBoxMessageInfo mi in summary.messages) {

				if ((count & 15) == 0)
					MessageStatus ("{0}: indexed {1} messages ({2}/{3} {4:###.0}%)",
						       folderName, indexedCount,
						       count, summary.header.count, 
						       100.0 * count / summary.header.count);
				++count;

				if (lastTime < mi.Date) {

					if (latestTime < mi.Date)
						latestTime = mi.Date;

					// If we haven't open the mbox yet, do it now
					if (mboxStream == null)
						mboxStream = new FileStream (mboxFile, FileMode.Open, FileAccess.Read);
					
					// Seek to the beginning of this message
					mboxStream.Seek (mi.from_pos, SeekOrigin.Begin);

					// Read the message and split the text into lines
					byte[] msgBytes = new byte [mi.size];
					mboxStream.Read (msgBytes, 0, (int) mi.size);
					String[] lines = encoding.GetString (msgBytes).Split ('\n');

					// Parse an RFC 2822 message from the array of lines
					MailMessage msg = new MailMessage (lines);

					Schedule (new IndexableMail ("local@local",
								     folderName,
								     mi,
								     msg));
					++indexedCount;
				}
			}

			MessageFinished ("{0}: indexed {1} of {2} messages", folderName, indexedCount, count);

			if (mboxStream != null)
				mboxStream.Close ();

			if (latestTime != lastTime)
				PathFinder.WriteAppDataLine ("IndexMail", dataName, latestTime.ToString ());
		}

	}

	class IndexMailTool {

		static void Main (String[] args)
		{
			String local = Environment.GetEnvironmentVariable ("HOME");
			local = Path.Combine (local, ".evolution/mail/local");
			if (! Directory.Exists (local)) {
				Console.WriteLine ("Can't find {0}", local);
				return;
			}

			MailScanner scanner = new MailScanner ();

			DirectoryInfo dir = new DirectoryInfo (local);

			if (args.Length > 0) {
			    foreach (String summaryPath in args) {
				if (Path.GetExtension (summaryPath) != ".ev-summary") {
				    Console.WriteLine ("Argument is not Evolution summary file.");
				    continue;
				}

				string folderName = Path.GetFileNameWithoutExtension (summaryPath);
				string mboxFile = Path.ChangeExtension (summaryPath, null);

				Console.WriteLine ("Mbox: " + mboxFile);
				if (File.Exists (mboxFile))
				    scanner.ScanMbox (folderName, mboxFile, summaryPath);

			    }
			    
			    scanner.Flush ();
			    return;
			}
			    
			foreach (FileInfo f in dir.GetFiles ()) {
				if (Path.GetExtension (f.Name) == ".ev-summary") {
					String folderName = Path.GetFileNameWithoutExtension (f.Name);
					String summaryFile = f.FullName;
					String mboxFile = Path.Combine (local, folderName);

					if (File.Exists (mboxFile) && folderName.ToLower () != "spam")
						scanner.ScanMbox (folderName, mboxFile, summaryFile);
				}
			}
			
			scanner.Flush ();
		}
	}
}
