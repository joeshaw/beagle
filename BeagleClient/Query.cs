//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using DBus;

namespace Beagle
{

	public abstract class Query : QueryProxy, IDisposable {

		public delegate void HitAddedHandler (Query source, Hit hit);
		public virtual event HitAddedHandler HitAddedEvent;

		public delegate void HitSubtractedHandler (Query source, Uri uri);
		public virtual event HitSubtractedHandler HitSubtractedEvent;

		private bool cancelled = false;

		private class HitInfo {
			public Hit Hit = null;
			public int RefCount = 0;
		}
		private Hashtable allHits = new Hashtable ();

		public Query ()
		{
			HitsAddedAsXmlEvent += OnHitsAddedAsXml;
			HitsSubtractedAsStringEvent += OnHitsSubtractedAsString;
			CancelledEvent += OnCancelled;
		}

		public bool IsCancelled {
			get { return cancelled; }
		}

		public void Dispose ()
		{
			CloseQuery ();
			
			GC.SuppressFinalize (this);
		}

		~Query ()
		{
			CloseQuery ();
		}

		private void InternalSubtractUri (Uri uri)
		{
			// Only subtract previously-added Uris.
			if (allHits.Contains (uri)) {
				HitInfo info = (HitInfo) allHits [uri];
				--info.RefCount;
				if (info.RefCount == 0) {
					allHits.Remove (uri);
					if (HitSubtractedEvent != null)
						HitSubtractedEvent (this, uri);
				}
			}
		}

		private void InternalAddHit (Hit hit)
		{
			HitInfo info = (HitInfo) allHits [hit.Uri];
			if (info == null) {
				info = new HitInfo ();
				info.Hit = hit;
				info.RefCount = 0;
				allHits [hit.Uri] = info;
			}

			++info.RefCount;
			// If necessary, synthesize a subtracted event
			if (info.RefCount > 1)
				if (HitSubtractedEvent != null)
					HitSubtractedEvent (this, hit.Uri);
			if (info.RefCount > 0)
				if (HitAddedEvent != null)
					HitAddedEvent (this, hit);
		}

		private void OnHitsAddedAsXml (QueryProxy sender, string hitsXml)
		{
			ArrayList hits = Hit.ReadHitXml (hitsXml);
			
			foreach (Hit hit in hits)
				InternalAddHit (hit);
		}

		private void OnHitsSubtractedAsString (QueryProxy sender, string uriList)
		{
			string[] uris = uriList.Split ('|');
			foreach (string uriStr in uris) {
				Uri uri = new Uri (uriStr, true);
				InternalSubtractUri (uri);
			}
		}

		private void OnCancelled (QueryProxy sender)
		{
			cancelled = true;
		}
	}
}
