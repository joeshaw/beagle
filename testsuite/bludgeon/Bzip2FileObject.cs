
using System;
using System.Collections;
using System.IO;
using System.Text;

using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.BZip2;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class Bzip2FileObject : FileObject {

		public Bzip2FileObject (FileObject bzip2ed_file)
		{
			AllowChildren ();
			AddChild (bzip2ed_file, null);
		}

		override protected Uri GetChildUri (FileSystemObject children)
		{
			// FIXME: What is the uri scheme for bzip2 files?
			return new Uri (this.Uri.ToString () + "#" + children.Name);
		}

		override public string MimeType {
			get { return "application/x-bzip2"; }
		}

		override public string Extension {
			get { return ".bz2"; }
		}

		override public void AddToStream (Stream stream, EventTracker tracker)
		{
			if (ChildCount > 1)
				throw new Exception ("Bzip2 file " + Uri + " has " + ChildCount + " children");

			if (tracker != null)
				tracker.ExpectingAdded (UriFu.UriToEscapedString (this.Uri));

			UnclosableStream unclosable;
			unclosable = new UnclosableStream (stream);
			
			BZip2OutputStream bz2_out;
			bz2_out = new BZip2OutputStream (unclosable);
			
			MemoryStream memory;
			memory = new MemoryStream ();
			// There should just be one child
			foreach (FileObject file in Children)
				file.AddToStream (memory, tracker);
			bz2_out.Write (memory.ToArray (), 0, (int) memory.Length);
			memory.Close ();

			bz2_out.Close ();
		}
		
		override protected bool MatchesQueryPart (QueryPart part)
		{
			// The only thing that could match is the name, and we check
			// for that already in FileSystemObject.MatchesQuery.
			return false;
		}
	}
}
