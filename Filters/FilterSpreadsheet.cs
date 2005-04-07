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

namespace Beagle.Filters {
    
	public class FilterSpreadsheet : Beagle.Daemon.Filter {

		XmlTextReader xmlReader;
		bool ignoredFirst2lines = false;
		public FilterSpreadsheet () 
		{
			SnippetMode = true;
			AddSupportedMimeType ("application/x-gnumeric");
			AddSupportedMimeType ("application/csv");
			AddSupportedMimeType ("application/tab-separated-values");
			AddSupportedMimeType ("text/comma-separated-values");
			AddSupportedMimeType ("text/csv");
			AddSupportedMimeType ("text/spreadsheet");
			AddSupportedMimeType ("text/tab-separated-values");
			AddSupportedMimeType ("text/x-comma-separated-values");
			AddSupportedMimeType ("application/vnd.ms-excel");
			AddSupportedMimeType ("application/excel");
			AddSupportedMimeType ("application/x-msexcel");
			AddSupportedMimeType ("application/x-excel");
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
			Finished ();
		}
		
		override protected void DoClose ()
		{
			xmlReader.Close ();
		}
	}
}
