//
// HitRegulator.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Collections;

using Beagle.Util;

namespace Beagle.Daemon {

	public class HitRegulator : IQueryResult {

		const int max_n_hits = 100;

		Queryable queryable;

		ArrayList hit_array = new ArrayList ();
		Hashtable by_uri = new Hashtable ();

		const double epsilon = 1e-8;
		double cutoff_score = epsilon;

		public Hashtable added_hits = new Hashtable ();
		public Hashtable subtracted_uris = new Hashtable ();

		public HitRegulator (Queryable queryable)
		{
			this.queryable = queryable;
		}

		public bool WillReject (double score)
		{
			return score < cutoff_score;
		}

		public bool Add (Hit hit)
		{
			if (WillReject (hit.Score))
				return false;

			int i = hit_array.BinarySearch (hit);
			if (i < 0)
				i = ~i;
			hit_array.Insert (i, hit);

			if (hit_array.Count > max_n_hits) {
				Hit low_hit;
				for (int j = max_n_hits; j < hit_array.Count; ++j) {
					low_hit = hit_array [j] as Hit;
					by_uri.Remove (low_hit.Uri.ToString ());
					added_hits.Remove (low_hit.Uri);
				}
				hit_array.RemoveRange (max_n_hits, hit_array.Count - max_n_hits);
				
				low_hit = hit_array [hit_array.Count - 1] as Hit;
				cutoff_score = low_hit.Score;
				//Logger.Log.Debug ("cutoff score is {0:0.00000}", cutoff_score);
			}
			
			by_uri [hit.Uri.ToString ()] = hit;
			added_hits [hit.Uri.ToString ()] = hit;
			
			return true;
		}

		public void Subtract (Uri uri)
		{
			Hit hit = by_uri [uri.ToString ()] as Hit;
			if (hit != null) {
				int i = hit_array.BinarySearch (hit);
				if (i >= 0) {
					hit_array.RemoveAt (i);
					added_hits.Remove (uri.ToString ());
					subtracted_uris [uri.ToString ()] = uri;
				}
			}
		}

		public void Flush (QueryResult result)
		{
			ICollection added, subtracted;
			lock (this) {
				added = added_hits.Values;
				subtracted = subtracted_uris.Values;
				added_hits = new Hashtable ();
				subtracted_uris = new Hashtable ();
			}

			foreach (Hit hit in added)
				hit.SourceObject = queryable;

			result.Subtract (subtracted);
			result.Add (added);
		}
	}
}
