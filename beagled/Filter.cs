//
// Filter.cs
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
using System.IO;
using System.Text;
using System.Reflection;

using BU = Beagle.Util;

namespace Beagle.Daemon {

	public class Filter {

		// Derived classes always must have a constructor that
		// takes no arguments.
		public Filter () { }

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

		int hotCount = 0;
		int freezeCount = 0;

		public void HotUp ()
		{
			++hotCount;
		}
		
		public void HotDown ()
		{
			if (hotCount > 0) {
				--hotCount;
				if (hotCount == 0)
					AppendWhiteSpace ();
			}
		}

		public bool IsHot {
			get { return hotCount > 0; }
		}

		public void FreezeUp ()
		{
			++freezeCount;
		}

		public void FreezeDown ()
		{
			--freezeCount;
		}

		public bool IsFrozen {
			get { return freezeCount > 0; }
		}

		//////////////////////////

		private ArrayList textPool;
		private ArrayList hotPool;
		private ArrayList propertyPool;

		public void AppendText (string str)
		{
			Console.WriteLine ("AppendText (\"{0}\")", str);
			if (! IsFrozen && str != null && str != "") {
				textPool.Add (str);
				if (IsHot)
					hotPool.Add (str);
			}
		}

		private bool NeedsWhiteSpace (ArrayList array)
		{
			if (array.Count == 0)
				return false;
			
			string last = (string) array [array.Count-1];
			if (last.Length > 0
			    && char.IsWhiteSpace (last [last.Length-1]))
				return false;

			return true;
		}

		public void AppendWhiteSpace ()
		{
			Console.WriteLine ("AppendWhiteSpace ()");
			if (NeedsWhiteSpace (textPool))
				textPool.Add (" ");
			if (NeedsWhiteSpace (hotPool))
				hotPool.Add (" ");
			
		}

		public void AddProperty (Property prop)
		{
			if (prop != null && prop.Value != null)
				propertyPool.Add (prop);
		}

		//////////////////////////

		private bool isFinished = false;

		public bool IsFinished {
			get { return isFinished; }
		}
		
		protected void Finished ()
		{
			isFinished = true;
		}

		//////////////////////////

		protected virtual void DoOpen (FileInfo info) { }

		protected virtual void DoPullProperties () { }

		protected virtual void DoPullSetup () { }

		protected virtual void DoPull () { Finished (); }

		protected virtual void DoClose () { }

		//////////////////////////

		/*
		  Open () calls:
		  (1) DoOpen (FileInfo info) or DoOpen (Stream)
		  (2) DoPullProperties ()
		  (3) DoPullSetup ()
		  At this point all properties must be in place

		  Once someone starts reading from the TextReader,
		  the following are called:
		  DoPull () [until Finished() is called]
		  DoClose () [when finished]
		  
		*/

		private bool isOpen = false;
		private FileInfo currentInfo;
		private string tempFile = null;

		public void Open (Stream stream)
		{
			// If we are handed a stream, dump it into
			// a temporary file.
			tempFile = Path.GetTempFileName();
			Stream tempStream = File.OpenWrite (tempFile);

			const int BUFFER_SIZE = 8192;
			byte[] buffer = new byte [BUFFER_SIZE];
			int n;
			while ((n = stream.Read (buffer, 0, BUFFER_SIZE)) > 0) {
				tempStream.Write (buffer, 0, n);
			}

			tempStream.Close ();

			Open (new FileInfo (tempFile));
		}

		public void Open (FileInfo info)
		{
			isFinished = false;
			textPool = new ArrayList ();
			hotPool = new ArrayList ();
			propertyPool = new ArrayList ();

			currentInfo = info;
			
			DoOpen (info);
			isOpen = true;
			
			if (IsFinished) {
				isOpen = false;
				return;
			}
			
			DoPullProperties ();
			if (IsFinished) {
				isOpen = false;
				return;
			}

			DoPullSetup ();
			if (IsFinished) {
				isOpen = false;
				return;
			}
		}

		public FileInfo CurrentFileInfo {
			get { return currentInfo; }
		}

		private bool Pull () 
		{
			if (IsFinished) {
				DoClose ();
				Cleanup ();
				return false;
			}

			DoPull ();

			return true;
		}

		public void Cleanup ()
		{
			if (tempFile != null)
				File.Delete (tempFile);
		}

		private string PullFromArray (ArrayList array)
		{
			while (array.Count == 0 && Pull ()) { }
			if (array.Count > 0) {
				string str = (string) array [0];
				array.RemoveAt (0);
				return str;
			}
			return null;
		}

		private string PullText ()
		{
			return PullFromArray (textPool);
		}

		private string PullHotText ()
		{
			return PullFromArray (hotPool);
		}

		public TextReader GetTextReader ()
		{
			return new BU.PullingReader (new BU.PullingReader.Pull (PullText));
		}

		public TextReader GetHotTextReader ()
		{
			return new BU.PullingReader (new BU.PullingReader.Pull (PullHotText));
		}

		public IEnumerable Properties {
			get { return propertyPool; }
		}

		//////////////////////////

		static SortedList registry = null;

		static private int ScanAssemblyForFilters (Assembly assembly)
		{
			int count = 0;

			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (Filter))) {
					Filter filter = (Filter) Activator.CreateInstance (t);
					bool first = true;
					foreach (Flavor flavor in filter.SupportedFlavors) {
						if (registry.ContainsKey (flavor)) {
							Type otherType = (Type) registry [flavor];
							Filter other = (Filter) Activator.CreateInstance (otherType);
							String estr = String.Format ("Type Collision: {0} ({1} vs {2})",
										     flavor, filter, other);
							throw new Exception (estr);
						}
						registry [flavor] = t;
						if (first) {
							++count;
							first = false;
						}
					}
				}
			}

			return count;
		}

		static private void FindAssemblies (string dir)
		{
			if (dir == null || dir == "")
				return;

			if (! Directory.Exists (dir)) {
				//Console.WriteLine ("'{0}' is not a directory: No filters loaded", dir);
				return;
			}

			DirectoryInfo dirInfo = new DirectoryInfo (dir);
			foreach (FileInfo file in dirInfo.GetFiles ()) {
				if (file.Extension == ".dll") {
					Assembly a = Assembly.LoadFrom (file.FullName);
					int n = ScanAssemblyForFilters (a);
					//Console.WriteLine ("Loaded {0} filters from {1}", n, file.FullName);
				}
			}
		}

		static private void AutoRegisterFilters ()
		{
			string path = Environment.GetEnvironmentVariable ("BEAGLE_FILTER_PATH");
			
			if (path == null || path == "")
				path = PathFinder.FilterDir;
			else if (path [path.Length-1] == ':')
				path += PathFinder.FilterDir;

			foreach (string dir in path.Split (':')) 
				FindAssemblies (dir);
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
