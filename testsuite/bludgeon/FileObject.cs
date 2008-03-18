
using System;
using System.Collections;
using System.IO;

using Mono.Unix.Native;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	abstract public class FileObject : FileSystemObject {

		public FileObject ()
		{
			timestamp = FileSystemObject.PickTimestamp ();
		}

		abstract public void AddToStream (Stream stream, EventTracker tracker);

		override public void AddOnDisk (EventTracker tracker)
		{
			FileStream stream;
			stream = new FileStream (FullName,
						 FileMode.Create,
						 FileAccess.Write,
						 FileShare.ReadWrite);
			AddToStream (stream, tracker);
			stream.Close ();

			// Stamp the right timestamp onto the file
			FileInfo info;
			info = new FileInfo (FullName);
			info.LastWriteTime = Timestamp;
		}

		override public void DeleteOnDisk (EventTracker tracker)
		{
			Syscall.unlink (FullName);
			// FIXME: adjust tracker
		}

		override public void MoveOnDisk (string old_full_name, EventTracker tracker)
		{
			Syscall.rename (old_full_name, FullName);
			// FIXME: adjust tracker
		}

		override public bool VerifyOnDisk ()
		{
			if (! File.Exists (FullName)) {
				Log.Failure ("Missing file '{0}'", FullName);
				return false;
			}
			return true;
		}
	}

}
