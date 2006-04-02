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

		static private bool Debug = false;
		// Lucene fields allow a maximum of 10000 words
		// Some of the words will be stop words... so a failsafe maximum of 40000 words
		// Dont accept more words than that
		const int MAXWORDS = 40000; // Lucene.Net.Index.IndexWriter.DEFAULT_MAX_FIELD_LENGTH * 4

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

		private ArrayList supported_flavors = new ArrayList ();
		
		protected void AddSupportedFlavor (FilterFlavor flavor) 
		{
			supported_flavors.Add (flavor);
		}

		public ICollection SupportedFlavors {
			get { return supported_flavors; }
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
		private ArrayList indexable_properties = null;

		public string MimeType {
			get { return this_mime_type; }
			set { this_mime_type = value; }
		}

		public string Extension {
			get { return this_extension; }
			set { this_extension = value; }
		}

		// allow the filter to access the properties
		// set by indexable
		public ArrayList IndexableProperties {
			get { return indexable_properties; }
			set { indexable_properties = value; }
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

		// Filters which deal with big files, and that don't need
		// to read in whole files may want to set this to false
		// to avoid wasting cycles in disk wait.

		private bool preload = true;

		protected bool PreLoad {
			get { return preload; }
			set { preload = value; }
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
			if (hotCount > 0)
				--hotCount;
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
			if (freezeCount > 0)
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

		private int word_count = 0;
		private int hotword_count = 0;

		protected bool AllowMoreWords ()
		{
			return (word_count < MAXWORDS);
		}
		
		private bool last_was_structural_break = true;

		// This two-arg AppendText() will give flexibility to
		// filters to segregate hot-contents and
		// normal-contents of a para and call this method with
		// respective contents.  
		//
		// str : Holds both the normal-contents and hot contents.
		// strHot: Holds only hot-contents.
		//
		// Ex:- suppose the actual-content is "one <b>two</b> three"
		// str = "one two three"
		// strHot = "two"
		//
		// NOTE: HotUp() or HotDown() has NO-EFFECT on this variant 
		// of AppendText ()
		
		public int AppendText (string str, string strHot)
		{
			int num_words = 0;

			if (!IsFrozen && word_count < MAXWORDS && str != null && str != "") {
				string[] lines;

				// Avoid unnecessary allocation of a string
				// FIXME: Handle \r, \r\n cases.
				if (str.IndexOf ('\n') > -1) {
					lines = str.Split ('\n'); 
					foreach (string line in lines) {
						if (line.Length > 0) {
							ReallyAppendText (line, null);
							AppendStructuralBreak ();
						}
					}
				} else 
					ReallyAppendText (str, null);
				num_words = StringFu.CountWords (str, 3, -1);
				word_count += num_words;
			}

			if (hotword_count < MAXWORDS) {
				ReallyAppendText (null, strHot);
				hotword_count += StringFu.CountWords (strHot, 3, -1);
			}

			return num_words;
		}
		
		public int AppendText (string str)
		{
			if (Debug)
				Logger.Log.Debug ("AppendText (\"{0}\")", str);

			if (! IsFrozen && str != null && str != "")
				return AppendText (str, IsHot ? str : null);

			return 0;
		}

		// Does adding text to to text/hot pools respectively.
		private void ReallyAppendText (string str, string strHot)
		{
			if (!IsFrozen && strHot != null && strHot != "")
				hotPool.Add (strHot.Trim()+" ");

			if (str != null) {
				textPool.Add (str);

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

			if (Debug)
				Logger.Log.Debug ("AppendWhiteSpace ()");

			if (NeedsWhiteSpace (textPool)) {
				textPool.Add (" ");
				if (snippetWriter != null)
					snippetWriter.Write (" ");
				last_was_structural_break = false;
			}
		}

		public void AddProperty (Property prop)
		{
			if (prop != null && prop.Value != null && prop.Value != "")
				propertyPool.Add (prop);
		}

		public void AppendStructuralBreak ()
		{
			if (snippetWriter != null && ! last_was_structural_break) {
				snippetWriter.WriteLine ();
				last_was_structural_break = true;
			}
			// When adding a "newline" to the textCache, we need to 
			// append a "Whitespace" to the text pool.
			if (NeedsWhiteSpace (textPool))
				textPool.Add (" ");
		}

		//////////////////////////

		private bool isFinished = false;

		public bool IsFinished {
			get { return isFinished; }
		}
		
		protected void Finished ()
		{
			isFinished = true;
		}

		private bool has_error = false;

		public bool HasError {
			get { return has_error; }
		}

		protected void Error ()
		{
			Cleanup (); // force the clean-up of temporary files on an error
			has_error = true;
		}

		//////////////////////////

		protected virtual void DoOpen (FileSystemInfo info) {
			if (info is FileInfo)
				DoOpen (info as FileInfo);
			else if (info is DirectoryInfo)
				DoOpen (info as DirectoryInfo);
		}

		protected virtual void DoOpen (FileInfo info) { }

		protected virtual void DoOpen (DirectoryInfo info) { }

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

		private string tempFile = null;
		private FileSystemInfo currentInfo = null;
		private FileStream currentStream = null;
		private StreamReader currentReader = null;

		public bool Open (TextReader reader)
		{
			tempFile = Path.GetTempFileName ();
                        FileStream file_stream = File.OpenWrite (tempFile);

			if (Debug)
				Logger.Log.Debug ("Storing text in tempFile {0}", tempFile);

                        // When we dump the contents of a reader into a file, we
                        // expect to use it again soon.
			FileAdvise.PreLoad (file_stream);

                        // Make sure the temporary file is only readable by the owner.
                        // FIXME: There is probably a race here.  Could some malicious program
                        // do something to the file between creation and the chmod?
                        Mono.Unix.Native.Syscall.chmod (tempFile, (Mono.Unix.Native.FilePermissions) 256);

                        BufferedStream buffered_stream = new BufferedStream (file_stream);
                        StreamWriter writer = new StreamWriter (buffered_stream);

                        const int BUFFER_SIZE = 8192;
                        char [] buffer = new char [BUFFER_SIZE];

                        int read;
                        do {
                                read = reader.Read (buffer, 0, BUFFER_SIZE);
                                if (read > 0)
                                        writer.Write (buffer, 0, read);
                        } while (read > 0);

                        writer.Close ();

			return Open (new FileInfo (tempFile));
		}
		
		public bool Open (Stream stream)
		{
			tempFile = Path.GetTempFileName ();
                        FileStream file_stream = File.OpenWrite (tempFile);
			
			if (Debug)
				Logger.Log.Debug ("Storing stream in tempFile {0}", tempFile);

                        // When we dump the contents of a reader into a file, we
                        // expect to use it again soon.
                        FileAdvise.PreLoad (file_stream);

                        // Make sure the temporary file is only readable by the owner.
                        // FIXME: There is probably a race here.  Could some malicious program
                        // do something to the file between creation and the chmod?
                        Mono.Unix.Native.Syscall.chmod (tempFile, (Mono.Unix.Native.FilePermissions) 256);

                        BufferedStream buffered_stream = new BufferedStream (file_stream);

                        const int BUFFER_SIZE = 8192;
                        byte [] buffer = new byte [BUFFER_SIZE];

                        int read;
                        do {
                                read = stream.Read (buffer, 0, BUFFER_SIZE);
                                if (read > 0)
                                        buffered_stream.Write (buffer, 0, read);
                        } while (read > 0);

                        buffered_stream.Close ();

			return Open (new FileInfo (tempFile));
		}

		public bool Open (FileSystemInfo info)
		{
			isFinished = false;
			textPool = new ArrayList ();
			hotPool = new ArrayList ();
			propertyPool = new ArrayList ();

			currentInfo = info;

			if (info is FileInfo) {
				// Open a stream for this file.
				currentStream = new FileStream (info.FullName,
								FileMode.Open,
								FileAccess.Read,
								FileShare.Read);
				
				if (preload) {
					// Our default assumption is sequential reads.
					// FIXME: Is this the right thing to do here?
					FileAdvise.IncreaseReadAhead (currentStream);
				
					// Give the OS a hint that we will be reading this
					// file soon.
					FileAdvise.PreLoad (currentStream);				
				}
			}

			try {
				DoOpen (info);

				if (IsFinished)
					return true;
				else if (HasError)
					return false;
				
				DoPullProperties ();
				
				if (IsFinished) 
					return true;
				else if (HasError)
					return false;
				
				// Close and reset our TextReader
				if (currentReader != null) {
					currentReader.Close ();
					currentReader = null;
				}
				
				// Seek back to the beginning of our stream
				currentStream.Seek (0, SeekOrigin.Begin);
				
				DoPullSetup ();
				
				if (HasError)
					return false;				
			} catch (Exception e) {
				Logger.Log.Warn ("Unable to filter {0}: {1}", info.FullName, e.Message);
				Cleanup (); // clean up temporary files on an exception
				return false;
			}

			return true;
		}

		public bool Open (string path)
		{
			if (File.Exists (path))
				return Open (new FileInfo (path));
			else if (Directory.Exists (path))
				 return Open (new DirectoryInfo (path));
			else 
				return false;
		}

		public FileInfo FileInfo {
			get { return currentInfo as FileInfo; }
		}

		public DirectoryInfo DirectoryInfo {
			get { return currentInfo as DirectoryInfo; }
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

		private void Close ()
		{
			Cleanup ();

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
		}

		public void Cleanup ()
		{
			if (tempFile != null) {
				try {
					File.Delete (tempFile);
				} catch (Exception ex) {
					// Just in case it is gone already
				}
				tempFile = null;
			}
		}

		private bool PullFromArray (ArrayList array, StringBuilder sb)
		{
			while (array.Count == 0 && Pull ()) { }

			if (array.Count > 0) {
				foreach (string str in array)
					sb.Append (str);

				array.Clear ();
				return true;
			}
			return false;
		}

		private bool PullTextCarefully (ArrayList array, StringBuilder sb)
		{
			bool pulled = false;
			try {
				pulled = PullFromArray (array, sb);
			} catch (Exception ex) {
				Logger.Log.Debug ("Caught exception while pulling text in filter '{0}'", Name);
				Logger.Log.Debug (ex);
			}

			return pulled;
		}

		private bool PullText (StringBuilder sb)
		{
			return PullTextCarefully (textPool, sb);
		}

		private bool PullHotText (StringBuilder sb)
		{
			return PullTextCarefully (hotPool, sb);
		}

		public TextReader GetTextReader ()
		{
			PullingReader pr = new PullingReader (new PullingReader.Pull (PullText));
			pr.Identifier = Identifier;
			return pr;
		}

		public TextReader GetHotTextReader ()
		{
			return new PullingReader (new PullingReader.Pull (PullHotText));
		}

		public IEnumerable Properties {
			get { return propertyPool; }
		}

		//////////////////////////////

		// This is used primarily for the generation of URIs for the
		// child indexables that can be created as a result of the
		// filtering process.

		private Uri uri = null;

		public Uri Uri {
			get { return uri; }
			set { uri = value; }
		}

		//////////////////////////////

		private ArrayList child_indexables = new ArrayList ();

		protected void AddChildIndexable (Indexable indexable)
		{
			this.child_indexables.Add (indexable);
		}
		
		protected void AddChildIndexables (ICollection indexables)
		{
			this.child_indexables.AddRange (indexables);
		}

		public ArrayList ChildIndexables {
			get { return this.child_indexables; }
		}
	}
}
