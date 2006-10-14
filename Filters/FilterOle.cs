//
// FilterOle.cs
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
using System.Collections;
using System.IO;
using System.Text;
using Gsf;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {
    
	public abstract class FilterOle : Beagle.Daemon.Filter {

		public FilterOle () 
		{
		}

		protected virtual void ExtractMetaData (Gsf.Input sum_stream, 
							Gsf.Input doc_stream) 
		{ 
		}
		
		protected virtual void OpenStorage (FileInfo info) {}

		protected Infile file;
		protected DocMetaData sumMeta = null;
		protected DocMetaData docSumMeta = null;
		protected string FileName;

		override protected void DoOpen (FileInfo info)
		{
			try {
				Gsf.Global.Init ();

				Input input = new InputStdio (info.FullName);
				
				if (input != null) {
					Input uncompressed_input = input.Uncompress();
					input.Dispose ();
					file = new InfileMSOle (uncompressed_input);
					uncompressed_input.Dispose ();
				}
				
				if (input == null || file == null) {
					Logger.Log.Error ("Unable to open [{0}] ",info.FullName);
					Console.WriteLine ("input/file is null");
					Error ();
					return;
				}
				
				OpenStorage (info);
			} catch (Exception e) {
				Logger.Log.Error ("Unable to open "+info.FullName);
				Console.WriteLine ("{0}", e.Message);
				Error ();
				return;
			}
		}

		void PullMetaData (Gsf.Input sum_stream, Gsf.Input doc_stream) 
		{ 
			
			DocProp prop = null;
			string str = null;

			sumMeta = new DocMetaData ();
			if (sum_stream != null)
				Msole.MetadataRead (sum_stream, sumMeta);
			else
				Logger.Log.Warn ("SummaryInformationStream not found in {0}", FileName);

			docSumMeta = new DocMetaData ();
			if (doc_stream != null)
				Msole.MetadataRead (doc_stream, docSumMeta);
			else
				Logger.Log.Warn ("DocumentSummaryInformationStream not found in {0}", FileName);

			if (sumMeta != null) {
				prop = sumMeta.Lookup ("dc:title");
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("dc:title", str));

				str = null;
				prop = sumMeta.Lookup ("dc:subject");			
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("dc:subject", str));

				str = null;
				prop = sumMeta.Lookup ("dc:description");		
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("dc:description", str));

				str = null;
				prop = sumMeta.Lookup ("gsf:keywords");
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:keywords", str));

				str = null;
				prop = sumMeta.Lookup ("gsf:creator");
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:author", str));

				str = null;
				prop = sumMeta.Lookup ("gsf:last-saved-by");		
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:last-saved-by", str));

				str = null;
				prop = sumMeta.Lookup ("gsf:generator");		
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:generator", str));

				str = null;
				prop = sumMeta.Lookup ("gsf:template");		
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:template", str));
			}
			
			if (docSumMeta != null) {
				str = null;
				prop = docSumMeta.Lookup ("gsf:company");
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:company", str));

				str = null;
				prop = docSumMeta.Lookup ("gsf:category");
				if (prop != null)
					str = prop.Val as string;
				if (str != null && str.Length > 0)
					AddProperty (Beagle.Property.New ("fixme:category", str));

			}

			ExtractMetaData (sum_stream, doc_stream);
			
			if (sumMeta != null)
				sumMeta.Dispose ();
			
			if (docSumMeta != null)
				docSumMeta.Dispose ();
		}

		override protected void DoPullProperties ()
		{
			Input sum_stream = null;
			Input doc_stream = null;
			string str = null;
			int childCount = 0;
			int found = 0;
			
			if (file == null) {
				Finished ();
				return;
			}
			
			try {
				sum_stream = file.ChildByName ("\u0005SummaryInformation");
				doc_stream = file.ChildByName ("\u0005DocumentSummaryInformation");

				PullMetaData (sum_stream, doc_stream);
			} catch (Exception e) {
				Logger.Log.Error (e, "Exception occurred duing DoPullProperties.");
				Error ();
			} finally {
				if (sum_stream != null)
					sum_stream.Dispose ();
				if (doc_stream != null)
					doc_stream.Dispose ();
			}
		}

		override protected void DoClose ()
		{
			if (file != null)
				file.Dispose ();
			
			Log.Debug ("File should be closed now or very shortly.");
			
			// FIXME: Uncomment this when Shutdown() is available in gsf#
			// Gsf.Global.Shutdown ();
		}

		// FIXME: These are utility functions and can be useful 
		// outside this filter as well.
		public static uint GetInt32 (byte [] data, int offset) {
			return (uint)(data[offset] + (data[offset + 1] << 8) + (data[offset + 2] << 16) + (data[offset + 3] << 24));
		}
		public static ushort GetInt16 (byte [] data, int offset) {
			return (ushort)(data[offset] + (data[offset + 1] << 8));
		}

	}
}
