
using System;

namespace Bludgeon {

	[AttributeUsage (AttributeTargets.Class)]
	public class HammerAttribute : Attribute {
		public string Name = "Unnamed";
	}

}
