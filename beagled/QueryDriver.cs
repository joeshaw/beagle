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
using System.Text;
using System.Threading;
using Beagle.Util;

namespace Beagle.Daemon {
	
	public class QueryDriver {

		static ArrayList allowed_queryables = new ArrayList ();
		static ArrayList denied_queryables = new ArrayList ();
		
		static public void Allow (string name)
		{
			allowed_queryables.Add (name.ToLower ());
		}
		
		static public void Deny (string name)
		{
			denied_queryables.Add (name.ToLower ());
		}

		static private bool UseQueryable (string name)
		{
			name = name.ToLower ();

			if (allowed_queryables.Count > 0) {
				foreach (string allowed in allowed_queryables) {
					if (name == allowed) 
						return true;
				}
				return false;
			}

			foreach (string denied in denied_queryables) {
				if (name == denied)
					return false;
			}
			return true;

		}

		static ArrayList queryables = new ArrayList ();

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

		// For every type in the assembly that
		// (1) implements IQueryable
		// (2) Has a QueryableFlavor attribute attached
		// assemble a Queryable object and stick it into our list of queryables.
		static void ScanAssembly (Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes ()) {
				if (TypeImplementsInterface (type, typeof (IQueryable))) {
					foreach (object obj in Attribute.GetCustomAttributes (type)) {
						QueryableFlavor flavor = obj as QueryableFlavor;
						if (flavor == null)
							continue;
						
						if (! UseQueryable (flavor.Name))
							continue;
						
						IQueryable iq = null;
						try {
							iq = Activator.CreateInstance (type) as IQueryable;
						} catch (Exception e) {
							Logger.Log.Error ("Caught exception while activiting {0} backend", flavor.Name);
							Logger.Log.Error (e);
						}
						if (iq == null)
							continue;

						Queryable q = new Queryable (flavor, iq);
						queryables.Add (q);
					}
				}
			}
		}

		static bool initialized = false;

		static void Initialize ()
		{
			lock (queryables) {
				if (initialized)
					return;
				ScanAssembly (Assembly.GetCallingAssembly ());
				initialized = true;
			}
		}

		////////////////////////////////////////////////////////

		public delegate void ChangedHandler (QueryDriver          source,
						     Queryable            queryable,
						     IQueryableChangeData changeData);

		public event ChangedHandler ChangedEvent;


		// FIXME: Would we have problems if there were multiple QueryDriver
		// objects floating around?
		
		// FIXME: There should be a way to disconnect this OnQueryableChanged
		// from the ChangedEvents.
		public QueryDriver ()
		{
			Initialize ();
			foreach (Queryable q in queryables) {
				q.ChangedEvent += OnQueryableChanged;
			}
		}

		public void Start ()
		{
			foreach (Queryable q in queryables) {
				q.Start ();
			}
		}

		public void DoQuery (QueryBody body, QueryResult result)
		{
			// The extra pair of calls to WorkerStart/WorkerFinished ensures that
			// the QueryResult will fire the StartedEvent and FinishedEvent,
			// even if no queryable accepts the query.
			if (!result.WorkerStart (this)) {
				return;
			}
			
			try {
				foreach (Queryable queryable in queryables) {
					if (queryable.AcceptQuery (body))
						queryable.DoQuery (body, result, null);
				}
			} finally {
				result.WorkerFinished (this);
			}
		}

		public void DoQueryChange (Queryable            queryable,
					   IQueryableChangeData changeData,
					   QueryBody            body,
					   QueryResult          result)
		{
			if (queryable.AcceptQuery (body)) 
				queryable.DoQuery (body, result, changeData);
		}

		private void OnQueryableChanged (Queryable            source,
						 IQueryableChangeData changeData)
		{
			if (ChangedEvent != null) {
				ChangedEvent (this, source, changeData);
			}
		}

		////////////////////////////////////////////////////////

		public string GetHumanReadableStatus ()
		{
			StringBuilder builder = new StringBuilder ("\n");

			foreach (Queryable q in queryables) {
				if (builder.Length > 1)
					builder.Append ("\n--------------------------------------\n\n");
				builder.Append (q.Name);
				builder.Append (":\n");
				builder.Append (q.GetHumanReadableStatus ());
				builder.Append ("\n");
			}
			builder.Append ("\n");
			
			return builder.ToString ();
		}
	}
}
