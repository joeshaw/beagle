//
// QueryDriver.cs
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
using System.Reflection;
using System.Threading;

namespace Beagle.Daemon {
	
	public class QueryDriver {

		struct QueryableInfo {
			public QueryableFlavor Flavor;
			public Type Type;
		}

		static ArrayList queryableInfo = new ArrayList ();

		static bool ThisApiSoVeryIsBroken (Type m, object criteria)
		{
			return m == (Type) criteria;
		}

		static bool TypeImplementsInterface (Type t, Type iface)
		{
			Type[] impls = t.FindInterfaces (new TypeFilter (ThisApiSoVeryIsBroken),
							 iface);
			return impls.Length > 0;
		}

		static void ScanAssembly (Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes ()) {
				if (TypeImplementsInterface (type, typeof (IQueryable))) {
					foreach (object obj in Attribute.GetCustomAttributes (type)) {
						if (obj is QueryableFlavor) {
							QueryableFlavor flavor = (QueryableFlavor) obj;
							QueryableInfo info = new QueryableInfo ();
							info.Flavor = flavor;
							info.Type = type;
							queryableInfo.Add (info);
							break;
						}
					}
				}
			}
							
							
		}

		static bool initialized = false;

		static void Initialize ()
		{
			lock (queryableInfo) {
				if (initialized)
					return;
				ScanAssembly (Assembly.GetCallingAssembly ());
				initialized = true;
			}
		}

		private ArrayList queryables = new ArrayList ();

		public QueryDriver ()
		{
			Initialize ();
			foreach (QueryableInfo qi in queryableInfo) {

				IQueryable queryable;
				queryable = (IQueryable) Activator.CreateInstance (qi.Type);
				queryables.Add (queryable);
			}
		}

		class QueryClosure {

			IQueryable queryable;
			QueryBody body;
			
			public QueryClosure (IQueryable  _queryable,
					     QueryBody   _body)
			{
				queryable = _queryable;
				body      = _body;
			}

			public void Worker (QueryResult result)
			{
				queryable.DoQuery (body, result);
			}
		}

		public void DoQuery (QueryBody body, QueryResult result)
		{
			// The extra pair of calls to WorkerStart/WorkerFinished ensures that
			// the QueryResult will fire the StartedEvent and FinishedEvent,
			// even if no queryable accepts the query.
			result.WorkerStart ();

			if (! body.IsEmpty) {
				foreach (IQueryable queryable in queryables) {
					if (queryable.AcceptQuery (body)) {
						QueryClosure qc = new QueryClosure (queryable, body);
						result.AttachWorker (new QueryResult.QueryWorker (qc.Worker));
					}
				}
			}
			
			result.WorkerFinished ();
		}
	}
}
