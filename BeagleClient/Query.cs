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

namespace Beagle
{
	using System.Collections;
	using DBus;
	using System;

	public abstract class Query : QueryProxy, IDisposable {
		public delegate void GotHitsHandler (ICollection hits);

		public virtual event GotHitsHandler GotHitsEvent;

		public void Dispose () {
			CloseQuery ();
			
			GC.SuppressFinalize (this);
		}

		~Query () {
			CloseQuery ();
		}

		private void OnGotHitsXml (string hitsXml)
		{
			if (GotHitsEvent != null) {
				ArrayList hits = Hit.ReadHitXml (hitsXml);
				GotHitsEvent (hits);
			}
		}

		static public Query New ()
		{
			string queryPath = DBusisms.QueryManager.NewQuery ();

			Query query;
			query = (Query) DBusisms.Service.GetObject (typeof (Query), queryPath);
			query.GotHitsXmlEvent += query.OnGotHitsXml;
			
			return query;
		}

	}

}
