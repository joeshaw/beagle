using System;

namespace Bludgeon {

	public interface IHammer {

		// Return false if there is a fatal error
		// and the test should stop.
		// Returns true otherwise.
		void HammerOnce ();

	}

}
