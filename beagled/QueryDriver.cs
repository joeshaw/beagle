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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using Beagle.Util;

namespace Beagle.Daemon {
	
	public class QueryDriver {

		// Enable or Disable specific Queryables by name

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
			if (allowed_queryables.Count == 0
			    && denied_queryables.Count == 0)
				return true;

			name = name.ToLower ();

			if (allowed_queryables.Count > 0) {
				foreach (string allowed in allowed_queryables)
					if (name == allowed)
						return true;
				return false;
			}

			foreach (string denied in denied_queryables)
				if (name == denied)
					return false;
			return true;

		}

		//////////////////////////////////////////////////////////////////////////////////////

		// Use introspection to find all classes that implement IQueryable, the construct
		// associated Queryables objects.

		static ArrayList queryables = new ArrayList ();
		static Hashtable iqueryable_to_queryable = new Hashtable ();

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
			int count = 0;

			foreach (Type type in GetQueryableTypes (assembly)) {
				foreach (QueryableFlavor flavor in GetQueryableFlavors (type)) {
					if (! UseQueryable (flavor.Name))
						continue;

					if (flavor.RequireInotify && ! Inotify.Enabled) {
						Logger.Log.Warn ("Can't start backend '{0}' without inotify",  flavor.Name);
						continue;
					}

					IQueryable iq = null;
					try {
						iq = Activator.CreateInstance (type) as IQueryable;
					} catch (Exception e) {
						Logger.Log.Error ("Caught exception while instantiating {0} backend", flavor.Name);
						Logger.Log.Error (e);
					}

					if (iq != null) {
						Queryable q = new Queryable (flavor, iq);
						queryables.Add (q);
						iqueryable_to_queryable [iq] = q;
						++count;
						break;
					}
				}
			}
			Logger.Log.Debug ("Found {0} types in {1}", count, assembly.FullName);
		}

		static private Type[] GetQueryableTypes (Assembly assembly)
		{
			Type[] assembly_types = assembly.GetTypes ();
			ArrayList types = new ArrayList (assembly_types.Length);

			foreach (Type type in assembly_types)
				if (TypeImplementsInterface (type, typeof (IQueryable)))
					types.Add (type);
		
			return (Type[]) types.ToArray (typeof (Type));
		}
		
		static private QueryableFlavor[] GetQueryableFlavors (Type type)
		{
			object[] attributes = Attribute.GetCustomAttributes (type);
			ArrayList flavors = new ArrayList (attributes.Length);
			
			foreach (object obj in attributes) {
					QueryableFlavor flavor = obj as QueryableFlavor;
					if (flavor != null)
						flavors.Add (flavor);
			}

			return (QueryableFlavor[]) flavors.ToArray (typeof (QueryableFlavor));
		}

		static private Assembly[] GetAssemblies ()
		{
			Assembly[] assemblies;
			int i = 0;
			DirectoryInfo backends = new DirectoryInfo (PathFinder.BackendDir);

			if (backends.Exists) {
				FileInfo[] assembly_files = backends.GetFiles ("*.dll");
				assemblies = new Assembly [assembly_files.Length + 1];

				foreach (FileInfo assembly in assembly_files)
					assemblies[i++] = Assembly.LoadFile (assembly.ToString ());

			} else {
				assemblies = new Assembly [1];
			}

			assemblies[i] = Assembly.GetExecutingAssembly ();
		
			return assemblies;
		}

		static public void Start ()
		{
			foreach (Assembly assembly in GetAssemblies ()) {
				ScanAssembly (assembly);

				// This allows backends to define their
				// own executors.
				Server.ScanAssemblyForExecutors (assembly);
			}

			foreach (Queryable q in queryables)
				q.Start ();
		}

		static public string ListBackends ()
		{
			string ret = "";
			foreach (Assembly assembly in GetAssemblies ())
				foreach (Type type in GetQueryableTypes (assembly))
					foreach (QueryableFlavor flavor in GetQueryableFlavors (type))
						ret += " - " + flavor.Name + "\n";

			return ret;
		}

		static public Queryable GetQueryable (string name)
		{
			foreach (Queryable q in queryables) {
				if (q.Name == name)
					return q;
			}

			return null;
		}

		////////////////////////////////////////////////////////

		public delegate void ChangedHandler (Queryable            queryable,
						     IQueryableChangeData changeData);

		static public event ChangedHandler ChangedEvent;

		// A method to fire the ChangedEvent event.
		static public void QueryableChanged (IQueryable           iqueryable,
						     IQueryableChangeData change_data)
		{
			if (ChangedEvent != null) {
				Queryable queryable = iqueryable_to_queryable [iqueryable] as Queryable;
				ChangedEvent (queryable, change_data);
			}
		}

		////////////////////////////////////////////////////////

		private class QueryClosure : IQueryWorker {

			Queryable queryable;
			Query query;
			QueryResult result;
			IQueryableChangeData change_data;
			
			public QueryClosure (Queryable            queryable,
					     Query                query,
					     QueryResult          result,
					     IQueryableChangeData change_data)
			{
				this.queryable = queryable;
				this.query = query;
				this.result = result;
				this.change_data = change_data;
			}

			public void DoWork ()
			{
				HitRegulator regulator = result.GetHitRegulator (queryable);
				regulator.MaxHits = query.MaxHits;
				queryable.DoQuery (query, regulator, change_data);
				regulator.Flush (result);
			}
		}

		static public void DoOneQuery (Queryable            queryable,
					       Query                query,
					       QueryResult          result,
					       IQueryableChangeData change_data)
		{
			if (queryable.AcceptQuery (query)) {
				QueryClosure qc = new QueryClosure (queryable, query, result, change_data);
				result.AttachWorker (qc);
			}
		}

		static public void DoQuery (Query query, QueryResult result)
		{
			// The extra pair of calls to WorkerStart/WorkerFinished ensures:
			// (1) that the QueryResult will fire the StartedEvent
			// and FinishedEvent, even if no queryable accepts the
			// query.
			// (2) that the FinishedEvent will only get called when all of the
			// backends have had time to finish.

			object dummy_worker = new object ();

			if (! result.WorkerStart (dummy_worker))
				return;
			
			foreach (Queryable queryable in queryables)
				DoOneQuery (queryable, query, result, null);
			
			result.WorkerFinished (dummy_worker);
		}

		////////////////////////////////////////////////////////

		static public string GetIndexInformation ()
		{
			StringBuilder builder = new StringBuilder ("\n");

			foreach (Queryable q in queryables) {
				builder.AppendFormat ("Name: {0}\n", q.Name);
				builder.AppendFormat ("Count: {0}\n", q.GetItemCount ());
				builder.Append ("\n");
			}

			return builder.ToString ();
		}
	}
}
