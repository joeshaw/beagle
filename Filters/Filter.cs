//
// Filter.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//


using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Beagle.Filters {

	public abstract class Filter {

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
		Hashtable     properties;
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
		
		//////////////////////////

		protected virtual void DoOpen (Stream stream) { }

		protected abstract void DoPull ();

		//////////////////////////
		
		private string PullContent ()
		{
			if (content == null)
				DoPull ();
			if (content == null)
				return null;
			string str = content.ToString ();
			content = null;
			return str;
		}
		
		public TextReader Content {
			get { return new Beagle.Util.PullingReader (new Beagle.Util.PullingReader.Pull (PullContent)); }
		}

		public TextReader HotContent {
			get {
				if (hot == null)
					return null;
				StringReader sr = new StringReader (hot.ToString ());
				hot = null;
				return sr;
			}
		}

		public IDictionary Properties {
			get { return properties; }
		}

		public ICollection Keys {
			get { return properties.Keys; }
		}

		public String this [String key] {
			get { return (String) properties [key]; }
			set { 
				if (value == null) {
					if (properties.Contains (key))
						properties.Remove (key);
					return;
				}
				properties [key] = value as String;
			}
		}

		//////////////////////////

		public void Open (Stream stream)
		{
			content = null;
			hot = null;
			properties = new Hashtable (new CaseInsensitiveHashCodeProvider (), 
						    new CaseInsensitiveComparer ());
			hotCount = 0;
			freezeCount = 0;

			DoOpen (stream);
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
			return FromFlavor (flavor) != null;
		}

		static public Filter FromFlavor (Flavor flavor)
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
			return FromFlavor (flavor);
		}

		static public Filter FilterFromPath (String path)
		{
			Flavor flavor = Flavor.FromPath (path);
			return FromFlavor (flavor);
		}
	}
}
