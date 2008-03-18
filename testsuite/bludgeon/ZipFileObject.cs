using System;
using System.Collections;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class ZipFileObject : FileObject {

		public ZipFileObject (DirectoryObject zip_root)
		{
			AllowChildren ();
			
			ArrayList to_be_zipped;
			to_be_zipped = new ArrayList (zip_root.Children);

			foreach (FileSystemObject fso in to_be_zipped) {
				zip_root.RemoveChild (fso, null);
				this.AddChild (fso, null);
			}
		}

		override protected Uri GetChildUri (FileSystemObject children)
		{
			// FIXME: What is the uri scheme for zip files?
			Uri uri = new Uri (this.Uri.ToString () + "#" + children.Name);
			return uri;
		}

		override public string MimeType {
			get { return "application/x-zip"; }
		}

		override public string Extension {
			get { return ".zip"; }
		}

		private void WriteObjectToZip (ZipOutputStream  zip_out,
					       FileSystemObject fso,
					       EventTracker     tracker)
		{

			MemoryStream memory = null;

			string name;
			name = fso.FullName.Substring (this.FullName.Length + 1);
			if (fso is DirectoryObject)
				name += "/";

			ZipEntry entry;
			entry = new ZipEntry (name);
			entry.DateTime = fso.Timestamp;
			
			if (fso is DirectoryObject)
				entry.Size = 0;
			else {
				memory = new MemoryStream ();
				((FileObject) fso).AddToStream (memory, tracker);
				entry.Size = memory.Length;
			}

			zip_out.PutNextEntry (entry);
			if (memory != null) {
				zip_out.Write (memory.ToArray (), 0, (int) memory.Length);
				memory.Close ();
			}

			// If this is a directory, write out the children
			if (fso is DirectoryObject)
				foreach (FileSystemObject child in fso.Children)
					WriteObjectToZip (zip_out, child, tracker);
		}

		override public void AddToStream (Stream stream, EventTracker tracker)
		{
			if (tracker != null)
				tracker.ExpectingAdded (UriFu.UriToEscapedString (this.Uri));

			UnclosableStream unclosable;
			unclosable = new UnclosableStream (stream);

			ZipOutputStream zip_out;
			zip_out = new ZipOutputStream (unclosable);

			foreach (FileSystemObject fso in Children)
				WriteObjectToZip (zip_out, fso, tracker);
			zip_out.Finish ();
			zip_out.Close ();
		}

		override protected bool MatchesQueryPart (QueryPart part)
		{
			// The only thing that could match is the name, and we check
			// for that already in FileSystemObject.MatchesQuery.
			return false;
		}
	}
}
