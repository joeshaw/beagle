using System;
using System.Collections;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Tar;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	// FIXME: When we untar (using the command-line 'tar') one of the
	// files created by this class, it always complains about 'A lone zero
	// block at xxx'.  Are we doing something wrong, or is SharpZipLib the
	// culprit?

	public class TarFileObject : FileObject {

		public TarFileObject (DirectoryObject tar_root)
		{
			AllowChildren ();
			
			ArrayList to_be_tarred;
			to_be_tarred = new ArrayList (tar_root.Children);

			foreach (FileSystemObject fso in to_be_tarred) {
				tar_root.RemoveChild (fso, null);
				this.AddChild (fso, null);
			}
		}

		override protected string GetChildUri (FileSystemObject children)
		{
			// FIXME: What is the uri scheme for tar files?
			return null;
		}

		override public string MimeType {
			get { return "application/x-tar"; }
		}

		override public string Extension {
			get { return ".tar"; }
		}

		private void WriteObjectToTar (TarOutputStream  tar_out,
					       FileSystemObject fso,
					       EventTracker     tracker)
		{
			MemoryStream memory = null;

			TarHeader header;
			header = new TarHeader ();

			StringBuilder name_builder;
			name_builder = new StringBuilder (fso.FullName);
			name_builder.Remove (0, this.FullName.Length+1);
			header.name = name_builder;

			header.modTime = fso.Timestamp;
			if (fso is DirectoryObject) {
				header.mode = 511; // 0777
				header.typeFlag = TarHeader.LF_DIR;
				header.size = 0;
			} else {
				header.mode = 438; // 0666
				header.typeFlag = TarHeader.LF_NORMAL;
				memory = new MemoryStream ();
				((FileObject) fso).AddToStream (memory, tracker);
				header.size = memory.Length;
			}

			TarEntry entry;
			entry = new TarEntry (header);

			tar_out.PutNextEntry (entry);
			if (memory != null) {
				tar_out.Write (memory.ToArray (), 0, (int) memory.Length);
				memory.Close ();
			}
			tar_out.CloseEntry ();

			// If this is a directory, write out the children
			if (fso is DirectoryObject)
				foreach (FileSystemObject child in fso.Children)
					WriteObjectToTar (tar_out, child, tracker);
		}

		override public void AddToStream (Stream stream, EventTracker tracker)
		{
			if (tracker != null)
				tracker.ExpectingAdded (this.Uri);

			UnclosableStream unclosable;
			unclosable = new UnclosableStream (stream);

			TarOutputStream tar_out;
			tar_out = new TarOutputStream (unclosable);
			foreach (FileSystemObject fso in Children)
				WriteObjectToTar (tar_out, fso, tracker);
			
			// This calls close on the underlying stream,
			// which is why we wrapped the stream in an
			// UnclosableStream.
			tar_out.Close ();
		}

		override protected bool MatchesQueryPart (QueryPart part)
		{
			// The only thing that could match is the name, and we check
			// for that already in FileSystemObject.MatchesQuery.
			return false;
		}
	}
}
