//
// Filter.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Dewey.Filters {

	abstract public class Filter {

		// Derived classes always must have a constructor that
		// takes no arguments.
		public Filter () { }

		//////////////////////////

		protected String name = "UnNamed";

		public String Name {
			get { return name; }
		}

		//////////////////////////
		
		private   ArrayList supportedFlavors = new ArrayList ();
		protected Flavor flavor;

		protected void AddSupportedFlavor (Flavor flavor)
		{
			supportedFlavors.Add (flavor);
		}

		protected void AddSupportedMimeType (String mimeType)
		{
			Flavor flavor = new Flavor (mimeType, Flavor.Wildcard);
			AddSupportedFlavor (flavor);
		}

		protected void AddSupportedExtension (String extension)
		{
			Flavor flavor = new Flavor (Flavor.Wildcard, extension);
			AddSupportedFlavor (flavor);
		}

		public IEnumerable SupportedFlavors {
			get { return supportedFlavors; }
		}

		public Flavor Flavor {
			get { return flavor; }
		}

		//////////////////////////
		
		StringBuilder content;
		StringBuilder hot;
		Hashtable     metadata;
		int           hotCount;
		int           freezeCount;
		bool          closeStream = false;

		protected void HotUp ()
		{
			++hotCount;
		}
		
		protected void HotDown ()
		{
			if (hotCount > 0) {
				--hotCount;
				if (hotCount == 0)
					BuilderAppendWhitespace (hot);
			}
		}

		protected void FreezeUp ()
		{
			++freezeCount;
		}

		protected void FreezeDown ()
		{
			if (freezeCount > 0)
				--freezeCount;
		}
		
		protected void AppendContent (String str)
		{
			if (freezeCount == 0 && str != null) {
				if (content == null)
					content = new StringBuilder ("");
				content.Append (str);
				if (hotCount > 0) {
					if (hot == null)
						hot = new StringBuilder ("");
					hot.Append (str);
				}
			}
		}

		static void BuilderAppendWhitespace(StringBuilder builder)
		{
			if (builder != null)
				builder.Append (" ");
		}

		protected void AppendWhiteSpace ()
		{
			BuilderAppendWhitespace (content);
			BuilderAppendWhitespace (hot);
		}
		
		protected void SetMetadata (String key, String val)
		{
			key = key.ToLower ();
			if (key == null)
				throw new Exception ("Metadata keys may not be null");
			if (metadata.Contains (key))
				throw new Exception ("Clobbering metadata " + key);
			if (val != null)
				metadata[key] = val;
		}
		
		//////////////////////////
		
		static String CleanUp (StringBuilder builder)
		{
			if (builder == null)
				return null;
			String str = builder.ToString ();
			str = Regex.Replace (str, "\\s{2,}", " ");
			str = str.Trim ();
			return str;
		}
		
		public String Content {
			get { return CleanUp (content); }
		}

		public String HotContent {
			get { return CleanUp (hot); }
		}

		public ICollection MetadataKeys {
			get { return metadata.Keys; }
		}

		public String this [String key] {
			get { return metadata[key.ToLower ()] as String; }
		}

		//////////////////////////

		abstract protected void Read (Stream stream);
		
		//////////////////////////

		public void Open (Stream stream)
		{
			content = null;
			hot = null;
			metadata = new Hashtable ();
			hotCount = 0;
			freezeCount = 0;
			
			if (stream != null)
				Read (stream);
		}

		public void Open (String path)
		{
			Stream stream = new FileStream (path,
							FileMode.Open,
							FileAccess.Read);
			Open (stream);
			stream.Close ();
		}
		
		public void Close ()
		{
			content = null;
			hot = null;
			metadata = null;
		}
		
		//////////////////////////

		static SortedList registry = null;
		
		static private void AutoRegisterFilters ()
		{
			Assembly a = Assembly.GetAssembly (typeof (Filter));
			foreach (Type t in a.GetTypes ()) {
				if (t.IsSubclassOf (typeof (Filter))) {
					Filter filter = (Filter) Activator.CreateInstance (t);
					foreach (Flavor flavor in filter.SupportedFlavors) {
						if (registry.ContainsKey (flavor)) {
							Type otherType = (Type) registry [flavor];
							Filter other = (Filter) Activator.CreateInstance (otherType);
							String estr = String.Format ("Type Collision: {0} ({1} vs {2})",
										     flavor,
										     filter.Name,
										     other.Name);
							throw new Exception (estr);
						}
						registry [flavor] = t;
					}
				}
			}
		}

		static public bool CanFilter (Flavor flavor)
		{
			return FilterFromFlavor (flavor) != null;
		}

		static public Filter FilterFromFlavor (Flavor flavor)
		{
			if (registry == null) {
				registry = new SortedList ();
				AutoRegisterFilters ();
			}

			if (flavor.IsPattern)
				throw new Exception ("Can't create filter from content type pattern " + flavor);

			Filter filter = null;

			foreach (Flavor other in registry.Keys) {
				if (other.IsMatch (flavor)) {
					Type t = (Type) registry [other];
					filter = (Filter) Activator.CreateInstance (t);
					filter.flavor = flavor;
				}
			}
			
			return filter;
		}

		static public Filter FilterFromMimeType (String mimeType)
		{
			Flavor flavor = Flavor.FromMimeType (mimeType);
			return FilterFromFlavor (flavor);
		}

		static public Filter FilterFromPath (String path)
		{
			Flavor flavor = Flavor.FromPath (path);
			return FilterFromFlavor (flavor);
		}
	}
}
