//
// ExternalAccessFilter.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Authors:
//   Vijay K. Nanjundaswamy (knvijay@novell.com)
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

using Beagle;
using Beagle.Util;

namespace Beagle.WebService {

	public class ExternalAccessFilter 
	{
		ArrayList matchers;

		static readonly string configuration = "shares.cfg";

		public ExternalAccessFilter ()
		{
			matchers = new ArrayList();

			if (!File.Exists (Path.Combine (PathFinder.StorageDir, configuration)))
                                return;

            StreamReader reader = new StreamReader(
                         Path.Combine (PathFinder.StorageDir, configuration));

            string entry;

            while ( ((entry = reader.ReadLine ()) != null) && (entry.Trim().Length > 1)) {
            		
            	if ((entry[0] != '#') && (entry.IndexOf(';') > 0)) {
                	string[] data = entry.Split (';');
					SimpleMatcher matcher = new SimpleMatcher ();
					matcher.Match = data[0];
					matcher.Rewrite = data[1];
					matchers.Add (matcher);
				}
            }
		}
		
		public ICollection FilterHit (Hit hit)
		{
			ArrayList hits = new ArrayList ();
		
			if (matchers.Count == 0)
				hits.Add(hit);
			else
			foreach (SimpleMatcher matcher in matchers)
			{
				if (hit.Uri.ToString ().IndexOf (matcher.Match) == -1)
					continue;

				hits.Add (hit);
			}

			return hits;		
		}

		public ICollection TranslateHit (Hit hit)
		{
			ArrayList hits = new ArrayList ();
		
			if (matchers.Count == 0)
				hits.Add(hit);
			else
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
