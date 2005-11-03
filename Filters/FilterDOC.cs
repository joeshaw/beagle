//
// FilterDOC.cs : Trivial implementation of a MS Word-document filter.
//                This filter uses wv1 library - http://wvware.sourceforge.net/
//
// Author: Veerapuram Varadhan <vvaradhan@novell.com>
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
using System.IO;
using System.Runtime.InteropServices;

using Beagle.Util;
using Beagle.Daemon;

using Gsf;

namespace Beagle.Filters {
    
	public class FilterDOC : FilterOle {

		//////////////////////////////////////////////////////////

		private delegate void TextHandlerCallback (IntPtr byteArray, int dataLen, 
							   IntPtr byteHotArray, int hotDataLen,
							   bool appendStructBrk);
		
		[DllImport ("libbeagleglue")]
		private static extern int wv1_glue_init_doc_parsing (string fname, TextHandlerCallback callback);

		[DllImport ("libbeagleglue")]
		private static extern int wv1_init ();

		//////////////////////////////////////////////////////////

		static bool wv1_Initted = false;

		public FilterDOC () 
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/msword"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/vnd.ms-word"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-msword"));
			SnippetMode = true;
		}
		
  		private void IndexText (IntPtr byteArray, int dataLen, 
					IntPtr byteHotArray, int hotDataLen,
					bool appendStructBrk)
  		{
			byte[] data = null;
			string str = null;
			string strHot = null;

			try {
				if (dataLen > 0){
					data = new byte[dataLen];
					Marshal.Copy (byteArray, data, 0, dataLen);
				}
			
				if (data != null)
					str = System.Text.Encoding.UTF8.GetString (data, 0, dataLen);

				data = null;
				if (hotDataLen > 0) {
					data = new byte [hotDataLen];
					Marshal.Copy (byteHotArray, data, 0, hotDataLen);
				}
				if (data != null)
					strHot = System.Text.Encoding.UTF8.GetString (data, 0, hotDataLen);
			
				AppendText (str, strHot);
			
				if (appendStructBrk)
					AppendStructuralBreak ();
			} catch (Exception e) {
				Logger.Log.Debug ("Exception occurred in Word-Doc filter. {0}", e);
			}
  		}

		override protected void OpenStorage (FileInfo info)
		{
			FileName = info.FullName;
		}

		override protected void ExtractMetaData (Gsf.Input sumStream, Gsf.Input docSumStream)
		{
			int count = 0;
			DocProp prop = null;

			if (sumMeta != null) {
				prop = sumMeta.Lookup ("gsf:word-count");
				if (prop != null)
					count = (int) prop.Val;
				if (count > 0)
					AddProperty (Beagle.Property.NewKeyword ("fixme:word-count", count));

				count = 0;
				prop = sumMeta.Lookup ("gsf:page-count");		
				if (prop != null)
					count = (int) prop.Val;
				if (count > 0)
					AddProperty (Beagle.Property.NewKeyword ("fixme:page-count", count));
			}
		}

		override protected void DoPull ()
		{
			int ret;
			TextHandlerCallback textHandler;
			textHandler = new TextHandlerCallback (IndexText);

			if (!wv1_Initted) {
				wv1_init ();
				wv1_Initted = true;
			}

			Stopwatch stopwatch = new Stopwatch ();
			stopwatch.Start ();

			ret = wv1_glue_init_doc_parsing (FileName, textHandler);
			if (ret == -2)
				Logger.Log.Error ("{0} : is password protected", FileName);
			else if (ret == -1)
				Logger.Log.Error ("{0} : Unable to read", FileName);
			else if (ret == -3)
				Logger.Log.Error ("Unable to initiate the parser for {0}", FileName);
			stopwatch.Stop ();
			Logger.Log.Info ("Word document extraction done in {0}", stopwatch);
			Finished ();
		}
	}
}
