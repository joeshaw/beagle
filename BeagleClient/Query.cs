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
			try {
				CloseQuery ();
			}
			catch (Exception e) { }
			
			GC.SuppressFinalize (this);
		}

		~Query ()
		{
			try {
				CloseQuery ();
			}
			catch (Exception e) { }
		}

		private void OnHitsAddedAsXml (QueryProxy sender, string hitsXml)
		{
			ArrayList hits = Hit.ReadHitXml (hitsXml);
			
			if (HitAddedEvent != null) {
				foreach (Hit hit in hits)
					HitAddedEvent (this, hit);
			}
		}

		private void OnHitsSubtractedAsString (QueryProxy sender, string uriList)
		{
			if (HitSubtractedEvent != null) {
				string[] uris = uriList.Split ('|');
				foreach (string uriStr in uris) {
					Uri uri = new Uri (uriStr, true);
					HitSubtractedEvent (this, uri);
				}
			}
		}

		private void OnCancelled (QueryProxy sender)
		{
			cancelled = true;
		}
	}
}
