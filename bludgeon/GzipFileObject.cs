
using System;
using System.Collections;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.GZip;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class GzipFileObject : FileObject {

		public GzipFileObject (FileObject gzipped_file)
		{
			AllowChildren ();
			AddChild (gzipped_file, null);
		}

		override protected string GetChildUri (FileSystemObject children)
		{
			// FIXME: What is the uri scheme for gzip files?
			return null;
		}

		override public string MimeType {
			get { return "application/x-gzip"; }
		}

		override public string Extension {
			get { return ".gz"; }
		}

		override public void AddToStream (Stream stream, EventTracker tracker)
		{
			if (ChildCount > 1)
				throw new Exception ("Gzip file " + Uri + " has " + ChildCount + " children");

			if (tracker != null)
				tracker.ExpectingAdded (this.Uri);

			UnclosableStream unclosable;
			unclosable = new UnclosableStream (stream);
			
			GZipOutputStream gz_out;
			gz_out = new GZipOutputStream (unclosable);

			MemoryStream memory;
			memory = new MemoryStream ();
			// There should just be one child
			foreach (FileObject file in Children)
				file.AddToStream (memory, tracker);
			gz_out.Write (memory.ToArray (), 0, (int) memory.Length);
			memory.Close ();

			gz_out.Close ();
		}
		
		override protected bool MatchesQueryPart (QueryPart part)
		{
			// The only thing that could match is the name, and we check
			// for that already in FileSystemObject.MatchesQuery.
			return false;
		}
	}
}
