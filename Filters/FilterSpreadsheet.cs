//
// FilterSpreadSheet.cs
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
using System.Diagnostics;
using System.IO;
using System.Xml;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {
    
	public class FilterSpreadsheet : Filter {

		XmlTextReader xmlReader;
		bool ignoredFirst2lines = false;
		public FilterSpreadsheet () 
		{
			SnippetMode = true;
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-gnumeric"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/csv"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/tab-separated-values"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/comma-separated-values"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/csv"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/spreadsheet"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/tab-separated-values"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("text/x-comma-separated-values"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/vnd.ms-excel"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/excel"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-msexcel"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-excel"));
		}

		void WalkContentNodes (XmlReader reader) 
		{
			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.Text:
					AppendText (reader.Value);
					AppendStructuralBreak ();
					break;
				}
			}
		}
		
		override protected void DoOpen (FileInfo info)
		{
			ignoredFirst2lines = false;
		}

		override protected void DoPull ()
		{
			// create new external process
			Process pc = new Process ();
			pc.StartInfo.FileName = "ssindex";

			pc.StartInfo.Arguments = String.Format ("-i \"{0}\"", FileInfo.FullName);
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.UseShellExecute = false;
			try {
				pc.Start ();
			} catch (System.ComponentModel.Win32Exception) {
				Logger.Log.Warn ("Unable to find ssindex in path; {0} file not indexed.",
						 FileInfo.FullName);
				Finished ();
				return;
			}

			// process ssindex output
			StreamReader pout = pc.StandardOutput;
			if (!ignoredFirst2lines) {
				pout.ReadLine ();
				pout.ReadLine ();
				xmlReader = new XmlTextReader (pout);
				ignoredFirst2lines = true;
			}
			try {
				WalkContentNodes (xmlReader);
			} catch (Exception e) {
				Logger.Log.Debug ("Exception occurred while indexing {0}.", FileInfo.FullName);
				Logger.Log.Debug (e);
			}
			pout.Close ();
			pc.WaitForExit ();
			pc.Close ();
			Finished ();
		}
		
		override protected void DoClose ()
		{
			if (xmlReader != null)
				xmlReader.Close ();
		}
	}
}

