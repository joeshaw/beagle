//
// IndexGaimLogs.cs
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
using System.Text;

using Beagle;
using Beagle.Util;

namespace IndexGaimLogs {

	class IndexableImLog : Indexable {
		
		private ImLog log;
		private ArrayList properties = new ArrayList ();
		private TextReader reader = null;
	        public bool verbose = false;

       		private void VerbosePrint (String text)
		{
			if (! verbose)
				return;

			Console.WriteLine ("IndexableImLog: " + text);
		}
			
		public IndexableImLog (ImLog _log, bool _verbose) 
		{
			log = _log;
			verbose = _verbose;

			Uri = log.Uri;
			Timestamp = log.Timestamp;
			Type = "IMLog";

			properties.Add (Property.NewKeyword ("fixme:file", log.LogFile));
			properties.Add (Property.NewKeyword ("fixme:offset", log.LogOffset));
			properties.Add (Property.NewDate ("fixme:starttime", log.StartTime));
			properties.Add (Property.NewKeyword ("fixme:speakingto", log.SpeakingTo));
			properties.Add (Property.NewKeyword ("fixme:identity", log.Identity));
		}

		override protected void DoBuild ()
		{
			log.Load ();

			properties.Add (Property.NewDate ("fixme:endtime", log.EndTime));

			StringBuilder text = new StringBuilder ();
			foreach (ImLog.Utterance utt in log.Utterances) {
				//Console.WriteLine ("[{0}][{1}]", utt.Who, utt.Text);
				text.Append (utt.Text);
				text.Append (" ");
			}

			reader = new StringReader (text.ToString ());
			Console.WriteLine (text.ToString ());
			VerbosePrint ("Indexing conversation");
		}

		override public TextReader GetTextReader ()
		{
			return reader;
		}

		override public IEnumerable Properties {
			get { return properties; }
		}
	}

	class IndexGaimLogsTool {

		static ArrayList indexables = new ArrayList ();
	        static bool verbose = false;

		static void AddLog (ImLog log)
		{
			indexables.Add (new IndexableImLog (log, verbose));
		}

		static void Main (string[] args)
		{
			foreach (string arg in args)
			    if (arg == "--verbose")
				verbose = true;
			
			string dir = Environment.GetEnvironmentVariable ("HOME");
			dir = Path.Combine (dir, ".gaim");
			dir = Path.Combine (dir, "logs");

			ImLogScanner scan = new GaimLogScanner ();
			scan.verbose = verbose;

			scan.Scan (dir, new ImLog.Sink (AddLog));

			if (indexables.Count > 0) {
			        Console.WriteLine ("Creating driver");
				IndexDriver driver = new IndexDriver ();
				
			        Console.WriteLine ("Adding indexables");
				driver.Add (indexables);
			        Console.WriteLine ("Optimizing");
				driver.Optimize ();
			}
		}
	}

}


