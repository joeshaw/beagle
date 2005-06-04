//
// Conf.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Reflection;
using System.Xml.Serialization;

using Beagle.Util;
namespace Beagle.Util {

	public class Conf {

		// No instantiation
		private Conf () { }

		public static Hashtable Sections;
		public static IndexingConfig Indexing = null;
		private static string configs_dir;
		private static Hashtable mtimes;
		private static Hashtable subscriptions;
		private static bool watching_for_updates;
		private static int update_wd;

		public delegate void ConfigUpdateHandler (Section section);

		static Conf ()
		{
			Sections = new Hashtable (1);
			mtimes = new Hashtable (1);
			subscriptions = new Hashtable (1);

			configs_dir = Path.Combine (PathFinder.StorageDir, "config");
			if (!Directory.Exists (configs_dir))
				Directory.CreateDirectory (configs_dir);

			// We'll start processing file update notifications after we've loaded the
			// configuration for the first time.
			watching_for_updates = false;
		}

		public static void WatchForUpdates ()
		{
			if (update_wd > 0)
				return;

			if (Inotify.Enabled) {
				Inotify.Event += OnInotifyEvent;
				update_wd = Inotify.Watch (configs_dir, Inotify.EventType.Create | Inotify.EventType.Modify);
			} else {
				// Poll for updates every 60 secs
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForUpdates));
				update_wd = 1;
			}
		}

		private static void OnInotifyEvent (int wd, string path, string subitem, string srcpath, Inotify.EventType type)
		{
			if (wd != update_wd || subitem == "" || watching_for_updates == false)
				return;

			Load ();
		}

		private static bool CheckForUpdates ()
		{
			if (watching_for_updates)
				Load ();
			return true;
		}

		public static void Subscribe (Type type, ConfigUpdateHandler callback)
		{
			if (!subscriptions.ContainsKey (type))
				subscriptions.Add (type, new ArrayList (1));
			
			ArrayList callbacks = (ArrayList) subscriptions [type];
			callbacks.Add (callback);
		}

		private static void NotifySubscribers (Section section)
		{
			Type type = section.GetType ();
			ArrayList callbacks = (ArrayList) subscriptions [type];

			if (callbacks == null)
				return;

			foreach (ConfigUpdateHandler callback in callbacks)
				callback (section);
		}

		public static void Load ()
		{
			Load (false);
		}

		public static void Load (bool force)
		{
			Section temp;
	
			LoadFile (typeof (IndexingConfig), Indexing, out temp, force);
			Indexing = (IndexingConfig) temp;
			NotifySubscribers (Indexing);

			watching_for_updates = true;
		}

		public static void Save ()
		{
			Save (false);
		}

		public static void Save (bool force)
		{
			foreach (Section section in Sections.Values)
				if (force || section.SaveNeeded)
					SaveFile (section);
		}

		private static bool LoadFile (Type type, Section current, out Section section, bool force)
		{
			section = current;
			object [] attrs = Attribute.GetCustomAttributes (type, typeof (ConfigSection));
			if (attrs.Length == 0)
				throw new Exception ("Could not find ConfigSection attribute on " + type);

			string sectionname = ((ConfigSection) attrs [0]).Name;
			string filename = sectionname + ".xml";
			string filepath = Path.Combine (configs_dir, filename);
			if (!File.Exists (filepath)) {
				if (current == null)
					ConstructDefaultSection (type, sectionname, out section);
				return false;
			}
			if (!force && current != null && mtimes.ContainsKey (sectionname) &&
					File.GetLastWriteTimeUtc (filepath).CompareTo ((DateTime) mtimes [sectionname]) < 0)
				return false;

			Logger.Log.Debug ("Loading {0} from {1}", type, filename);
			FileStream fs = null;

			try {
				fs = File.OpenRead (filepath);
				XmlSerializer serializer = new XmlSerializer (type);
				section = (Section) serializer.Deserialize (fs);
			} catch (Exception e) {
				Logger.Log.Error ("Could not load configuration from {0}: {1}", filename, e.Message);
				if (fs != null)
					fs.Close ();
				if (current == null)
					ConstructDefaultSection (type, sectionname, out section);
				return false;
			}

			fs.Close ();
			Sections.Remove (sectionname);
			Sections.Add (sectionname, section);
			mtimes.Remove (sectionname);
			mtimes.Add (sectionname, File.GetLastWriteTimeUtc (filepath));
			return true;
		}

		private static bool SaveFile (Section section)
		{
			Type type = section.GetType ();
			object [] attrs = Attribute.GetCustomAttributes (type, typeof (ConfigSection));
			if (attrs.Length == 0)
				throw new Exception ("Could not find ConfigSection attribute on " + type);

			string sectionname = ((ConfigSection) attrs [0]).Name;
			string filename = sectionname + ".xml";
			string filepath = Path.Combine (configs_dir, filename);

			Logger.Log.Debug ("Saving {0} to {1}", type, filename);
			FileStream fs = null;

			try {
				watching_for_updates = false;
				fs = new FileStream (filepath, FileMode.Create);
				XmlSerializer serializer = new XmlSerializer (type);
				serializer.Serialize (fs, section);
			} catch (Exception e) {
				if (fs != null)
					fs.Close ();
				Logger.Log.Error ("Could not save configuration to {0}: {1}", filename, e);
				watching_for_updates = true;
				return false;
			}

			fs.Close ();
			mtimes.Remove (sectionname);
			mtimes.Add (sectionname, File.GetLastWriteTimeUtc (filepath));
			watching_for_updates = true;
			return true;
		}

		private static void ConstructDefaultSection (Type type, string sectionname, out Section section)
		{
			ConstructorInfo ctor = type.GetConstructor (Type.EmptyTypes);
			section = (Section) ctor.Invoke (null);
			Sections.Remove (sectionname);
			Sections.Add (sectionname, section);
		}

		// Lists all config file options in a hash table where key is option name,
		// and value is description.
		public static Hashtable GetOptions (Section section)
		{
			Hashtable options = new Hashtable ();
			MemberInfo [] members = section.GetType ().GetMembers ();

			// Find all of the methods ("options") inside the specified section
			// object which have the ConfigOption attribute.
			foreach (MemberInfo member in members) {
				object [] attrs = member.GetCustomAttributes (typeof (ConfigOption), false);
				if (attrs.Length > 0)
					options.Add (member.Name, ((ConfigOption) attrs [0]).Description);
			}

			return options;
		}

		public static bool InvokeOption (Section section, string option, string [] args, out string output)
		{
			MethodInfo method = section.GetType ().GetMethod (option);
			if (method == null)
				throw new Exception("No such method '" + option + "' for section '" + section + "'");

			object [] attrs = method.GetCustomAttributes (typeof (ConfigOption), false);
			if (attrs.Length == 0)
				throw new Exception ("Method '" + option + "' is not a configurable option");

			// Check the required number of parameters have been provided
			ConfigOption attr = (ConfigOption) attrs [0];
			if (attr.Params > 0 && args.Length < attr.Params)
				throw new Exception ("Option '" + option + "' requires " + attr.Params + " parameter(s): " + attr.ParamsDescription);

			// Can't pass a ref/out in a MethodInfo.Invoke() call so we box the
			// output in a single-element array.
			string [] boxoutput = new string[1];

			object [] methodparams = { boxoutput, args };
			bool result = (bool) method.Invoke (section, methodparams);
			output = boxoutput[0];

			// Mark the section as save-needed if we just changed something
			if (result && attr.IsMutator)
				section.SaveNeeded = true;
				
			return result;
		}

		[ConfigSection (Name="indexing")]
		public class IndexingConfig : Section {

			public IndexingConfig ()
			{
				roots = new ArrayList ();
				ignore_patterns = new ArrayList ();
				index_home_dir = true;
			}

			private ArrayList roots;
			public ArrayList Roots {
				get { return ArrayList.ReadOnly (roots); }
				set { roots = value; }
			}

			private bool index_home_dir;
			public bool IndexHomeDir {
				get { return index_home_dir; }
				set { index_home_dir = value; }
			}

			private ArrayList ignore_patterns;
			public ArrayList IgnorePatterns {
				get { return ArrayList.ReadOnly (ignore_patterns); }
				set { ignore_patterns = value; }
			}

			[ConfigOption (Description="List the indexing roots")]
			public bool ListRoots (string [] output, string [] args)
			{
				output [0] = "Current roots:\n";
				if (this.index_home_dir == true)
					output [0] += " - Your home directory\n";
				foreach (string root in roots)
					output [0] += " - " + root + "\n";

				return true;
			}

			[ConfigOption (Description="Toggles whether your home directory is to be indexed as a root", IsMutator=true)]
			public bool IndexHome (string [] output, string [] args)
			{
				output [0] = "Your home directory will ";
				if (index_home_dir)
					output [0] += "not ";
				output [0] += "be indexed";
				index_home_dir = !index_home_dir;
				return true;
			}

			[ConfigOption (Description="Add a root path to be indexed", Params=1, ParamsDescription="A path", IsMutator=true)]
			public bool AddRoot (string [] output, string [] args)
			{
				roots.Add (args [0]);
				output [0] = "Root added.";
				return true;
			}

			[ConfigOption (Description="Remove an indexing root", Params=1, ParamsDescription="A path", IsMutator=true)]
			public bool DelRoot (string [] output, string [] args)
			{
				roots.Remove (args [0]);
				output [0] = "Root removed.";
				return true;
			}

			[ConfigOption (Description="List user-specified filename patterns to be ignored")]
			public bool ListIgnorePatterns (string [] output, string [] args)
			{
				output [0] = "User-specified ignore patterns:\n";
				foreach (string pattern in ignore_patterns)
					output [0] += " - " + pattern + "\n";
				return true;
			}

			[ConfigOption (Description="Add a filename pattern to be ignored", Params=1, ParamsDescription="A pattern", IsMutator=true)]
			public bool AddIgnorePattern (string [] output, string [] args)
			{
				ignore_patterns.Add (args [0]);
				output [0] = "Pattern added.";
				return true;
			}

			[ConfigOption (Description="Remove an ignored filename pattern", Params=1, ParamsDescription="A pattern", IsMutator=true)]
			public bool DelIgnorePattern (string [] output, string [] args)
			{
				ignore_patterns.Remove (args [0]);
				output [0] = "Pattern removed.";
				return true;
			}

		}

		public class Section {
			[XmlIgnore]
			public bool SaveNeeded = false;
		}

		private class ConfigOption : Attribute {
			public string Description;
			public int Params;
			public string ParamsDescription;
			public bool IsMutator = false;
		}

		private class ConfigSection : Attribute {
			public string Name;
		}

	}



}
