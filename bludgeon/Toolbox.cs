using System;
using System.Collections;
using System.Reflection;

using Beagle.Util;

namespace Bludgeon {

	public class Toolbox {

		private class Pair {
			public HammerAttribute Attribute;
			public Type Type;

			public IHammer CreateInstance () {
				return Activator.CreateInstance (Type) as IHammer;	
			}
		}

		static ArrayList all_pairs = new ArrayList ();
		static Hashtable pairs_by_name = new Hashtable ();

		/////////////////////////////////////////////////////

		private Toolbox () { } // This is a static class

		static Toolbox () 
		{
			LoadAllPairs (Assembly.GetExecutingAssembly ());
		}

		// These are strings
		static public ICollection HammerNames {
			get { return pairs_by_name.Keys; }
		}

		/////////////////////////////////////////////////////

		private class MixedHammer : IHammer {
			Random random = new Random ();
			ArrayList hammers = new ArrayList ();

			public void Add (IHammer hammer)
			{
				hammers.Add (hammer);
			}

			public int Count {
				get { return hammers.Count; }
			}

			public bool HammerOnce (DirectoryObject dir, EventTracker tracker)
			{
				// We randomly pick an IHammer, and call HammerOnce on it.
				// If it returns false, we try the next IHammer until we
				// find one that returns true or until we've exhausted
				// all possibilities.
				int i;
				i = random.Next (hammers.Count);

				for (int j = 0; j < hammers.Count; ++j) {
					int k = (i + j) % hammers.Count;
					IHammer hammer;
					hammer = hammers [k] as IHammer;
					if (hammer.HammerOnce (dir, tracker))
						return true;
				}

				return false;
			}
		}

		static private IHammer GetMixedHammer (string name)
		{
			string [] parts;
			parts = name.Split (',');
			
			MixedHammer mixed;
			mixed = new MixedHammer ();
			foreach (string part in parts) {
				IHammer hammer;

				if (part.IndexOf ('*') != -1) {
					foreach (IHammer match in GetMatchingHammers (part))
						mixed.Add (match);
					continue;
				}

				hammer = GetHammer (part);
				if (hammer != null)
					mixed.Add (hammer);
			}

			return mixed.Count > 0 ? mixed : null;
		}

		static public IHammer GetHammer (string name)
		{
			if (name.IndexOf (',') != -1 || name.IndexOf ('*') != -1)
				return GetMixedHammer (name);

			Pair pair;
			pair = pairs_by_name [name] as Pair;
			if (pair == null)
				return null; // should probably throw exception

			return pair.CreateInstance ();
		}

		static public ICollection GetMatchingHammers (string pattern)
		{
			ArrayList matches;
			matches = new ArrayList ();

			foreach (Pair pair in all_pairs)
				if (StringFu.GlobMatch (pattern, pair.Attribute.Name))
					matches.Add (pair.CreateInstance ());

			return matches;
		}

		/////////////////////////////////////////////////////

		// Yuck.

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

		static private void LoadAllPairs (Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes ()) {

				if (TypeImplementsInterface (type, typeof (IHammer))) {

					object [] attributes;
					attributes = Attribute.GetCustomAttributes (type);

					foreach (object obj in attributes) {
						HammerAttribute attr;
						attr = obj as HammerAttribute;
						if (attr != null) {

							Pair pair;
							pair = new Pair ();
							pair.Attribute = attr;
							pair.Type = type;
							
							all_pairs.Add (pair);
							pairs_by_name [pair.Attribute.Name] = pair;

							break;
						}
					}
				}
			}
		}
	}
}
