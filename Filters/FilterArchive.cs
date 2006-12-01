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

		internal delegate ArchiveEntry GetNextEntry ();

		private FileInfo file_info;
		private Stream archive_stream;
		private GetNextEntry get_next_entry;

		public FilterArchive ()
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

			switch (MimeType) {
			case "application/zip":
				archive_stream = new ZipInputStream (Stream);
				get_next_entry = GetNextEntryZip;
				break;

			case "application/x-bzip-compressed-tar":
				archive_stream = new TarInputStream (new BZip2InputStream (Stream));
				get_next_entry = GetNextEntryTar;
				break;

			case "application/x-compressed-tar":
			case "application/x-tgz":
				archive_stream = new TarInputStream (new GZipInputStream (Stream));
				get_next_entry = GetNextEntryTar;
				break;

			case "application/x-tar":
				archive_stream = new TarInputStream (Stream);
				get_next_entry = GetNextEntryTar;
				break;

			case "application/x-gzip":
				archive_stream = new GZipInputStream (Stream);
				get_next_entry = GetNextEntrySingle;
				break;

			case "application/x-bzip":
				archive_stream = new BZip2InputStream (Stream);
				get_next_entry = GetNextEntrySingle;
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

			while ((a_entry = this.get_next_entry ()) != null) {
				// FIXME: For nested archives, create uid:foo#bar
				// instead of uid:foo#xxx#bar (avoid duplicates ?)
				Indexable child = new Indexable (new Uri (Uri.ToString () + "#" + a_entry.Name, true));

				child.CacheContent = false;
				child.MimeType = a_entry.MimeType;

				child.DisplayUri = new Uri (DisplayUri.ToString () + "#" + a_entry.Name, true);
				child.ContentUri = UriFu.PathToFileUri (a_entry.TempFile);
				child.DeleteContent = true;

				child.AddProperty (Property.NewKeyword ("fixme:relativeuri", a_entry.Name));

				if (a_entry.Comment != null)
					child.AddProperty (Property.New ("fixme:comment", a_entry.Comment));

				foreach (Property prop in Property.StandardFileProperties (Path.GetFileName (a_entry.Name), false))
					child.AddProperty (prop);

				AddChildIndexable (child);
			}
		}

		internal class ArchiveEntry {
			public string Name;
			public string MimeType;
			public DateTime Modified;
			public string Comment;

			public string TempFile;
		}

		private static string StoreStreamInTempFile (Stream stream, DateTime mtime)
		{
			if (stream == null)
				return null;

			string filename = Path.GetTempFileName ();
			FileStream file_stream = File.OpenWrite (filename);
			
			Mono.Unix.Native.Syscall.chmod (filename, (Mono.Unix.Native.FilePermissions) 384); // 384 is 0600
			
			BufferedStream buffered_stream = new BufferedStream (file_stream);

			byte [] buffer = new byte [8192];
			int read;

			do {
				read = stream.Read (buffer, 0, buffer.Length);
				if (read > 0)
					buffered_stream.Write (buffer, 0, read);
			} while (read > 0);

			buffered_stream.Close ();

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

			entry.TempFile = StoreStreamInTempFile (archive_stream, entry.Modified);
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

			entry.TempFile = StoreStreamInTempFile (archive_stream, entry.Modified);
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

			entry.TempFile = StoreStreamInTempFile (archive_stream, entry.Modified);
			entry.MimeType = XdgMime.GetMimeType (entry.TempFile);

			handled_single = true;

			return entry;
		}
	}	
}
