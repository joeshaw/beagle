//
// Galago.cs
//
// Copyright (C) 2005 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//
using System;
using Galago;

// FIXME: Idea - the icons should be dimmed if idle

namespace Beagle.Util {

	public class GalagoTools {

		private GalagoTools () {} // this class is static

		private static bool Connect ()
		{
			return true;
		}

		public static string GetPresence (string service_id, string username)
		{
			if (! Galago.Core.Init ("beagle-galago-presence"))
				return null;

			Galago.Service service = Galago.Core.GetService (service_id, false, true);

			if (service == null)
				return null;

			Galago.Account account = service.GetAccount (username, true);

			if (account == null)
				return null;

			Galago.Person person = account.Person;
			Galago.Presence presence = account.Presence;

			if (presence == null)
				return null;

			string user_status = null;

			if (presence.Idle == true) 
				user_status = String.Format ("Idle {0}",
							     StringFu.DurationToPrettyString (DateTime.Now.AddSeconds (presence.IdleTime), DateTime.Now));
			else
				user_status = presence.ActiveStatus.Name;

			Galago.Core.Uninit();

			return user_status;
		}
	}
}
