
using System;
using System.IO;

namespace Bludgeon {

	[Hammer (Name="Basic:CreateFile")]
	public class Basic_CreateFile : IHammer {
		
		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			FileObject created;
			created = new TextFileObject (); // FIXME: or an archive

			DirectoryObject parent;
			parent = dir.PickDirectory ();
			parent.AddChild (created, tracker);
			
			Log.Info ("Created file '{0}'", created.ShortName);

			return true;
		}
	}

	[Hammer (Name="Basic:ClobberFile")]
	public class Basic_ClobberFile : IHammer {
		
		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			FileObject victim;

			do {
				victim = dir.PickChildFile ();
			} while (victim != null && victim.IsWritable);

			if (victim == null)
				return false;

			// Create an object w/ the right type
			FileObject created;
			created = TreeBuilder.NewFile (5, 10, victim.Extension, 0.1, 0.5, null); // FIXME: stupid magic numbers

			Log.Info ("Clobbered file '{0}'", victim.ShortName);
			victim.Parent.ClobberingAddChild (created, victim, tracker);

			return true;
		}
	}

	[Hammer (Name="Basic:CreateDirectory")]
	public class Basic_CreateDirectory : IHammer {
		
		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			DirectoryObject created;
			created = new DirectoryObject ();

			DirectoryObject parent;
			parent = dir.PickDirectory ();
			parent.AddChild (created, tracker);
			
			Log.Info ("Created directory {0}", created.ShortName);

			return true;
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	[Hammer (Name="Basic:DeleteFile")]
	public class Basic_DeleteFile : IHammer {
		
		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			FileObject target;
			target = dir.PickChildFile ();
			if (target == null)
				return false;

			Log.Info ("Deleted file '{0}'", target.ShortName);			
			target.Parent.RemoveChild (target, tracker);
			
			return true;
		}
	}

	[Hammer (Name="Basic:DeleteDirectory")]
	public class Basic_DeleteDirectory : IHammer {
		
		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			DirectoryObject target;
			target = dir.PickChildDirectory ();
			if (target == null)
				return false;
		
			Log.Info ("Deleted directory '{0}'", target.ShortName);
			target.Parent.RemoveChild (target, tracker);

			return true;
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	public abstract class Basic_Move : IHammer {
		
		abstract protected FileSystemObject PickTarget (DirectoryObject dir);

		public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
		{
			DirectoryObject new_parent;
			new_parent = dir.PickDirectory ();

			// 10 is a stupid magic number here.
			FileSystemObject target = null;
			for (int i = 0; i < 10 && target == null; ++i) {
				target = PickTarget (dir);
				if (target == null)
					return false;
				if (target.IsAncestorOf (new_parent))
					target = null;
			}
			if (target == null)
				return false;

			Log.Spew ("Moved {0} to {1}", target.ShortName, new_parent.ShortName);
			target.Parent.MoveChild (target, new_parent, tracker);
			return true;
		}
	}

	[Hammer (Name="Basic:MoveFile")]
	public class Basic_MoveFile : Basic_Move {

		override protected FileSystemObject PickTarget (DirectoryObject dir)
		{
			return dir.PickChildFile ();
		}
	}

	[Hammer (Name="Basic:MoveDirectory")]
	public class Basic_MoveDirectory : Basic_Move {

		override protected FileSystemObject PickTarget (DirectoryObject dir)
		{
			return dir.PickChildDirectory ();
		}
	}
}
