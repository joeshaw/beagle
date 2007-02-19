//
// FilterPdf.cs: Very simplistic PDF filter
//
// Author:
//   Christopher Orr <dashboard@protactin.co.uk>
//
// Copyright 2004 by Christopher Orr
//

using System;
using System.IO;
using System.Diagnostics;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterPdf : Beagle.Daemon.Filter {

		public FilterPdf ()
		{
			SnippetMode = true;
			SetFileType ("document");
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/pdf"));
		}

		// FIXME: we should have a reasonable failure mode if pdftotext is
		// not installed.

		SafeProcess pc = null;
		StreamReader pout = null;

		protected override void DoPullProperties ()
		{
			// create new external process
			pc = new SafeProcess ();
			pc.Arguments = new string [] { "pdfinfo", FileInfo.FullName };
			pc.RedirectStandardOutput = true;
			pc.RedirectStandardError = true;

			// Let pdfinfo run for at most 10 CPU seconds, and not
			// use more than 100 megs memory.
			pc.CpuLimit = 90;
			pc.MemLimit = 100*1024*1024;

			try {
				pc.Start ();
			} catch (SafeProcessException e) {
				Log.Warn (e.Message);
				Error ();
				return;
			}

			// add pdfinfo's output to pool
			pout = new StreamReader (pc.StandardOutput);
			string str = null;
			string[] tokens = null;
			string strMetaTag = null;
			bool bKeyword = false;

			while ((str = pout.ReadLine ()) != null) {
				bKeyword = false;
				strMetaTag = null;
				tokens = str.Split (':');
				if (tokens.Length > 1) {
					switch (tokens[0]) {
					case "Title":
						strMetaTag = "dc:title";
						break;
					case "Author":
						strMetaTag = "dc:author";
						break;
					case "Pages":
						strMetaTag = "fixme:page-count";
						bKeyword = true;
						break;
					case "Creator":
						strMetaTag = "dc:creator";
						break;
					case "Producer":
						strMetaTag = "dc:appname";
						break;
					}
					if (strMetaTag != null) {
						if (bKeyword)
							AddProperty (Beagle.Property.NewUnsearched (strMetaTag, 
												 tokens[1].Trim()));
						else
							AddProperty (Beagle.Property.New (strMetaTag, 
											  tokens[1].Trim()));
					}
						
				}
			}
			pout.Close ();

			// Log any errors or warnings from stderr
			pout = new StreamReader (pc.StandardError);
			while ((str = pout.ReadLine ()) != null)
				Log.Warn ("pdfinfo [{0}]: {1}", Uri, str);

			pout.Close ();
			pc.Close ();
		}
		
		bool pull_started = false;

		private bool InitDoPull ()
		{
			// create new external process
			pc = new SafeProcess ();
			pc.Arguments = new string [] { "pdftotext", "-q", "-nopgbrk", "-enc", "UTF-8", FileInfo.FullName, "-" };
			pc.RedirectStandardOutput = true;

			// FIXME: This should really be true, and we should
			// process the output.  But we can deadlock when
			// pdftotext is blocked writing to stderr because of a
			// full buffer and we're blocking while reading from
			// stdout.
			pc.RedirectStandardError = false;

			// Let pdftotext run for at most 90 CPU seconds, and not
			// use more than 100 megs memory.
			pc.CpuLimit = 90;
			pc.MemLimit = 100*1024*1024;

			try {
				pc.Start ();
			} catch (SafeProcessException e) {
				Log.Warn (e.Message);
				Error ();
				return false;
			}

			// add pdftotext's output to pool
			pout = new StreamReader (pc.StandardOutput);
			pull_started = true;

			return true;
		}

		protected override void DoPull ()
		{
			// InitDoPull() calls Error() if it fails
			if (! pull_started && ! InitDoPull ())
				return;

			int n = 0;

			// Using internal information: Lucene currently asks for char[2048] data
			while (n <= 2048) {

				// FIXME:  I don't think this is really required
				// Line by line parsing, however, we have to make
				// sure, that "pdftotext" doesn't output any "New-lines".
				string str = pout.ReadLine ();
				if (str == null) {
					Finished ();
					return;
				} else {
					AppendLine (str);
					AppendStructuralBreak ();
					// If we have added 2048 chars, stop
					// DoPull is called repeatedly till the buffer is full,
					// so stop after the buffer is full (and possibly overflown)
					// to reduce number of function calls
					n += str.Length;
					n ++; // for the structural break
				}
			}
		}

		override protected void DoClose ()
		{
			if (! pull_started)
				return;

			pout.Close ();
#if false
			// FIXME: See FIXME above.
			pout = new StreamReader (pc.StandardError);

			string str;
			while ((str = pout.ReadLine ()) != null)
				Log.Warn ("pdftotext [{0}]: {1}", Uri, str);

			pout.Close ();
#endif
			pc.Close ();
		}
	}
}

