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

	[PropertyKeywordMapping (Keyword="inarchive", PropertyName="fixme:inside_archive", IsKeyword=true, Description="Use 'inarchive:true' for files inside an archive.")]
	public class FilterArchive : Beagle.Daemon.Filter {

		internal delegate ArchiveEntry GetNextEntry ();

		private FileInfo file_info;
		private Stream archive_stream;
		private GetNextEntry get_next_entry;
		private int total_bytes_read;

		// Fairly arbitrary limits
		private const int  MAX_CHILDREN    = 30;
		private const long MAX_SINGLE_FILE = 10*1024*1024;
		private const long MAX_ALL_FILES   = 25*1024*1024;

		public FilterArchive ()
		{
			// 1: Store entry names as text content
			SetVersion (1);
			SetFileType ("archive");
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/zip"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-bzip-compressed-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-compressed-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-tar"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-tgz"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-gzip"));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/x-bzip"));
		}
		
		protected override void DoOpen (FileInfo file_info)
		{
			this.file_info = file_info;
			this.total_bytes_read = 0;

			switch (MimeType) {
			case "application/zip":
				archive_stream = new ZipInputStream (Stream);
				get_next_entry = delegate () {
					try {
						return GetNextEntryZip ();
					} catch {
						return null;
					}
				};
				break;

			case "application/x-bzip-compressed-tar":
				archive_stream = new TarInputStream (new BZip2InputStream (Stream));
				get_next_entry = delegate () {
					try {
						return GetNextEntryTar ();
					} catch {
						return null;
					}
				};
				break;

			case "application/x-compressed-tar":
			case "application/x-tgz":
				archive_stream = new TarInputStream (new GZipInputStream (Stream));
				get_next_entry = delegate () {
					try {
						return GetNextEntryTar ();
					} catch {
						return null;
					}
				};
				break;

			case "application/x-tar":
				archive_stream = new TarInputStream (Stream);
				get_next_entry = delegate () {
					try {
						return GetNextEntryTar ();
					} catch {
						return null;
					}
				};
				break;

			case "application/x-gzip":
				archive_stream = new GZipInputStream (Stream);
				get_next_entry = delegate () {
					try {
						return GetNextEntrySingle ();
					} catch {
						return null;
					}
				};
				break;

			case "application/x-bzip":
				archive_stream = new BZip2InputStream (Stream);
				get_next_entry = delegate () {
					try {
						return GetNextEntrySingle ();
					} catch {
						return null;
					}
				};
				break;

			default:
				throw new ArgumentException ("Invalid or unsupported mime type.");
			}
		}
				
		protected override void DoPullProperties ()
		{
			// FIXME: Fetch the archive properties.
		}

		protected override void DoPullSetup ()
		{
			ArchiveEntry a_entry;
			int count = 0;
			long total_size = 0;

			while (count < MAX_CHILDREN
			       && total_size <= MAX_ALL_FILES
			       && (a_entry = this.get_next_entry ()) != null) {

				// Store file names in the archive
				AppendText (Path.GetFileName (a_entry.Name));
				AppendWhiteSpace ();

				// If this is an invalid or oversized entry, skip it.
				if (a_entry.TempFile == null)
					continue;

				++count;
				total_size += a_entry.Size;

				// FIXME: For nested archives, create uid:foo#bar
				// instead of uid:foo#xxx#bar (avoid duplicates ?)
				// FIXME: Construct correct Uri
				// 1. Use Uri (foo, true): foo is not escaped. This creates serialization error for uris containing non-ascii character
				// 2: Using Uri (foo): foo is escaped. This causes foo.zip#b#c (file c in archive b in archive foo) to be escaped to foo.zip#b%23c. This should be properly escaped back. Note that, file b#c in archive foo.zip would also be escaped to foo.zip#b%23c. If there is any problem about incorrect filename containing '#' in an archive, then System.Uri should be abandoned and a custom Beagle.Uri should be created which can handle multiple fragments.
				// If you read the Uri related FIXMEs and hacks in beagled/*Queryable,
				// then you would see why creating Beagle.Uri class over System.Uri
				// is not at all a bad idea :)
				Indexable child = new Indexable (new Uri (Uri.ToString () + "#" + a_entry.Name));

				child.CacheContent = true;
				child.MimeType = a_entry.MimeType;

				child.DisplayUri = new Uri (DisplayUri.ToString () + "#" + a_entry.Name);
				child.ContentUri = UriFu.PathToFileUri (a_entry.TempFile);
				child.DeleteContent = true;

				child.AddProperty (Property.NewBool ("fixme:inside_archive", true));

				child.AddProperty (Property.NewKeyword ("fixme:relativeuri", a_entry.Name));
				child.AddProperty (Property.New ("fixme:comment", a_entry.Comment));

				foreach (Property prop in Property.StandardFileProperties (Path.GetFileName (a_entry.Name), false))
					child.AddProperty (prop);

				AddChildIndexable (child);
			}

			if (total_size > MAX_ALL_FILES)
				Log.Debug ("Archive {0} crossed our max uncompressed size threshold.  Only {1} files extracted", this.file_info, count);
		}

		protected override void DoClose ()
		{
			// We don't close the archive stream, since it closes
			// the underlying stream.
			archive_stream = null;
			file_info = null;
		}

		internal class ArchiveEntry {
			public string Name;
			public string MimeType;
			public DateTime Modified;
			public string Comment;
			public long Size;

			public string TempFile;
		}

		private string StoreStreamInTempFile (Stream stream, string extension, DateTime mtime)
		{
			if (stream == null)
				return null;

			string filename = FileSystem.GetTempFileName (extension);
			FileStream file_stream = File.OpenWrite (filename);

			//Log.Debug ("Storing archive contents in {0}", filename);
			
			Mono.Unix.Native.Syscall.chmod (filename, (Mono.Unix.Native.FilePermissions) 384); // 384 is 0600
			
			BufferedStream buffered_stream = new BufferedStream (file_stream);

			byte [] buffer = new byte [8192];
			long prev_pos = -1;
			int read, total_bytes_read = 0;
			bool skip_file = false;
			int stuck_count = 0;

			do {
				try {
					read = stream.Read (buffer, 0, buffer.Length);
				} catch (Exception e) {
					Log.Error (e, "Caught exception extracting data from archive {0}", this.file_info);
					skip_file = true;
					break;
				}

				total_bytes_read += read;

				// Don't extract big files, to avoid filling up /tmp
				if (total_bytes_read > MAX_SINGLE_FILE) {
					Log.Debug ("10 meg threshold hit, skipping over {0}", this.file_info);
					skip_file = true;
					break;
				}

				if (read > 0)
					buffered_stream.Write (buffer, 0, read);

				// Lame workaround for some gzip files which loop
				// forever with SharpZipLib.  We have to check for
				// the parser getting stuck on a certain stream
				// position.
				if (stream is GZipInputStream && read == buffer.Length) {
					if (stream.Position == prev_pos) {
						stuck_count++;

						// 20 is a fairly arbitrary value
						if (stuck_count == 20) {
							Log.Debug ("{0} appears to be broken, skipping", this.file_info);
							skip_file = true;
							break;
						}
					} else {
						stuck_count = 0;
						prev_pos = stream.Position;
					}
				}
			} while (read > 0);

			buffered_stream.Close ();

			if (skip_file) {
				File.Delete (filename);
				return null;
			}

			File.SetLastWriteTimeUtc (filename, mtime);

			Mono.Unix.Native.Syscall.chmod (filename, (Mono.Unix.Native.FilePermissions) 256); // 256 is 0400

			return filename;
		}

		private ArchiveEntry GetNextEntryZip ()
		{
			ZipInputStream zip_stream = (ZipInputStream) archive_stream;
			ZipEntry zip_entry;

			do {
				zip_entry = zip_stream.GetNextEntry ();
			} while (zip_entry != null && zip_entry.IsDirectory);

			// End of the entries.
			if (zip_entry == null)
				return null;

			ArchiveEntry entry = new ArchiveEntry ();
			entry.Name = zip_entry.Name;
			entry.Modified = zip_entry.DateTime; // FIXME: Not sure zip_entry.DateTime is UTC.
			entry.Comment = zip_entry.Comment;
			entry.Size = zip_entry.Size;

			// Only index smaller subfiles, to avoid filling /tmp
			if (entry.Size > MAX_SINGLE_FILE) {
				Log.Debug ("Skipping over large file {0} in {1}", entry.Name, this.file_info);
				return entry;
			}

			entry.TempFile = StoreStreamInTempFile (archive_stream, Path.GetExtension (entry.Name), entry.Modified);
			if (entry.TempFile != null)
				entry.MimeType = XdgMime.GetMimeType (entry.TempFile);

			return entry;
		}

		private ArchiveEntry GetNextEntryTar ()
		{
			TarInputStream tar_stream = (TarInputStream) archive_stream;
			TarEntry tar_entry;

			do {
				tar_entry = tar_stream.GetNextEntry ();
			} while (tar_entry != null && tar_entry.IsDirectory);

			// End of the entries;
			if (tar_entry == null)
				return null;

			ArchiveEntry entry = new ArchiveEntry ();
			entry.Name = tar_entry.Name;
			entry.Modified = tar_entry.ModTime;
			entry.Size = tar_entry.Size;

			// Only index smaller subfiles, to avoid filling /tmp
			if (entry.Size > MAX_SINGLE_FILE) {
				Log.Debug ("Skipping over large file {0} in {1}", entry.Name, this.file_info);
				return entry;
			}

			entry.TempFile = StoreStreamInTempFile (archive_stream, Path.GetExtension (entry.Name), entry.Modified);
			if (entry.TempFile != null)
				entry.MimeType = XdgMime.GetMimeType (entry.TempFile);

			return entry;
		}

		private bool handled_single = false;

		private ArchiveEntry GetNextEntrySingle ()
		{
			if (handled_single)
				return null;

			ArchiveEntry entry = new ArchiveEntry ();
			entry.Name = Path.GetFileNameWithoutExtension (this.file_info.Name);
			entry.Modified = this.file_info.LastWriteTimeUtc;

			entry.TempFile = StoreStreamInTempFile (archive_stream, Path.GetExtension (entry.Name), entry.Modified);

			if (entry.TempFile != null) {
				entry.Size = new FileInfo (entry.TempFile).Length;
				entry.MimeType = XdgMime.GetMimeType (entry.TempFile);
			}

			handled_single = true;

			return entry;
		}
	}	
}
