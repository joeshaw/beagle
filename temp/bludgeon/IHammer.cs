using System;

namespace Bludgeon {

	public interface IHammer {

		// Return true if we actually did something,
		// false if the hammering ended up being a no-op for some reason.
		bool HammerOnce (DirectoryObject dir, EventTracker tracker);

	}

}
