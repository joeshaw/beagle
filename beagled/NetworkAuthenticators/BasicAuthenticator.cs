//
// BasicAuthenticator.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//   Fredrik Hedberg (fredrik.hedberg@avafan.com)
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections;

namespace Beagle.Daemon
{
	public class BasicAuthenticator : IHitAuthenticator
	{
		ArrayList matchers;

		static readonly string configuration = "shares.cfg";

		public BasicAuthenticator ()
		{
			matchers = new ArrayList();

			LoadConfiguration ();
		}
		
		void LoadConfiguration ()
		{
			if (!File.Exists (Path.Combine (PathFinder.StorageDir, configuration)))
                                return;

                        StreamReader reader = new StreamReader(
                                Path.Combine (PathFinder.StorageDir, configuration));

                        string line = "";

                        while ( (line = reader.ReadLine ()) != null) {
                                string[] data = line.Split (';');
				SimpleMatcher matcher = new SimpleMatcher ();
				matcher.Match = data[0];
				matcher.Rewrite = data[1];
				matchers.Add (matcher);
                        }
		}
		
		public ICollection Authenticate (Hit hit, QueryBody query)
		{
			ArrayList hits = new ArrayList ();

			foreach (SimpleMatcher matcher in matchers)
			{
				if (hit.Uri.ToString ().IndexOf (matcher.Match) == -1)
					continue;

				hit.Uri = new Uri (hit.Uri.ToString ().Replace (matcher.Match,matcher.Rewrite));
				hits.Add (hit);
			}

			return hits;
		}
	
		internal struct SimpleMatcher
		{
			public string Match;
			public string Rewrite;
		}
	}	
}
