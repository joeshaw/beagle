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

namespace Beagle.Filters {

	public class FilterPdf : Beagle.Daemon.Filter {

		public FilterPdf ()
		{
			AddSupportedMimeType ("application/pdf");
		}

		// FIXME: we should have a reasonable failure mode if pdftotext is
		// not installed.

		protected override void DoPull ()
		{
			// get full file path from Filter
			string path = CurrentFileInfo.Directory +"/"+ CurrentFileInfo.Name;

			// create new external process
			Process pc = new Process ();
			pc.StartInfo.FileName = "pdftotext";
			// FIXME: We probably need to quote special chars in the path
			pc.StartInfo.Arguments = "-enc UTF-8 \""+ path +"\" -";
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.UseShellExecute = false;
			pc.Start ();

			// add pdftotext's output to pool
			StreamReader pout = pc.StandardOutput;
			AppendText (pout.ReadToEnd ());
			pout.Close ();
			pc.Close ();
			
			Finished ();
		}
	}
}
