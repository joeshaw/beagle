
using System;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	public class RemoteIndexerResponse : ResponseMessage {

		public int ItemCount = -1;

		public RemoteIndexerResponse ()
		{

		}
	}
}
