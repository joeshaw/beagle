//
// IndexMail.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using Dewey;

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

		static String[] dayNames = new String [7] { "sun", "mon", "tue", "wed", "thu", "fri", "sat" };

		static String[] monthNames = new String [13] {	"!!!", "jan", "feb", "mar", "apr",
								"may", "jun", "jul", "aug", "sep",
								"oct", "nov", "dec" };

		bool dateCached = false;
		DateTime date;

		public DateTime Date {
			get {
				if (dateCached)
					return date;

				String dateStr = headers ["Date"];

				Debug.Assert (dateStr != null);
				
				int i;
				
				// If there is a comma in there, it should be
				// preceeded by the day of the week.  We don't
				// need it, so we strip it out.
				i = dateStr.IndexOf (',');
				if (i != -1) {
					dateStr = dateStr.Substring (i+1).Trim ();
				} else {
					// There might be a day of the week, but w/o
					// a comma.  Check for that.
					String front = dateStr.Substring (0, 3).ToLower ();
					foreach (String dn in dayNames) {
						if (front == dn) {
							dateStr = dateStr.Substring (3).Trim ();
							break;
						}
					}
				}
				
				// Collapse adjacent spaces.  Not particularly
				// efficient, but maybe faster that using a regex.
				while (dateStr.IndexOf ("  ") != -1)
					dateStr = dateStr.Replace ("  ", " ");
				
				String[] parts = dateStr.Split (' ');

				if (parts.Length < 3) {
					Console.WriteLine ("(a) Totally broken date string: '{0}'", dateStr);
					return new DateTime ();
				}

				int day = 0, month = 0, year = 0;
				
				if (parts [1].Length >= 3) {
					String monthStr = parts [1].Substring (0, 3).ToLower ();
					for (i = 1; i < monthNames.Length; ++i)
						if (monthStr == monthNames [i]) {
							month = i;
							break;
						}
				}

				if (month == 0) {
					Console.WriteLine ("(b) Totally broken date string: '{0}'", dateStr);
					return new DateTime ();
				} else {
					try {
						day = int.Parse (parts [0]);
						year = int.Parse (parts [2]);
					} catch {
						Console.WriteLine ("(c) Totally broken date string '{0}'", dateStr);
						return new DateTime ();
					}
					
					if (year < 50)
						year += 2000;
					else if (year < 100)
						year += 1900;

					if (day < 1 || day > DateTime.DaysInMonth (year, month)) {
						Console.WriteLine ("(d) Totally broken date string '{0}'", dateStr);
						return new DateTime ();
					}
				}
				
				
				int hours = -1, minutes = -1, seconds = -1;
				// Now the time.  It is in the part that contains a ':'.
				for (i = 0; i < parts.Length; ++i)
					if (parts [i].IndexOf (':') != -1) {
						String[] timeparts = parts [i].Split (':');
						if (timeparts.Length != 3) {
							Console.WriteLine ("(e) Totally broken date string '{0}'", dateStr);
							return new DateTime ();
						}

						try {
							hours = int.Parse (timeparts [0]);
							minutes = int.Parse (timeparts [1]);
							seconds = int.Parse (timeparts [2]);
						} catch {
							Console.WriteLine ("(f) Totally broken date string '{0}'", dateStr);
							return new DateTime ();
						}
						break;
					}
				
				date = new DateTime (year, month, day, hours, minutes, seconds);
				dateCached = true;

				return date;
			}
		}

		public bool ValidDate {
			get { return Date.Ticks != 0; }
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

		StringBuilder contentBuilder = new StringBuilder ();

		public IndexableMail (String folderName, MailMessage message)
		{
			String xEv = message ["X-Evolution"];
			int i = xEv.IndexOf ('-');
			String uidStr = xEv.Substring (0, i);
			int uid = Convert.ToInt32 (uidStr, 16);

			// FIXME: The Uri is totally fake.
			uri = String.Format ("email://local@local/{0};uid={1}", folderName, uid);
			domain = "Mail";
			mimeType = "text/plain"; // FIXME: what about html mail?

			timestamp = message.Date;

			needPreload = false;

			// Assemble the metadata
			SetMetadata ("to", message ["To"]);
			SetMetadata ("from", message ["From"]);
			SetMetadata ("subject", message ["Subject"]);

			// Assemble the content
			foreach (MailBody body in message.Bodies)
				AddContent (body);
		}

		void AddContent (MailBody body)
		{
			if (body.Alternatives != null
			    && body.Alternatives.Count > 0) {
				foreach (MailBody alt in body.Alternatives)
					if (alt != null && alt.Headers != null && alt.Headers.ContentType == "text/plain")
						AddContent (alt);
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

		override public String Content {
			get { return contentBuilder.ToString (); }
		}

	}

	//////////////////////////////////////////////////////////////////	

	public class MboxScanner {

		public MboxScanner ()
		{

		}

		IndexDriver driver = new IndexDriver ();
		ArrayList toIndex = new ArrayList ();
		int count = 0;

		private void ProcessMessage (String folderName, MailMessage msg)
		{
			// Silently drop messages with mangled dates
			if (! msg.ValidDate)
				return;

			Indexable indexable = new IndexableMail (folderName, msg);

			toIndex.Add (indexable);
			if (toIndex.Count > 100) {
				driver.Add (toIndex);
				toIndex.Clear ();
			}

			++count;
			if ((count & 255) == 0)
				Console.WriteLine (count);


		}

		bool IsQuotedFrom (String line)
		{
			if (line.Length == 0)
				return false;

			if (line [0] != '>')
				return false;

			int i = 1;
			while (i < line.Length && line [i] == '>')
				++i;

			return line.Substring (i).StartsWith ("From");
		}

		public void Load (String folderName, String filename)
		{
			StreamReader sr = new StreamReader (filename);

			String line;
			ArrayList lines = new ArrayList ();
			bool first = true;
			while ((line = sr.ReadLine ()) != null) {
				if (line.StartsWith ("From ")) {
					// Throw away any lines that
					// precede the first From_ line
					if (! first)
						ProcessMessage (folderName, new MailMessage (lines));
					first = false;
					lines.Clear ();
					
				} else if (IsQuotedFrom (line)) {
					// Unquote quoted from lines
					line = line.Substring (1);
				}

				lines.Add (line);
			}

			if (lines.Count > 0)
				ProcessMessage (folderName, new MailMessage (lines));
		}

		public void Flush ()
		{
			if (toIndex.Count > 0)
				driver.Add (toIndex);
		}

	}

	class IndexMailTool {

		static void Main (String[] args)
		{
			MboxScanner mbox = new MboxScanner ();
			
			String home = Environment.GetEnvironmentVariable ("HOME");
			String local = Path.Combine (home, "evolution/local");
			if (! Directory.Exists (local)) {
				Console.WriteLine ("Can't find {0}", local);
				return;
			}

			DirectoryInfo dir = new DirectoryInfo (local);

			foreach (DirectoryInfo folder in dir.GetDirectories ()) {
				String mboxFile = Path.Combine (folder.FullName, "mbox");
				if (File.Exists (mboxFile))
					mbox.Load (folder.Name, mboxFile);
			}

			mbox.Flush ();
		}
	}
}
