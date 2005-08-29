
using System;
using System.IO;

namespace Bludgeon {

	public abstract class Move_Tour : IHammer {

		abstract protected FileModel PickTarget ();

		public void HammerOnce ()
		{
			FileModel tourist;
			tourist = PickTarget ();

			Log.Spew ("Touring {0} {1}",
				  tourist.IsFile ? "file" : "directory",
				  tourist);

			int count = 10;
			while (count > 0) {

				FileModel new_parent;
				new_parent = FileModel.PickAnyDirectory ();
				
				string new_name;
				new_name = Token.GetRandom ();

				if (tourist.MoveTo (new_parent, new_name)) {
					Log.Spew ("{0}: Toured to {1}", count, tourist.FullName);
					--count;
				}
			}
		}
	}

	[Hammer (Name="Move:TourFile")]
	public class Move_TourFile : Move_Tour {
		
		override protected FileModel PickTarget ()
		{
			return FileModel.PickFile ();
		}
	}

	[Hammer (Name="Move:TourDirectory")]
	public class Move_TourDirectory : Move_Tour {
		
		override protected FileModel PickTarget ()
		{
			return FileModel.PickNonRootDirectory ();
		}
	}

	/////////////////////////////////////////////////////////////////////

	public abstract class Move_ShortLived : IHammer {

		abstract protected FileModel Create ();

		public void HammerOnce ()
		{
			FileModel short_lived;
			short_lived = Create ();
			Log.Spew ("Created {0} {1}", short_lived.IsFile ? "file" : "directory", short_lived.FullName);

			int count = 10;
			// tour the file
			while (count > 0) {
				FileModel new_parent;
				new_parent = FileModel.PickAnyDirectory ();

				string new_name;
				new_name = Token.GetRandom ();

				if (short_lived.MoveTo (new_parent, new_name)) {
					Log.Spew ("Moved to {0}", short_lived.FullName);
					--count;
				}
			}

			short_lived.Delete ();
			Log.Spew ("Deleted {0}", short_lived.FullName);
		}
	}

	[Hammer (Name="Move:ShortLivedFile")]
	public class Move_ShortLivedFile : Move_ShortLived {

		override protected FileModel Create ()
		{
			FileModel parent, child;
			do {
				parent = FileModel.PickAnyDirectory ();
				child = parent.NewFile ();
			} while (child == null);
			return child;
		}
	}

	[Hammer (Name="Move:ShortLivedDirectory")]
	public class Move_ShortLivedDirectory : Move_ShortLived {

		override protected FileModel Create ()
		{
			FileModel parent, child;
			do {
				parent = FileModel.PickAnyDirectory ();
				child = parent.NewDirectory ();
			} while (child == null);
			return child;
		}
	}


}
