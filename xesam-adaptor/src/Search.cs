//
// Search.cs : Translation between BeagleClient API and Xesam(-like) API
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Beagle;

namespace Beagle {
	namespace Xesam {
		public class Hit {
			private int id;
			private Uri uri;
			private object[] hitValue;

			public int Id {
				get { return id; }
			}

			public object[] Value {
				get { return hitValue; }
			}

			public Uri Uri {
				get { return uri; }
			}

			public Hit(int id, Beagle.Hit hit, string[] fields)
			{
				this.id = id;
				hitValue = new object[fields.Length];
				int i = 0;

				uri = hit.Uri;

				foreach (string field in fields) {
					switch (field) {
						case "uri":
							hitValue[i++] = hit.Uri.ToString();
							break;
					}
				}
			}
		}

		public class Search {
			private Session parentSession;
			private Query query;
			private string id;
			private bool running;
			private int hitCount = 0;
			private Dictionary<int, Xesam.Hit> hits;
			private Dictionary<int, Xesam.Hit> newHits;

			public Mutex mutex;
			public event HitsAddedMethod HitsAddedHandler;
			public event HitsRemovedMethod HitsRemovedHandler;

			public Search(string myID, Session parentSession, string qTxt)
			{
				this.parentSession = parentSession;
				id = myID;
				running = false;
				hits = new Dictionary<int, Xesam.Hit>();
				newHits = new Dictionary<int, Xesam.Hit>();
				mutex = new Mutex();

				query = new Query();

				query.AddText(qTxt);

				query.HitsAddedEvent += OnHitsAdded;
				query.HitsSubtractedEvent += OnHitsSubtracted;
				query.FinishedEvent += OnFinished;
			}

			public void Start()
			{
				query.SendAsync();
				running = true;
			}

			public void Close()
			{
				// The if() might not be strictly necessary,
				// but just in case
				if (running) {
					query.Close();
					running = false;
				}
			}

			public object[][] GetHits(int num)
			{
				mutex.WaitOne();

				// XXX: TBD -- sorting
				List<int> returned = new List<int>();
				List<object[]> ret = new List<object[]>();
				int i = 1;

				foreach (KeyValuePair<int, Xesam.Hit> kvp in newHits) {
					ret.Add(kvp.Value.Value);
					returned.Add(kvp.Key);
					hits.Add(kvp.Key, kvp.Value);
					if (i++ == num)
						break;
				}

				foreach (int key in returned) {
					newHits.Remove(key);
				}

				Console.Error.WriteLine("GetHits(): returning {0} hits ({1} requested)", i-1, num);
				mutex.ReleaseMutex();

				return ret.ToArray();
			}

			private void OnHitsAdded(HitsAddedResponse response)
			{
				mutex.WaitOne();

				// cache the hits and keep them nice and safe
				Console.Error.WriteLine("{0}: Got some hits: {1}", id, response.NumMatches);
				foreach (Beagle.Hit bHit in response.Hits) {
					Console.Error.WriteLine("+Hit: {0}", bHit.Uri);
					newHits.Add(hitCount++, new Xesam.Hit(hitCount, bHit, parentSession.HitFields));
				}

				mutex.ReleaseMutex();
			}

			private void OnHitsSubtracted(HitsSubtractedResponse response)
			{
				mutex.WaitOne();

				List<int> removed = new List<int>();

				Console.Error.WriteLine("Removing some hits");
				foreach (KeyValuePair<int, Xesam.Hit> kvp in hits) {
					foreach (Uri uri in response.Uris) {
						if (kvp.Value.Uri == uri) {
							Console.Error.WriteLine("-Hit: {0}", uri);
							removed.Add(kvp.Key);
						}
					}
				}

				foreach (int key in removed) {
					hits.Remove(key);
				}

				HitsRemovedHandler(id, removed.ToArray());
				mutex.ReleaseMutex();
			}

			private void OnFinished(FinishedResponse response)
			{
				mutex.WaitOne();

				Console.Error.WriteLine("Search finished");
				if (newHits.Count > 0) {
					HitsAddedHandler(id, newHits.Count);
				}

				mutex.ReleaseMutex();
			}

		}
	}
}
