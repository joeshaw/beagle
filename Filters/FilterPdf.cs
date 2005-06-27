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
			Process pc = new Process ();
			pc.StartInfo.FileName = "pdfinfo";
			// FIXME: We probably need to quote special chars in the path
			pc.StartInfo.Arguments = String.Format (" \"{0}\"", FileInfo.FullName);
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.UseShellExecute = false;
			try {
				pc.Start ();
			} catch (System.ComponentModel.Win32Exception) {
				Logger.Log.Warn ("Unable to find pdfinfo in path; PDF file not indexed.");
				Finished ();
				return;
			}
			
			// add pdfinfo's output to pool
			StreamReader pout = pc.StandardOutput;
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
							AddProperty (Beagle.Property.NewKeyword (strMetaTag, 
												 tokens[1].Trim()));
						else
							AddProperty (Beagle.Property.New (strMetaTag, 
											  tokens[1].Trim()));
					}
						
				}
			}
			pout.Close ();
			pc.Close ();
		}
		
		protected override void DoPull ()
		{
			// create new external process
			Process pc = new Process ();
			pc.StartInfo.FileName = "pdftotext";
			// FIXME: We probably need to quote special chars in the path
			pc.StartInfo.Arguments = String.Format ("-nopgbrk -enc UTF-8 \"{0}\" -", FileInfo.FullName);
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.UseShellExecute = false;
			try {
				pc.Start ();
			} catch (System.ComponentModel.Win32Exception) {
				Logger.Log.Warn ("Unable to find pdftotext in path; PDF file not indexed.");
				Finished ();
				return;
			}

			// add pdftotext's output to pool
			StreamReader pout = pc.StandardOutput;

			// FIXME:  I don't think this is really required
			// Line by line parsing, however, we have to make
			// sure, that "pdftotext" doesn't output any "New-lines".
			string str;
			while ((str = pout.ReadLine()) != null) {
				AppendText (str);
				AppendStructuralBreak ();
			}
			pout.Close ();
			pc.Close ();
			Finished ();
		}
	}
}
