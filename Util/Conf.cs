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
using System.Text.RegularExpressions;

using Beagle.Util;

namespace Beagle.Util {

	public class Conf {

		// No instantiation
		private Conf () { }

		public static Hashtable Sections;
		
		public static IndexingConfig Indexing = null;
		public static DaemonConfig Daemon = null;
		public static SearchingConfig Searching = null;

//#if ENABLE_WEBSERVICES		
		public static NetworkingConfig Networking = null;
		public static WebServicesConfig WebServices = null;
//#endif 		
		private static string configs_dir;
		private static Hashtable mtimes;
		private static Hashtable subscriptions;

		private static bool watching_for_updates;
		private static bool update_watch_present;

		private static BindingFlags method_search_flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.InvokeMethod;

		public delegate void ConfigUpdateHandler (Section section);

		static Conf ()
		{
			Sections = new Hashtable (3);
			mtimes = new Hashtable (3);
			subscriptions = new Hashtable (3);

			configs_dir = Path.Combine (PathFinder.StorageDir, "config");
			if (!Directory.Exists (configs_dir))
				Directory.CreateDirectory (configs_dir);

			Conf.Load ();
		}

		public static void WatchForUpdates ()
		{
			// Make sure we don't try and watch for updates more than once
			if (update_watch_present)
				return;

			if (Inotify.Enabled) {
				Inotify.Subscribe (configs_dir, OnInotifyEvent, Inotify.EventType.Create | Inotify.EventType.CloseWrite);
			} else {
				// Poll for updates every 60 secs
				GLib.Timeout.Add (60000, new GLib.TimeoutHandler (CheckForUpdates));
			}

			update_watch_present = true;
		}

		private static void OnInotifyEvent (Inotify.Watch watch, string path, string subitem, string srcpath, Inotify.EventType type)
		{
			if (subitem == "" || watching_for_updates == false)
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

			// FIXME: Yeah
			LoadFile (typeof (IndexingConfig), Indexing, out temp, force);
			Indexing = (IndexingConfig) temp;
			NotifySubscribers (Indexing);

			LoadFile (typeof (DaemonConfig), Daemon, out temp, force);
			Daemon = (DaemonConfig) temp;
			NotifySubscribers (Daemon);

			LoadFile (typeof (SearchingConfig), Searching, out temp, force);
		        Searching = (SearchingConfig) temp;
			NotifySubscribers (Searching);

//#if ENABLE_WEBSERVICES
			LoadFile (typeof (NetworkingConfig), Networking, out temp, force);
		    	Networking = (NetworkingConfig) temp;
			NotifySubscribers (Networking);
			
			LoadFile (typeof (WebServicesConfig), WebServices, out temp, force);
		    	WebServices = (WebServicesConfig) temp;
			NotifySubscribers (WebServices);
//#endif

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
				throw new ConfigException ("Could not find ConfigSection attribute on " + type);

			string sectionname = ((ConfigSection) attrs [0]).Name;
			string filename = sectionname + ".xml";
			string filepath = Path.Combine (configs_dir, filename);
			if (!File.Exists (filepath)) {
				if (current == null)
					ConstructDefaultSection (type, sectionname, out section);
				return false;
			}

			if (!force && current != null && mtimes.ContainsKey (sectionname) &&
					File.GetLastWriteTimeUtc (filepath).CompareTo ((DateTime) mtimes [sectionname]) <= 0)
				return false;

			Logger.Log.Debug ("Loading {0} from {1}", type, filename);
			FileStream fs = null;

			try {
				fs = File.Open (filepath, FileMode.Open, FileAccess.Read, FileShare.Read);
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
				throw new ConfigException ("Could not find ConfigSection attribute on " + type);

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
			MemberInfo [] members = section.GetType ().GetMembers (method_search_flags);

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
			MethodInfo method = section.GetType ().GetMethod (option, method_search_flags);
			if (method == null) {
				string msg = String.Format ("No such method '{0}' for section '{1}'", option, section);
				throw new ConfigException(msg);
			}
			object [] attrs = method.GetCustomAttributes (typeof (ConfigOption), false);
			if (attrs.Length == 0) {
				string msg = String.Format ("Method '{0}' is not a configurable option", option);
				throw new ConfigException (msg);
			}

			// Check the required number of parameters have been provided
			ConfigOption attr = (ConfigOption) attrs [0];
			if (attr.Params > 0 && args.Length < attr.Params) {
				string msg = String.Format ("Option '{0}' requires {1} parameter(s): {2}", option, attr.Params, attr.ParamsDescription);
				throw new ConfigException (msg);
			}

			object [] methodparams = { null, args };
			bool result = (bool) method.Invoke (section, methodparams);
			output = (string) methodparams [0];

			// Mark the section as save-needed if we just changed something
			if (result && attr.IsMutator)
				section.SaveNeeded = true;

			return result;
		}

		[ConfigSection (Name="searching")]
		public class SearchingConfig : Section {
			
			private bool autostart = true;
			public bool Autostart {
				get { return autostart; }
				set { autostart = value; }
			}
			
			private KeyBinding show_search_window_binding = new KeyBinding ("F12");
			public KeyBinding ShowSearchWindowBinding {
				get { return show_search_window_binding; }
				set { show_search_window_binding = value; }
			}
		}

		[ConfigSection (Name="daemon")]
		public class DaemonConfig : Section {
			private ArrayList static_queryables = new ArrayList ();
			public ArrayList StaticQueryables {
				get { return static_queryables; }
				set { static_queryables = value; }
			}

			private ArrayList allowed_backends = new ArrayList ();
			public ArrayList AllowedBackends {
				get { return allowed_backends; }
				set { allowed_backends = value; }
			}

			private ArrayList denied_backends = new ArrayList ();
			public ArrayList DeniedBackends {
				get { return denied_backends; }
				set { denied_backends = value; }
			}

			private bool index_synchronization = true;
			public bool IndexSynchronization {
				get { return index_synchronization; }
				// Don't really want to expose this, but serialization requires it
				set { index_synchronization = value; }
			}

			[ConfigOption (Description="Add a static queryable", Params=1, ParamsDescription="Index path")]
			internal bool AddStaticQueryable (out string output, string [] args)
			{
				static_queryables.Add (args [0]);
				output = "Static queryable added.";
				return true;
			}

			[ConfigOption (Description="Remove a static queryable", Params=1, ParamsDescription="Index path")]
			internal bool DelStaticQueryable (out string output, string [] args)
			{
				static_queryables.Remove (args [0]);
				output = "Static queryable removed.";
				return true;
			}
			
			[ConfigOption (Description="List user-specified static queryables", IsMutator=false)]
			internal bool ListStaticQueryables (out string output, string [] args)
			{
				output = "User-specified static queryables:\n";
				foreach (string index_path in static_queryables)
					output += String.Format (" - {0}\n", index_path);
				return true;
			}

			[ConfigOption (Description="Toggles whether your indexes will be synchronized locally if your home directory is on a network device (eg. NFS/Samba)")]
			internal bool ToggleIndexSynchronization (out string output, string [] args)
			{
				index_synchronization = !index_synchronization;
				output = "Index Synchronization is " + ((index_synchronization) ? "enabled" : "disabled") + ".";
				return true;
			}		
		}

		[ConfigSection (Name="indexing")]
		public class IndexingConfig : Section 
		{
			private ArrayList roots = new ArrayList ();
			[XmlArray]
			[XmlArrayItem(ElementName="Root", Type=typeof(string))]
			public ArrayList Roots {
				get { return ArrayList.ReadOnly (roots); }
				set { roots = value; }
			}

			private bool index_home_dir = true;
			public bool IndexHomeDir {
				get { return index_home_dir; }
				set { index_home_dir = value; }
			}

			private ArrayList excludes = new ArrayList ();
			[XmlArray]
			[XmlArrayItem (ElementName="ExcludeItem", Type=typeof(ExcludeItem))]
			public ArrayList Excludes {
				get { return ArrayList.ReadOnly (excludes); }
				set { excludes = value; }
			}

			[ConfigOption (Description="List the indexing roots", IsMutator=false)]
			internal bool ListRoots (out string output, string [] args)
			{
				output = "Current roots:\n";
				if (this.index_home_dir == true)
					output += " - Your home directory\n";
				foreach (string root in roots)
					output += " - " + root + "\n";

				return true;
			}

			[ConfigOption (Description="Toggles whether your home directory is to be indexed as a root")]
			internal bool IndexHome (out string output, string [] args)
			{
				if (index_home_dir)
					output = "Your home directory will not be indexed.";
				else
					output = "Your home directory will be indexed.";
				index_home_dir = !index_home_dir;
				return true;
			}

			[ConfigOption (Description="Add a root path to be indexed", Params=1, ParamsDescription="A path")]
			internal bool AddRoot (out string output, string [] args)
			{
				roots.Add (args [0]);
				output = "Root added.";
				return true;
			}

			[ConfigOption (Description="Remove an indexing root", Params=1, ParamsDescription="A path")]
			internal bool DelRoot (out string output, string [] args)
			{
				roots.Remove (args [0]);
				output = "Root removed.";
				return true;
			}
			
			[ConfigOption (Description="List user-specified resources to be excluded from indexing", IsMutator=false)]
			internal bool ListExcludes (out string output, string [] args)
			{
				output = "User-specified resources to be excluded from indexing:\n";
				foreach (ExcludeItem exclude_item in excludes)
					output += String.Format (" - [{0}] {1}\n", exclude_item.Type.ToString (), exclude_item.Value);
				return true;
			}

			[ConfigOption (Description="Add a resource to exclude from indexing", Params=2, ParamsDescription="A type [path/pattern/mailfolder], a path/pattern/name")]
			internal bool AddExclude (out string output, string [] args)
			{
				ExcludeType type;
				try {
					type = (ExcludeType) Enum.Parse (typeof (ExcludeType), args [0], true);
				} catch (Exception e) {
					output = String.Format("Invalid type '{0}'. Valid types: Path, Pattern, MailFolder", args [0]);
					return false;
				}

				excludes.Add (new ExcludeItem (type, args [1]));
				output = "Exclude added.";
				return true;
			}

			[ConfigOption (Description="Remove an excluded resource", Params=2, ParamsDescription="A type [path/pattern/mailfolder], a path/pattern/name")]
			internal bool DelExclude (out string output, string [] args)
			{
				ExcludeType type;
				try {
					type = (ExcludeType) Enum.Parse (typeof (ExcludeType), args [0], true);
				} catch (Exception e) {
					output = String.Format("Invalid type '{0}'. Valid types: Path, Pattern, MailFolder", args [0]);
					return false;
				}

				foreach (ExcludeItem item in excludes) {
					if (item.Type != type || item.Value != args [1])
						continue;
					excludes.Remove (item);
					output = "Exclude removed.";
					return true;
				}

				output = "Could not find requested exclude to remove.";
				return false;
			}

		}

//#if ENABLE_WEBSERVICES
		[ConfigSection (Name="webservices")]
		public class WebServicesConfig: Section 
		{
			private ArrayList publicFolders = new ArrayList ();
			[XmlArray]
			[XmlArrayItem(ElementName="PublicFolders", Type=typeof(string))]
			public ArrayList PublicFolders {
				get { return ArrayList.ReadOnly (publicFolders); }
				set { publicFolders = value; }
			}

			private bool allowGlobalAccess = true;
			public bool AllowGlobalAccess {
				get { return allowGlobalAccess; }
				set { allowGlobalAccess = value; }
			}

			[ConfigOption (Description="List the public folders", IsMutator=false)]
			internal bool ListPublicFolders(out string output, string [] args)
			{
				output = "Current list of public folders:\n";

				foreach (string pf in publicFolders)
					output += " - " + pf + "\n";

				return true;
			}
			
			[ConfigOption (Description="Check current configuration of global access to Beagle web-services", IsMutator=false)]
			internal bool CheckGlobalAccess(out string output, string [] args)
			{
				if (allowGlobalAccess)
					output = "Global Access to Beagle WebServices is currently ENABLED.";
				else
					output = "Global Access to Beagle WebServices is currently DISABLED.";

				return true;
			}
			
			[ConfigOption (Description="Enable/Disable global access to Beagle web-services")]
			internal bool SwitchGlobalAccess (out string output, string [] args)
			{
				allowGlobalAccess = !allowGlobalAccess;			
				
				if (allowGlobalAccess)
					output = "Global Access to Beagle WebServices now ENABLED.";
				else
					output = "Global Access to Beagle WebServices now DISABLED.";

				return true;
			}

			[ConfigOption (Description="Add public web-service access to a folder", Params=1, ParamsDescription="A path")]
			internal bool AddPublicFolder (out string output, string [] args)
			{
				publicFolders.Add (args [0]);					
				output = "PublicFolder " + args[0] + " added.";
				return true;
			}

			[ConfigOption (Description="Remove public web-service access to a folder", Params=1, ParamsDescription="A path")]
			internal bool DelPublicFolder (out string output, string [] args)
			{
				publicFolders.Remove (args [0]);		
				output = "PublicFolder " + args[0] + " removed.";
				return true;
			}			
		}

		[ConfigSection (Name="networking")]
		public class NetworkingConfig: Section 
		{
			private ArrayList netBeagleNodes = new ArrayList ();
			
			[XmlArray]
			[XmlArrayItem(ElementName="NetBeagleNodes", Type=typeof(string))]
			public ArrayList NetBeagleNodes {
				get { return ArrayList.ReadOnly (netBeagleNodes); }
				set { netBeagleNodes = value; }
			}

			[ConfigOption (Description="List Networked Beagle Daemons to query", IsMutator=false)]
			internal bool ListBeagleNodes (out string output, string [] args)
			{
				output = "Current list of Networked Beagle Daemons to query:\n";

				foreach (string nb in netBeagleNodes)
					output += " - " + nb + "\n";
				
				return true;
			}

			[ConfigOption (Description="Add a Networked Beagle Daemon to query", Params=1, ParamsDescription="HostName:PortNo")]
			internal bool AddBeagleNode (out string output, string [] args)
			{
				string node = args[0];
				
				if (((string[])node.Split(':')).Length < 2)
					node = args [0].Trim() + ":8888";
							
				netBeagleNodes.Add(node);			
				output = "Networked Beagle Daemon \"" + node +"\" added.";
				return true;
			}

			[ConfigOption (Description="Remove a configured Networked Beagle Daemon", Params=1, ParamsDescription="HostName:PortNo")]
			internal bool DelBeagleNode (out string output, string [] args)
			{
				string node = args[0];
				
				if (((string[])node.Split(':')).Length < 2)
					node = args [0].Trim() + ":8888";
							
				netBeagleNodes.Remove(node);					
				output = "Networked Beagle Daemon \"" + node +"\" removed.";
				return true;
			}
		}
//#endif

		public class Section {
			[XmlIgnore]
			public bool SaveNeeded = false;
		}

		private class ConfigOption : Attribute {
			public string Description;
			public int Params;
			public string ParamsDescription;
			public bool IsMutator = true;
		}

		private class ConfigSection : Attribute {
			public string Name;
		}

		public class ConfigException : Exception {
			public ConfigException (string msg) : base (msg) { }
		}

	}

	//////////////////////////////////////////////////////////////////////
	
	public enum ExcludeType {
		Path,
		Pattern,
		MailFolder
	}

	public class ExcludeItem {

		private ExcludeType type;
		private string val;

		[XmlAttribute]
		public ExcludeType Type {
			get { return type; }
			set { type = value; }
		}

		private string exactMatch;
		private string prefix;
		private string suffix;
		private Regex  regex;

		[XmlAttribute]
		public string Value {
			get { return val; }
			set {
				switch (type) {
				case ExcludeType.Path:
				case ExcludeType.MailFolder:
					prefix = value;
					break;

				case ExcludeType.Pattern:
					if (value.StartsWith ("/") && value.EndsWith ("/")) {
						regex = new Regex (value.Substring (1, value.Length - 2));
						break;
					}
					
					int i = value.IndexOf ('*');
					if (i == -1) {
						exactMatch = value;
					} else {
						if (i > 0)
							prefix = value.Substring (0, i);
						if (i < value.Length-1)
							suffix = value.Substring (i+1);
					}
					break;
				}

				val = value;
			}
		}

		public ExcludeItem () {}

		public ExcludeItem (ExcludeType type, string value) {
			this.Type = type;
			this.Value = value;
		}
		
		public bool IsMatch (string param) 
		{
			switch (Type) {
			case ExcludeType.Path:
			case ExcludeType.MailFolder:
				if (prefix != null && ! param.StartsWith (prefix))
					return false;

				return true;

			case ExcludeType.Pattern:
				if (exactMatch != null)
					return param == exactMatch;
				if (prefix != null && ! param.StartsWith (prefix))
					return false;
				if (suffix != null && ! param.EndsWith (suffix))
					return false;
				if (regex != null && ! regex.IsMatch (param))
					return false;

				return true;
			}

			return false;
		}

		public override bool Equals (object obj) 
		{
			ExcludeItem exclude = obj as ExcludeItem;
			return (exclude != null && exclude.Type == type && exclude.Value == val);
		}

		public override int GetHashCode ()
		{
			return (this.Value.GetHashCode () ^ (int) this.Type);
		}

	}

	//////////////////////////////////////////////////////////////////////
	
	public class KeyBinding {
		public string Key;
		
		[XmlAttribute]
		public bool Ctrl = false;
		[XmlAttribute]
		public bool Alt = false;
		
		public KeyBinding () {}
		public KeyBinding (string key) : this (key, false, false) {}
		
		public KeyBinding (string key, bool ctrl, bool alt) 
		{
			Key = key;
			Ctrl = ctrl;
			Alt = alt;
		}
		
		public override string ToString ()
		{
			string result = "";
			
			if (Ctrl)
				result += "<Ctrl>";
			if (Alt)
				result += "<Alt>";
			
			result += Key;
			
			return result;
		}
		
		public string ToReadableString ()
		{
			return ToString ().Replace (">", "-").Replace ("<", "");
		}
	}
}
