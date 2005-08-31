
using System;
using System.IO;

namespace Bludgeon {

	[Hammer (Name="Basic:CreateFile")]
	public class Basic_CreateFile : IHammer {
		
		public void HammerOnce ()
		{
			FileModel created;

			do {
				FileModel parent;
				parent = FileModel.PickAnyDirectory ();
				created = parent.NewFile ();
			} while (created == null);

			Log.Info ("Created file {0}", created.ShortName);
		}
	}

	[Hammer (Name="Basic:CreateDirectory")]
	public class Basic_CreateDirectory : IHammer {
		
		public void HammerOnce ()
		{
			FileModel created;

			do {
				FileModel parent;
				parent = FileModel.PickAnyDirectory ();
				created = parent.NewDirectory ();
			} while (created == null);

			Log.Info ("Created directory {0}", created.ShortName);
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	[Hammer (Name="Basic:DeleteFile")]
	public class Basic_DeleteFile : IHammer {
		
		public void HammerOnce ()
		{
			FileModel target;
			target = FileModel.PickFile ();
			
			if (target != null) {
				Log.Info ("Deleted file {0}", target.ShortName);
				target.Delete ();
			}
		}
	}

	[Hammer (Name="Basic:DeleteDirectory")]
	public class Basic_DeleteDirectory : IHammer {
		
		public void HammerOnce ()
		{
			FileModel target;
			target = FileModel.PickNonRootDirectory ();
		
			if (target != null) {
				Log.Info ("Deleted directory {0}", target.ShortName);
				target.Delete ();
			}
		}
	}

	///////////////////////////////////////////////////////////////////////////////

	public abstract class Basic_Move : IHammer {
		
		abstract protected FileModel PickTarget ();

		public void HammerOnce ()
		{
			string old_path, new_path;

			FileModel target;

			while (true) {

				target = PickTarget ();
				if (target == null)
					return;

				FileModel new_parent;
				new_parent = FileModel.PickAnyDirectory ();

				string new_name;
				new_name = Token.GetRandom ();
				
				old_path = target.ShortName;
				if (target.MoveTo (new_parent, new_name)) {
					new_path = target.ShortName;
					break;
				}
			}

			Log.Spew ("Moved {0} {1} to {2}", target.Type, old_path, new_path);
		}
	}

	[Hammer (Name="Basic:MoveFile")]
	public class Basic_MoveFile : Basic_Move {

		override protected FileModel PickTarget ()
		{
			return FileModel.PickFile ();
		}
	}

	[Hammer (Name="Basic:MoveDirectory")]
	public class Basic_MoveDirectory : Basic_Move {

		override protected FileModel PickTarget ()
		{
			return FileModel.PickNonRootDirectory ();
		}
	}


}
