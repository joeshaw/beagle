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

		// Paths to static queryables

		static ArrayList static_queryables = new ArrayList ();
		
		static public void AddStaticQueryable (string path) {

			if (! static_queryables.Contains (path))
				static_queryables.Add (path);
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
						Logger.Log.Warn ("Can't start backend '{0}' without inotify", flavor.Name);
						continue;
					}

					if (flavor.RequireExtendedAttributes && ! ExtendedAttribute.Supported) {
						Logger.Log.Warn ("Can't start backend '{0}' without extended attributes", flavor.Name);
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

		////////////////////////////////////////////////////////

		// Scans PathFinder.SystemIndexesDir after available 
		// system-wide indexes.
		static void LoadSystemIndexes () 
		{
			if (!Directory.Exists (PathFinder.SystemIndexesDir))
				return;
			
			int count = 0;

			foreach (DirectoryInfo index_dir in new DirectoryInfo (PathFinder.SystemIndexesDir).GetDirectories ()) {
				if (! UseQueryable (index_dir.Name))
					continue;
				
				if (LoadStaticQueryable (index_dir, QueryDomain.System))
					count++;
			}

			Logger.Log.Debug ("Found {0} system-wide indexes", count);
		}

		// Scans configuration for user-specified index paths 
		// to load StaticQueryables from.
		static void LoadStaticQueryables () 
		{
			int count = 0;

			foreach (string path in Conf.Daemon.StaticQueryables)
				static_queryables.Add (path);

			foreach (string path in static_queryables) {
				DirectoryInfo index_dir = new DirectoryInfo (path);

				if (!index_dir.Exists)
					continue;
				
				// FIXME: QueryDomain might be other than local
				if (LoadStaticQueryable (index_dir, QueryDomain.Local))
					count++;
			}

			Logger.Log.Debug ("Found {0} user-configured static queryables", count);
		}

		// Instantiates and loads a StaticQueryable from an index directory
		static private bool LoadStaticQueryable (DirectoryInfo index_dir, QueryDomain query_domain) 
		{
			StaticQueryable static_queryable = null;
			
			if (!index_dir.Exists)
				return false;
			
			try {
				static_queryable = new StaticQueryable (index_dir.Name, index_dir.FullName, true);
			} catch (Exception e) {
				Logger.Log.Error ("Caught exception while instantiating static queryable: {0}", index_dir.Name);
				Logger.Log.Error (e);					
				return false;
			}
			
			if (static_queryable != null) {
				QueryableFlavor flavor = new QueryableFlavor ();
				flavor.Name = index_dir.Name;
				flavor.Domain = query_domain;
				
				Queryable queryable = new Queryable (flavor, static_queryable);
				queryables.Add (queryable);
				
				iqueryable_to_queryable [static_queryable] = queryable;

				return true;
			}

			return false;
		}

		////////////////////////////////////////////////////////

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

			LoadSystemIndexes ();

			if (UseQueryable ("static"))
				LoadStaticQueryables ();

			foreach (Queryable q in queryables)
				q.Start ();
		}

		static public string ListBackends ()
		{
			string ret = "User:\n";
			foreach (Assembly assembly in GetAssemblies ())
				foreach (Type type in GetQueryableTypes (assembly))
					foreach (QueryableFlavor flavor in GetQueryableFlavors (type))
				ret += String.Format (" - {0}\n", flavor.Name);
			
			if (!Directory.Exists (PathFinder.SystemIndexesDir)) 
				return ret;
			
			ret += "System:\n";
			foreach (DirectoryInfo index_dir in new DirectoryInfo (PathFinder.SystemIndexesDir).GetDirectories ()) {
				ret += String.Format (" - {0}\n", index_dir.Name);
			}

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

		private class MarkAndForwardHits : IQueryResult {

			IQueryResult result;
			string name;
			
			public MarkAndForwardHits (IQueryResult result, string name)
			{
				this.result = result;
				this.name = name;
			}

			public void Add (ICollection some_hits)
			{
				foreach (Hit hit in some_hits)
					if (hit != null)
						hit.SourceObjectName = name;
				result.Add (some_hits);
			}

			public void Subtract (ICollection some_uris)
			{
				result.Subtract (some_uris);
			}
		}

		private class QueryClosure : IQueryWorker {

			Queryable queryable;
			Query query;
			IQueryResult result;
			IQueryableChangeData change_data;
			
			public QueryClosure (Queryable            queryable,
					     Query                query,
					     QueryResult          result,
					     IQueryableChangeData change_data)
			{
				this.queryable = queryable;
				this.query = query;
				this.result = new MarkAndForwardHits (result, queryable.Name);
				this.change_data = change_data;
			}

			public void DoWork ()
			{
				queryable.DoQuery (query, result, change_data);
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

		static void AddSearchTermInfo (QueryPart          part,
					       SearchTermResponse response)
		{
			if (part.Logic == QueryPartLogic.Prohibited)
				return;

			if (part is QueryPart_Or) {
				ICollection sub_parts;
				sub_parts = ((QueryPart_Or) part).SubParts;
				foreach (QueryPart qp in sub_parts)
					AddSearchTermInfo (qp, response);
				return;
			}

			if (! (part is QueryPart_Text))
				return;

			QueryPart_Text tp;
			tp = (QueryPart_Text) part;

			string [] split;
			split = tp.Text.Split (' ');
 
			// First, remove stop words
			for (int i = 0; i < split.Length; ++i)
				if (LuceneCommon.IsStopWord (split [i]))
					split [i] = null;

			// Assemble the phrase minus stop words
			StringBuilder sb;
			sb = new StringBuilder ();
			for (int i = 0; i < split.Length; ++i) {
				if (split [i] == null)
					continue;
				if (sb.Length > 0)
					sb.Append (' ');
				sb.Append (split [i]);
			}
			response.ExactText.Add (sb.ToString ());

			// Now assemble a stemmed version
			sb.Length = 0; // clear the previous value
			for (int i = 0; i < split.Length; ++i) {
				if (split [i] == null)
					continue;
				if (sb.Length > 0)
					sb.Append (' ');
				sb.Append (LuceneCommon.Stem (split [i]));
			}
			response.StemmedText.Add (sb.ToString ());
		}

		////////////////////////////////////////////////////////

		static private void DehumanizeQuery (Query query)
		{
			// We need to remap any QueryPart_Human parts into
			// lower-level part types.  First, we find any
			// QueryPart_Human parts and explode them into
			// lower-level types.
			ArrayList new_parts = null;
			foreach (QueryPart abstract_part in query.Parts) {
				if (abstract_part is QueryPart_Human) {
					QueryPart_Human human = abstract_part as QueryPart_Human;
					if (new_parts == null)
						new_parts = new ArrayList ();
					foreach (QueryPart sub_part in QueryStringParser.Parse (human.QueryString))
						new_parts.Add (sub_part);
				}
			}

			// If we found any QueryPart_Human parts, copy the
			// non-Human parts over and then replace the parts in
			// the query.
			if (new_parts != null) {
				foreach (QueryPart abstract_part in query.Parts) {
					if (! (abstract_part is QueryPart_Human))
						new_parts.Add (abstract_part);
				}
				
				query.ClearParts ();
				foreach (QueryPart part in new_parts)
					query.AddPart (part);
			}

		}

		static private SearchTermResponse AssembleSearchTermResponse (Query query)
		{
			SearchTermResponse search_term_response;
			search_term_response = new SearchTermResponse ();
			foreach (QueryPart part in query.Parts)
				AddSearchTermInfo (part, search_term_response);
			return search_term_response;
		}

		static private void QueryEachQueryable (Query       query,
							QueryResult result)
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
		
		static public void DoQueryLocal (Query       query,
						 QueryResult result)
		{
			DehumanizeQuery (query);

			SearchTermResponse search_term_response;
			search_term_response = AssembleSearchTermResponse (query);
			query.ProcessSearchTermResponse (search_term_response);

			QueryEachQueryable (query, result);
		}

		static public void DoQuery (Query                                query,
					    QueryResult                          result,
					    RequestMessageExecutor.AsyncResponse send_response)
		{
			DehumanizeQuery (query);

			SearchTermResponse search_term_response;
			search_term_response = AssembleSearchTermResponse (query);
			send_response (search_term_response);

			QueryEachQueryable (query, result);
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
