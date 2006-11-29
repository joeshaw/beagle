//
// FilterArchive.cs
//
// Copyright (C) 2004-2006 Novell, Inc.
//
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

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;

using Beagle;
using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {

	public class FilterArchive : Beagle.Daemon.Filter {

		Archive archive = null;

		public FilterArchive ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/zip"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-bzip-compressed-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-compressed-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-tgz"));
			//AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-gzip"));
			//AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-bzip"));
		}
		
		protected override void DoOpen (FileInfo info)
		{
			archive = new Archive (info.FullName, MimeType);
		}
		
		protected override void DoPullProperties ()
		{
			// FIXME: Fetch the archive properties.
		}

		protected override void DoPullSetup ()
		{
			ArchiveEntry a_entry;

			while ((a_entry = archive.GetNextEntry ()) != null) {
				// FIXME: For nested archives, create uid:foo#bar
				// instead of uid:foo#xxx#bar (avoid duplicates ?)
				Indexable child = new Indexable (new Uri (Uri.ToString () + "#" + a_entry.Uri, true));

				child.CacheContent = false;
				child.MimeType = a_entry.MimeType;
				child.ContentUri = UriFu.PathToFileUri (a_entry.TempFileName);
				child.DeleteContent = true;

				//Log.Debug ("Creating child with uri={0}, content-uri={1}",
				//	a_entry.Uri,
				//	child.ContentUri);
				//Console.WriteLine ("Name: {0}, MimeType: {1}", sub_uri, child.MimeType);

				if (a_entry.Comment != null)
					child.AddProperty (Property.New ("fixme:comment", a_entry.Comment));
				child.AddProperty (Property.New ("fixme:name", a_entry.Name));
				child.AddProperty (Property.NewKeyword ("fixme:size", a_entry.Size));
				child.AddProperty (Property.New ("fixme:relativeuri", a_entry.Uri));
				foreach (Property prop in Property.StandardFileProperties (Path.GetFileName (a_entry.Name), false))
					child.AddProperty (prop);

				//child.SetBinaryStream (File.OpenRead (a_entry.Name));

				AddChildIndexable (child);
			}
		}
		
		protected override void DoClose () 
		{
			if (this.archive != null)
				archive.Close ();
		}

		private class ArchiveEntry {
			private string name;
			private long size;
			private string mimetype;
			private bool is_directory;
			private DateTime modified;
			private string uri;
			private string comment;
			private string temp_file_name;
		
			public ArchiveEntry ()
			{
				name = null;
				size = 0;
				mimetype = null;
				is_directory = false;
				modified = DateTimeUtil.UnixToDateTimeUtc (0);
				comment = null;
				temp_file_name = null;
			}

			public string Name {
				get {
					return name;
				}

				set {
					name = value;
				}
			}

			public string MimeType {
				get {
					return mimetype;
				}

				set {
					mimetype = value;
				}
			}

			public System.DateTime Modified {
				get {
					return modified;
				}

				set {
					modified = value;
				}
			}

			public long Size {
				get {
					return size;
				}

				set {
					size = value;
				}
			}

			public bool IsDirectory {
				get {
					return is_directory;
				}

				set {
					is_directory = value;
				}
			}

			public string Uri {
				get {
					return uri;
				}

				set {
					uri = value;
				}
			}
		
			public string Comment {
				get {
					return comment;
				}
			
				set {
					comment = value;
				}
			}

			public string TempFileName {
				get {
					return temp_file_name;
				}

				set {
					temp_file_name = value;
				}
			}

		}

		private class Archive : Stream
		{
			Stream base_stream = null;
			string uri = null;
			string name = null;
			string method = null;

			delegate ArchiveEntry GetNextEntryType ();
			GetNextEntryType getNextEntry;
			bool first = true;

			public Archive (string filename, string mimeType) {
				this.uri = UriFu.PathToFileUriString (filename);
				name = filename;
				base_stream = new FileStream (filename,
							     FileMode.Open,
							     FileAccess.Read);
				Init (base_stream, mimeType);

			}

			private void Init (Stream stream, string mimeType) {

				switch (mimeType) {
				case "application/zip":
					base_stream = new ZipInputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntryZip);
					method = "zip:";
					break;
				case "application/x-bzip-compressed-tar":
					base_stream = new BZip2InputStream (base_stream);
					base_stream = new TarInputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntryTar);
					method = "bzip2tar:";
					break;
				case "application/x-compressed-tar":
				case "application/x-tgz":
					base_stream = new GZipInputStream (base_stream);
					base_stream = new TarInputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntryTar);
					method = "gziptar:";
					break;
				case "application/x-tar":
					base_stream = new TarInputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntryTar);
					method = "tar:";
					break;
				case "application/x-gzip":
					base_stream = new GZipInputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntrySingle);
					method = "gzip:";
					break;
				case "application/x-bzip":
					base_stream = new BZip2InputStream (base_stream);
					getNextEntry = new GetNextEntryType (GetNextEntrySingle);
					method = "bzip:";
					break;
				default:
					throw new ArgumentException ("Invalid or unsupported mime type.");
				}
			}

			/*
			public string GetNextEntrySingleAsUri () {
				if (first) {
					first = false;
					return uri + method;
				} else
					return null;
			}
			*/

			/* Returns stream of the next entry */
			public ArchiveEntry GetNextEntry () 
			{
				return getNextEntry ();
			}

			private String GetEntryFile (string entry_name)
			{			
				string temp_file = null;
				//temp_file = Path.Combine (Path.GetTempPath (), entry_name); 
				temp_file = Path.GetTempFileName ();
				File.Delete (temp_file);
				temp_file =  temp_file + Path.GetExtension (entry_name);
				Console.WriteLine ("GetEntryFile: temp_file = {0}", temp_file);
				FileStream filestream = File.OpenWrite (temp_file);

				BufferedStream buffered_stream = new BufferedStream (filestream);
				BinaryWriter writer = new BinaryWriter (buffered_stream);

				const int BUFFER_SIZE = 8192;
				byte[] data = new byte [BUFFER_SIZE];

				int read;
				do {
					read = base_stream.Read (data, 0, BUFFER_SIZE);
					if (read > 0)
						writer.Write (data, 0, read);
				} while (read > 0);
			
				writer.Close ();

				File.SetLastWriteTime (temp_file, File.GetLastWriteTime (this.name));
				return temp_file;
			}

			public ArchiveEntry GetNextEntryZip () {
				ArchiveEntry a_entry = null;

				ZipInputStream inputStream = base_stream as ZipInputStream;
				ZipEntry entry = inputStream.GetNextEntry();
				if (entry != null && !entry.IsDirectory) {
					a_entry = new ArchiveEntry ();
					a_entry.Uri = /* DB uri + method + */entry.Name;
					a_entry.Size = entry.Size;
					a_entry.Modified  = entry.DateTime;
					a_entry.IsDirectory = entry.IsDirectory;
					a_entry.Name = entry.Name;
					a_entry.TempFileName = GetEntryFile (entry.Name);
					a_entry.MimeType = Beagle.Util.XdgMime.GetMimeType (a_entry.TempFileName);
					a_entry.Comment = entry.Comment;
				}
				return a_entry;
			}

			public ArchiveEntry GetNextEntrySingle () {
				if (first) {
					first = false;
					return null;
				} else
					return null;
			}

			public ArchiveEntry GetNextEntryTar () {
				ArchiveEntry a_entry = null;
				TarInputStream inputStream = base_stream as TarInputStream;
				TarEntry entry = inputStream.GetNextEntry();
				if (entry != null && !entry.IsDirectory) {
					a_entry = new ArchiveEntry ();
					a_entry.Uri = /* DB uri + method + */entry.Name;
					a_entry.Size = entry.Size;
					a_entry.Modified  = entry.ModTime;
					a_entry.IsDirectory = entry.IsDirectory;
					a_entry.Name = entry.Name;
					a_entry.TempFileName = GetEntryFile (entry.Name);
					a_entry.MimeType = Beagle.Util.XdgMime.GetMimeType (a_entry.TempFileName);
				}
				return a_entry;
			}

			public override int Read (byte[] buffer, int offset, int length) {
				return base_stream.Read (buffer, offset, length);
			}

			public override IAsyncResult BeginRead (byte[] buffer, int offset, int length,
								AsyncCallback cback, object state)
			{
				return base_stream.BeginRead (buffer, offset, length, cback, state);
			}

			public override int EndRead(IAsyncResult async_result) {
				return base_stream.EndRead (async_result);
			}

			public override void Write (byte[] buffer, int offset, int length) {
				throw new NotSupportedException ();
			}
			public override void Flush () {
				throw new NotSupportedException ();
			}
			public override long Seek (long offset, SeekOrigin origin) {
				throw new NotSupportedException ();
			}
			public override void SetLength (long value) {
				throw new System.NotSupportedException();
			}
			public override bool CanRead {
				get {
					return base_stream.CanRead;
				}
			}
			public override bool CanSeek {
				get {
					return false;
				}
			}
			public override bool CanWrite {
				get {
					return false;
				}
			}
			public override long Length {
				get {
					throw new System.NotSupportedException();
				}
			}
			public override long Position {
				get {
					throw new System.NotSupportedException();
				}
				set {
					throw new System.NotSupportedException();
				}
			}
		}
	}	
}
