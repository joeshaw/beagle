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
		
		public IndexableImLog (ImLog _log) 
		{
			log = _log;

			Uri = log.Uri;
			Timestamp = log.Timestamp;
			Type = "IMLog";

			this ["File"] = log.LogFile;
			if (log.LogOffset > 0)
				this ["Offset"] = log.LogOffset.ToString ();
			this ["Protocol"] = log.Protocol;
			this ["StartTime"] = log.StartTime.ToString ();
			this ["SpeakingTo"] = log.SpeakingTo;
			this ["Identity"] = log.Identity;
		}

		override protected void DoBuild ()
		{
			log.Load ();
			this ["EndTime"] = log.EndTime.ToString ();

			StringBuilder text = new StringBuilder ();
			foreach (ImLog.Utterance utt in log.Utterances) {
				text.Append (utt.Text);
				text.Append (" ");
			}
			Content = text.ToString ();
		}
	}

	class IndexGaimLogsTool {

		static ArrayList indexables = new ArrayList ();

		static void AddLog (ImLog log)
		{
			indexables.Add (new IndexableImLog (log));
		}

		static void Main (string[] args)
		{
			string dir = Environment.GetEnvironmentVariable ("HOME");
			dir = Path.Combine (dir, ".gaim");
			dir = Path.Combine (dir, "logs");

			ImLogScanner scan = new GaimLogScanner ();
			scan.Scan (dir, new ImLog.Sink (AddLog));

			if (indexables.Count > 0) {
				IndexDriver driver = new IndexDriver ();
				driver.Add (indexables);
				driver.Optimize ();
			}
		}
	}

}


