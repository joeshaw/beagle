
//
// MailCrawler.cs
//
// Copyright (C) 2004 Novell, Inc.
//
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Daemon.EvolutionMailDriver {
	
	class MailCrawler {
		ArrayList roots = new ArrayList ();

		Hashtable last_write_time_cache = new Hashtable ();
		ArrayList summaries = new ArrayList ();
		ArrayList mboxes = new ArrayList ();
		
		public MailCrawler (params string[] paths)
		{
			foreach (string p in paths) {
				if (Directory.Exists (p))
					roots.Add (p);
			}
		}

		private bool FileIsInteresting (FileInfo file)
		{
			DateTime cached_time = new DateTime ();
			if (last_write_time_cache.Contains (file.FullName))
				cached_time = (DateTime) last_write_time_cache [file.FullName];
			
			last_write_time_cache [file.FullName] = file.LastWriteTime;
			
			return cached_time < file.LastWriteTime;
		}

		public void Crawl ()
		{
			summaries.Clear ();
			mboxes.Clear ();

			Queue pending = new Queue ();

			foreach (string root in roots)
				pending.Enqueue (new DirectoryInfo (root));

			while (pending.Count > 0) {

				DirectoryInfo dir = pending.Dequeue () as DirectoryInfo;

				foreach (DirectoryInfo subdir in dir.GetDirectories ())
					pending.Enqueue (subdir);

				foreach (FileInfo file in dir.GetFiles ("*summary")) {
					if (file.Name == "summary") {
						if (FileIsInteresting (file))
							summaries.Add (file);
					} else if (file.Extension == ".ev-summary") {
						string mbox_name = Path.Combine (file.DirectoryName,
										 Path.GetFileNameWithoutExtension (file.Name));
						FileInfo mbox_file = new FileInfo (mbox_name);
						if (FileIsInteresting (mbox_file))
							mboxes.Add (mbox_file);
					}
				}
			}
		}

		public ICollection Summaries {
			get { return summaries; } 
		}

		public ICollection Mboxes {
			get { return mboxes; }
		}
	}
}
