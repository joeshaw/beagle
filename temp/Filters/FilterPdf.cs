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
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/pdf"));
			SnippetMode = true;
		}

		// FIXME: we should have a reasonable failure mode if pdftotext is
		// not installed.

		protected override void DoPullProperties ()
		{
			// create new external process
			SafeProcess pc = new SafeProcess ();
			pc.Arguments = new string [] { "pdfinfo", FileInfo.FullName };
			pc.RedirectStandardOutput = true;
			pc.RedirectStandardError = true;

			try {
				pc.Start ();
			} catch (SafeProcessException e) {
				Log.Warn (e.Message);
				Error ();
				return;
			}

			// add pdfinfo's output to pool
			StreamReader pout = new StreamReader (pc.StandardOutput);
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
		
		protected override void DoPull ()
		{
			// create new external process
			SafeProcess pc = new SafeProcess ();
			pc.Arguments = new string [] { "pdftotext", "-q", "-nopgbrk", "-enc", "UTF-8", FileInfo.FullName, "-" };
			pc.RedirectStandardOutput = true;
			pc.RedirectStandardError = true;

			try {
				pc.Start ();
			} catch (SafeProcessException e) {
				Log.Warn (e.Message);
				Error ();
				return;
			}

			// add pdftotext's output to pool
			StreamReader pout = new StreamReader (pc.StandardOutput);

			// FIXME:  I don't think this is really required
			// Line by line parsing, however, we have to make
			// sure, that "pdftotext" doesn't output any "New-lines".
			string str;
			while ((str = pout.ReadLine()) != null) {
				AppendText (str);
				AppendStructuralBreak ();
				if (! AllowMoreWords ())
					break;
			}
			pout.Close ();

			pout = new StreamReader (pc.StandardError);
			while ((str = pout.ReadLine ()) != null)
				Log.Warn ("pdftotext [{0}]: {1}", Uri, str);

			pout.Close ();
			pc.Close ();
			Finished ();
		}
	}
}

