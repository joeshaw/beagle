//
// Queryable.cs
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

using Beagle.Util;
using Beagle;

namespace Beagle.Daemon {

	public class Queryable {

		private QueryableFlavor flavor;
		private IQueryable iqueryable;

		public Queryable (QueryableFlavor _flavor,
				  IQueryable _iqueryable)
		{
			flavor = _flavor;
			iqueryable = _iqueryable;
		}
		
		public void Start ()
		{
			iqueryable.Start ();
		}

		public string Name {
			get { return flavor.Name; }
		}

		public QueryDomain Domain {
			get { return flavor.Domain; }
		}
			
		public bool AcceptQuery (QueryBody body)
		{
			return body != null
				&& ! body.IsEmpty
				&& body.AllowsSource (Name)
				&& body.AllowsDomain (Domain)
				&& iqueryable.AcceptQuery (body);
		}

		public int GetItemCount ()
		{
			int n = -1;
			try {
				n = iqueryable.GetItemCount ();
			} catch (Exception ex) {
				Logger.Log.Warn ("Couldn't get item count for '{0}'", Name);
				Logger.Log.Warn (ex);
			}
			return n;
		}

		public void DoQuery (QueryBody body, IQueryResult result, IQueryableChangeData change_data)
		{
			try {
				iqueryable.DoQuery (body, result, change_data);
			} catch (Exception ex) {
				Logger.Log.Warn ("Caught exception calling DoQuery on '{0}'", Name);
				Logger.Log.Warn (ex);
			}
		}
	}
}
