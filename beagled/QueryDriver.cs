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
using Beagle.Util;

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

		////////////////////////////////////////////////////////

		public delegate void ChangedHandler (QueryDriver          source,
						     IQueryable           queryable,
						     IQueryableChangeData changeData);

		public event ChangedHandler ChangedEvent;


		private ArrayList queryables = new ArrayList ();

		public QueryDriver ()
		{
			Initialize ();
			foreach (QueryableInfo qi in queryableInfo) {

				IQueryable queryable;

				try {
					queryable = (IQueryable) Activator.CreateInstance (qi.Type);
				} catch (Exception e) {
					Exception ex = e.InnerException != null ? e.InnerException : e;

					Logger.Log.Error ("Exception trying to activate {0} backend:\n{1}", qi.Type, ex);
					continue;
				}
				
				queryables.Add (queryable);
				queryable.ChangedEvent += OnQueryableChanged;
			}
		}

		class QueryClosure {

			IQueryable queryable;
			QueryBody body;
			IQueryableChangeData changeData;
			
			public QueryClosure (IQueryable  _queryable,
					     QueryBody   _body)
			{
				queryable  = _queryable;
				body       = _body;
				changeData = null;
			}

			public QueryClosure (IQueryable           _queryable,
					     QueryBody            _body,
					     IQueryableChangeData _changeData)
			{
				queryable  = _queryable;
				body       = _body;
				changeData = _changeData;
			}

			public void Worker (QueryResult result)
			{
				queryable.DoQuery (body, result, changeData);
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

					if (! body.AllowsSource (queryable.Name))
						continue;
					    
					if (queryable.AcceptQuery (body)) {
						QueryClosure qc;
						qc = new QueryClosure (queryable, body);
						result.AttachWorker (new QueryResult.QueryWorker (qc.Worker));
					}
				}
			}
			
			result.WorkerFinished ();
		}

		public void DoQueryChange (IQueryable queryable, IQueryableChangeData changeData,
					   QueryBody body, QueryResult result)
		{
			if (result != null
			    && ! body.IsEmpty 
			    && queryable.AcceptQuery (body)) {
				QueryClosure qc;
				qc = new QueryClosure (queryable, body, changeData);
				result.AttachWorker (new QueryResult.QueryWorker (qc.Worker));
			}
		}

		private void OnQueryableChanged (IQueryable           source,
						 IQueryableChangeData changeData)
		{
			if (ChangedEvent != null)
				ChangedEvent (this, source, changeData);
		}
	}
}
