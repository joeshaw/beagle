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

namespace Beagle.Filters {
    
	public class FilterDOC : Beagle.Daemon.Filter {

		string FileName;

		//////////////////////////////////////////////////////////

		private delegate void TextHandlerCallback (IntPtr byteArray, int dataLen, bool hotText);
		//private delegate void TextHandlerCallback (string data, int dataLen, bool hotText);
		
		[DllImport ("wv1glue")]
		private static extern int wv1_glue_init_doc_parsing (string fname, TextHandlerCallback callback);

		[DllImport ("wv1glue")]
		private static extern IntPtr wv1_glue_get_ole_stream (string fname);

		[DllImport ("wv1glue")]
		private static extern IntPtr wv1_glue_get_ole_summary_stream (IntPtr oleStream);

		[DllImport ("wv1glue")]
		private static extern string wv1_glue_get_title (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern string wv1_glue_get_subject (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_author (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern string wv1_glue_get_keywords (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_comments (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_template (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_lastsavedby (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_revision_number (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern string wv1_glue_get_appname (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern Int32 wv1_glue_get_page_count (IntPtr smryStream);

		[DllImport ("wv1glue")]
	        private static extern Int32 wv1_glue_get_word_count (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern Int32 wv1_glue_get_character_count (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern Int32 wv1_glue_get_security (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern Int16 wv1_glue_get_codepage (IntPtr smryStream);

		[DllImport ("wv1glue")]
		private static extern void wv1_glue_close_stream (IntPtr oleStream, IntPtr summary);

		//////////////////////////////////////////////////////////

		public FilterDOC () 
		{
			AddSupportedMimeType ("application/msword");
			AddSupportedMimeType ("application/vnd.ms-word");
			AddSupportedMimeType ("application/x-msword");
			SnippetMode = true;
		}
		
  		private void IndexText (IntPtr byteArray, int dataLen, bool hotText)
  		{
			if (byteArray != IntPtr.Zero) {
				byte[] data = new byte[dataLen];
				Marshal.Copy (byteArray, data, 0, dataLen);
				if (hotText)
					HotUp();
				AppendText (System.Text.Encoding.UTF8.GetString(data, 0, dataLen));
			
				if (hotText)
					HotDown();
			}
  		}
		override protected void DoOpen (FileInfo info)
		{
			FileName = info.FullName;
		}
		override protected void DoPullProperties ()
		{
			IntPtr oleStream;
			IntPtr oleSummaryStream;
			string strProp = null;
			Int32 intProp = 0;
			
			oleStream = wv1_glue_get_ole_stream (FileName);
			if (oleStream == IntPtr.Zero) {
				Logger.Log.Error ("Could not open OLE stream {0}", FileName);
				return;
			}
			oleSummaryStream = wv1_glue_get_ole_summary_stream (oleStream);
			if (oleSummaryStream == IntPtr.Zero) {
				Logger.Log.Error ("Could not open OLE Meta data stream from {0}", FileName);
				return;
			}
			strProp = wv1_glue_get_title (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("dc:title", strProp ));
			strProp = null;
			
			strProp = wv1_glue_get_subject (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("dc:subject", strProp ));
			strProp = null;

			strProp = wv1_glue_get_comments (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("dc:comment", strProp));
			strProp = null;

			strProp = wv1_glue_get_author (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:author", strProp));
			strProp = null;

			strProp = wv1_glue_get_lastsavedby (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:lastsavedby", strProp));
			strProp = null;
			
			strProp = wv1_glue_get_appname (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:appname", strProp));
			strProp = null;
			
			strProp = wv1_glue_get_keywords (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:keywords", strProp));
			strProp = null;
			
			strProp = wv1_glue_get_template (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:template", strProp));
			strProp = null;

			strProp = wv1_glue_get_revision_number (oleSummaryStream);
			if (strProp != null && strProp.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:revisionnumber", strProp));
			
			intProp = wv1_glue_get_page_count (oleSummaryStream);
			if (intProp != 0)	
				AddProperty (Beagle.Property.New ("fixme:page-count", intProp ));
			intProp = 0;
			
			intProp = wv1_glue_get_word_count (oleSummaryStream);
			if (intProp != 0)
				AddProperty (Beagle.Property.New ("fixme:word-count", intProp));
			intProp = 0;

			wv1_glue_close_stream (oleStream, oleSummaryStream);
		}

		override protected void DoPull ()
		{
			int ret;
			TextHandlerCallback textHandler;
			textHandler = new TextHandlerCallback (IndexText);
			
			ret = wv1_glue_init_doc_parsing (FileName, textHandler);

			if (ret == -2)
				Logger.Log.Error ("{0} : is password protected", FileName);
			else if (ret == -1)
				Logger.Log.Error ("{0} : Unable to read", FileName);
			else if (ret == -3)
				Logger.Log.Error ("Unable to initiate the parser for {0}", FileName);
			Finished ();
		}
	}
}
