//
// Filter.cs
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
using System.Reflection;

using Beagle.Util;

namespace Beagle.Daemon {

	public class Filter {

		public enum ContentType {
			Text,
			WhiteSpace,
			StructuralBreak
		}

		public delegate void GotPropertyHandler (Property prop);
		public delegate void GotContentHandler  (ContentType type, string content);

		public event GotPropertyHandler GotPropetyEvent;
		public event GotContentHandler  GotContentEvent;

		//////////////////////////

		// Derived classes always must have a constructor that
		// takes no arguments.
		public Filter () { }

		//////////////////////////

		private string identifier;

		public string Identifier {
			get { return identifier; }
			set { identifier = value; }
		}

		//////////////////////////

		private   ArrayList supported_mime_types = new ArrayList ();
		private   ArrayList supported_extensions = new ArrayList ();
		
		protected void AddSupportedMimeType (string mime_type)
		{
			supported_mime_types.Add (mime_type);
		}

		protected void AddSupportedExtension (string extension)
		{
			supported_extensions.Add (extension);
		}

		public IEnumerable SupportedMimeTypes {
			get { return supported_mime_types; }
		}

		public IEnumerable SupportedExtensions {
			get { return supported_mime_types; }
		}

		//////////////////////////

		// Filters are versioned.  This allows us to automatically re-index
		// files when a newer filter is available.

		public string Name {
			get { return this.GetType ().Name; }
		}

		private int version = -1;

		public int Version {
			get { return version < 0 ? 0 : version; }
		}

		protected void SetVersion (int v)
		{
			if (v < 0) {
				string msg;
				msg = String.Format ("Attempt to set invalid version {0} on Filter {1}", v, Name);
				throw new Exception (msg);
			}

			if (version != -1) {
				string msg;
				msg = String.Format ("Attempt to re-set version from {0} to {1} on Filter {2}", version, v, Name);
				throw new Exception (msg);
			}

			version = v;
		}

		//////////////////////////

		private string this_mime_type = null;
		private string this_extension = null;

		public string MimeType {
			get { return this_mime_type; }
			set { this_mime_type = value; }
		}

		public string Extension {
			get { return this_extension; }
			set { this_extension = value; }
		}

		//////////////////////////
		
		private bool crawl_mode = false;

		public void EnableCrawlMode ()
		{
			crawl_mode = true;
		}
		
		protected bool CrawlMode {
			get { return crawl_mode; }
		}

		//////////////////////////

		int hotCount = 0;
		int freezeCount = 0;

		public void HotUp ()
		{
			++hotCount;
		}
		
		public void HotDown ()
		{
			if (hotCount > 0) {
				--hotCount;
				if (hotCount == 0)
					AppendWhiteSpace ();
			}
		}

		public bool IsHot {
			get { return hotCount > 0; }
		}

		public void FreezeUp ()
		{
			++freezeCount;
		}

		public void FreezeDown ()
		{
			--freezeCount;
		}

		public bool IsFrozen {
			get { return freezeCount > 0; }
		}

		//////////////////////////
		
		private bool snippetMode = false;
		private bool originalIsText = false;
		private TextWriter snippetWriter = null;

		public bool SnippetMode {
			get { return snippetMode; }
			set { snippetMode = value; }
		}

		public bool OriginalIsText {
			get { return originalIsText; }
			set { originalIsText = value; }
		}
		
		public void AttachSnippetWriter (TextWriter writer)
		{
			if (snippetMode)
				snippetWriter = writer;
		}

		//////////////////////////

		private ArrayList textPool;
		private ArrayList hotPool;
		private ArrayList propertyPool;
		
		private int max_text_pool_len = 0;
		private int max_text_pool_size = 0;
		private int max_hot_pool_len = 0;
		

		private bool last_was_structural_break = true;

		public void AppendText (string str)
		{
			//Logger.Log.Debug ("AppendText (\"{0}\")", str);
			if (! IsFrozen && str != null && str != "") {

				// FIXME: We should be smarter about newlines
				if (str.IndexOf ('\n') != -1)
					str = str.Replace ("\n", " ");

				textPool.Add (str);
				if (IsHot)
					hotPool.Add (str);

				int pool_size = 0;
				foreach (string x in textPool)
					pool_size += x.Length;

				if (pool_size > max_text_pool_size)
					max_text_pool_size = pool_size;

				if (textPool.Count > max_text_pool_len)
					max_text_pool_len = textPool.Count;

				if (hotPool.Count > max_hot_pool_len)
					max_hot_pool_len = hotPool.Count;

				if (snippetWriter != null)
					snippetWriter.Write (str);

				last_was_structural_break = false;
			}
		}

		private bool NeedsWhiteSpace (ArrayList array)
		{
			if (array.Count == 0)
				return true;
			
			string last = (string) array [array.Count-1];
			if (last.Length > 0
			    && char.IsWhiteSpace (last [last.Length-1]))
				return false;

			return true;
		}

		public void AppendWhiteSpace ()
		{
			if (last_was_structural_break)
				return;

			//Logger.Log.Debug ("AppendWhiteSpace ()");
			if (NeedsWhiteSpace (textPool)) {
				textPool.Add (" ");
				if (snippetWriter != null)
					snippetWriter.Write (" ");
				last_was_structural_break = false;
			}
			if (NeedsWhiteSpace (hotPool))
				hotPool.Add (" ");
			
		}

		public void AddProperty (Property prop)
		{
			if (prop != null && prop.Value != null)
				propertyPool.Add (prop);
		}

		public void AppendStructuralBreak ()
		{
			if (snippetWriter != null && ! last_was_structural_break) {
				snippetWriter.WriteLine ();
				last_was_structural_break = true;
			}
		}

		//////////////////////////

		private bool isFinished = false;

		public bool IsFinished {
			get { return isFinished; }
		}
		
		protected void Finished ()
		{
			isFinished = true;
			if (max_text_pool_len > 0 || max_hot_pool_len > 0)
				Logger.Log.Debug ("{0} pull pool stats: {1} {2} / {3}",
						  Identifier, max_text_pool_len, max_text_pool_size, max_hot_pool_len);
		}

		//////////////////////////

		protected virtual void DoOpen (FileInfo info) { }

		protected virtual void DoPullProperties () { }

		protected virtual void DoPullSetup () { }

		protected virtual void DoPull () { Finished (); }

		protected virtual void DoClose () { }

		//////////////////////////

		/*
		  Open () calls:
		  (1) DoOpen (FileInfo info) or DoOpen (Stream)
		  (2) DoPullProperties ()
		  (3) DoPullSetup ()
		  At this point all properties must be in place

		  Once someone starts reading from the TextReader,
		  the following are called:
		  DoPull () [until Finished() is called]
		  DoClose () [when finished]
		  
		*/

		private bool isOpen = false;
		private string tempFile = null;
		private FileInfo currentInfo = null;
		private FileStream currentStream = null;
		private StreamReader currentReader = null;

		public void Open (Stream stream)
		{
			// If we are handed a stream, dump it into
			// a temporary file.
			tempFile = Path.GetTempFileName();
			Stream tempStream = File.OpenWrite (tempFile);

			const int BUFFER_SIZE = 8192;
			byte[] buffer = new byte [BUFFER_SIZE];
			int n;
			while ((n = stream.Read (buffer, 0, BUFFER_SIZE)) > 0) {
				tempStream.Write (buffer, 0, n);
			}

			tempStream.Close ();

			Open (new FileInfo (tempFile));
		}

		public void Open (FileInfo info)
		{
			isFinished = false;
			textPool = new ArrayList ();
			hotPool = new ArrayList ();
			propertyPool = new ArrayList ();

			currentInfo = info;

			// Open a stream for this file.
			currentStream = new FileStream (info.FullName,
							FileMode.Open,
							FileAccess.Read,
							FileShare.Read);

			// Our default assumption is sequential reads.
			// FIXME: Is this the right thing to do here?
			FileAdvise.IncreaseReadAhead (currentStream);

			// Give the OS a hint that we will be reading this
			// file soon.
			FileAdvise.PreLoad (currentStream);			

			DoOpen (info);
			isOpen = true;

			if (IsFinished) {
				isOpen = false;
				return;
			}
			
			DoPullProperties ();
			if (IsFinished) {
				isOpen = false;
				return;
			}

			// Close and reset our TextReader
			if (currentReader != null) {
				currentReader.Close ();
				currentReader = null;
			}

			// Seek back to the beginning of our stream
			currentStream.Seek (0, SeekOrigin.Begin);

			DoPullSetup ();
			if (IsFinished) {
				isOpen = false;
				return;
			}
		}

		public FileInfo FileInfo {
			get { return currentInfo; }
		}

		public Stream Stream {
			get { return currentStream; }
		}

		public TextReader TextReader {
			get {
				if (currentReader == null
				    && currentStream != null) {
					currentReader = new StreamReader (currentStream);
				}

				return currentReader;
			}
		}

		private bool Pull ()
		{
			if (IsFinished) {
				Close ();
				return false;
			}

			DoPull ();

			return true;
		}

		private bool closed = false;

		private void Close ()
		{
			if (currentStream == null)
				return;

			DoClose ();

			// When crawling, give the OS a hint that we don't
			// need to keep this file around in the page cache.
			if (CrawlMode)
				FileAdvise.FlushCache (currentStream);

			if (currentReader != null)
				currentReader.Close ();

			currentStream.Close ();
			currentStream = null;

			if (snippetWriter != null)
				snippetWriter.Close ();

			if (tempFile != null)
				File.Delete (tempFile);

		}

		private string PullFromArray (ArrayList array)
		{
			while (array.Count == 0 && Pull ()) { }
			if (array.Count > 0) {
				string str = (string) array [0];
				array.RemoveAt (0);
				return str;
			}
			return null;
		}

		private string PullText ()
		{
			return PullFromArray (textPool);
		}

		private string PullHotText ()
		{
			return PullFromArray (hotPool);
		}

		public TextReader GetTextReader ()
		{
			PullingReader pr = new PullingReader (new PullingReader.Pull (PullText));
			pr.Identifier = Identifier;
			return pr;
		}

		public TextReader GetHotTextReader ()
		{
			return null;
			//return new PullingReader (new PullingReader.Pull (PullHotText));
		}

		public IEnumerable Properties {
			get { return propertyPool; }
		}
	}
}
