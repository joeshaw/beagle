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
using System.Collections.Specialized;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Serialization;
using System.Text.RegularExpressions;
using Mono.Unix;

using Beagle.Util;

namespace Beagle.Util {

	public class Conf {

		// No instantiation
		private Conf () { }

		public static Hashtable Sections;
		
		public static IndexingConfig Indexing = null;
		public static DaemonConfig Daemon = null;
		public static SearchingConfig Searching = null;
		public static NetworkingConfig Networking = null;

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

			LoadFile (typeof (NetworkingConfig), Networking, out temp, force);
		    	Networking = (NetworkingConfig) temp;
			NotifySubscribers (Networking);
			
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
				Logger.Log.Error (e, "Could not load configuration from {0}:", filename);
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
				XmlFu.SerializeUtf8 (serializer, fs, section);
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
			
			private KeyBinding show_search_window_binding = new KeyBinding ("F12");
			public KeyBinding ShowSearchWindowBinding {
				get { return show_search_window_binding; }
				set { show_search_window_binding = value; }
			}

			// BeagleSearch window position and dimension
			// stored as percentage of screen co-ordinates
			// to deal with change of resolution problem - hints from tberman

			private float beagle_search_pos_x = 0;
			public float BeaglePosX {
				get { return beagle_search_pos_x; }
				set { beagle_search_pos_x = value; }
			}
			
			private float beagle_search_pos_y = 0;
			public float BeaglePosY {
				get { return beagle_search_pos_y; }
				set { beagle_search_pos_y = value; }
			}
			
			private float beagle_search_width = 0; 
			public float BeagleSearchWidth {
				get { return beagle_search_width; }
				set { beagle_search_width = value; }
			}

			private float beagle_search_height = 0;
			public float BeagleSearchHeight {
				get { return beagle_search_height; }
				set { beagle_search_height = value; }
			}

			// ah!We want a Queue but Queue doesnt serialize *easily*
			private ArrayList search_history = new ArrayList ();
			public ArrayList SearchHistory {
				get { return search_history; }
				set { search_history = value; }
			}

			private bool beagle_search_auto_search = true;
			public bool BeagleSearchAutoSearch {
				get { return beagle_search_auto_search; }
				set { beagle_search_auto_search = value; }
			}

		}

		[ConfigSection (Name="daemon")]
		public class DaemonConfig : Section {
			private ArrayList static_queryables = new ArrayList ();
			public ArrayList StaticQueryables {
				get { return static_queryables; }
				set { static_queryables = value; }
			}

			// By default, every backend is allowed.
			// Only maintain a list of denied backends.
			private ArrayList denied_backends = new ArrayList ();
			public ArrayList DeniedBackends {
				get { return denied_backends; }
				set { denied_backends = value; }
			}

			private bool allow_static_backend = false; // by default, false
			public bool AllowStaticBackend {
				get { return allow_static_backend; }
				// Don't really want to expose this, but serialization requires it
				set { allow_static_backend = value; }
			}

			private bool index_synchronization = true;
			public bool IndexSynchronization {
				get { return index_synchronization; }
				// Don't really want to expose this, but serialization requires it
				set { index_synchronization = value; }
			}

			[ConfigOption (Description="Enable a backend", Params=1, ParamsDescription="Name of the backend to enable")]
			internal bool AllowBackend (out string output, string [] args)
			{
				denied_backends.Remove (args [0]);
				output = "Backend allowed (need to restart beagled for changes to take effect).";
				return true;
			}

			[ConfigOption (Description="Disable a backend", Params=1, ParamsDescription="Name of the backend to disable")]
			internal bool DenyBackend (out string output, string [] args)
			{
				denied_backends.Add (args [0]);
				output = "Backend disabled (need to restart beagled for changes to take effect).";
				return true;
			}
			
			private bool allow_root = false;
			public bool AllowRoot {
				get { return allow_root; }
				set { allow_root = value; }
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

			[ConfigOption (Description="Toggles whether static indexes will be enabled")]
			internal bool ToggleAllowStaticBackend (out string output, string [] args)
			{
				allow_static_backend = !allow_static_backend;
				output = "Static indexes are " + ((allow_static_backend) ? "enabled" : "disabled") + " (need to restart beagled for changes to take effect).";
				return true;
			}		

			[ConfigOption (Description="Toggles whether your indexes will be synchronized locally if your home directory is on a network device (eg. NFS/Samba)")]
			internal bool ToggleIndexSynchronization (out string output, string [] args)
			{
				index_synchronization = !index_synchronization;
				output = "Index Synchronization is " + ((index_synchronization) ? "enabled" : "disabled") + ".";
				return true;
			}

			[ConfigOption (Description="Toggles whether Beagle can be run as root")]
			internal bool ToggleAllowRoot (out string output, string [] args)
			{
				allow_root = ! allow_root;
				if (allow_root)
					output = "Beagle is now permitted to run as root";
				else
					output = "Beagle is no longer permitted to run as root";
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
				get { return roots; }
				set { roots = value; }
			}

			private bool index_home_dir = true;
			public bool IndexHomeDir {
				get { return index_home_dir; }
				set { index_home_dir = value; }
			}

			private bool index_on_battery = false;
			public bool IndexOnBattery {
				get { return index_on_battery; }
				set { index_on_battery = value; }
			}

			private bool index_faster_on_screensaver = true;
			public bool IndexFasterOnScreensaver {
				get { return index_faster_on_screensaver; }
				set { index_faster_on_screensaver = value; }
			}

			private ArrayList excludes = new ArrayList ();
			[XmlArray]
			[XmlArrayItem (ElementName="ExcludeItem", Type=typeof(ExcludeItem))]
			public ArrayList Excludes {
				get { return excludes; }
				set { excludes = value; }
			}

			public struct Maildir {
				public string Directory;
				public string Extension;
			}

			private ArrayList maildirs = new ArrayList ();
			[XmlArray]
			[XmlArrayItem (ElementName="Maildir", Type=typeof(Maildir))]
			public ArrayList Maildirs {
				get { return maildirs; }
				set { maildirs = value; }
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

			[ConfigOption (Description="Toggles whether any data should be indexed if the system is on battery")]
			internal bool IndexWhileOnBattery (out string output, string [] args)
			{
				if (index_on_battery)
					output = "Data will not be indexed while on battery.";
				else
					output = "Data will be indexed while on battery.";
				index_on_battery = !index_on_battery;
				return true;
			}

			[ConfigOption (Description="Toggles whether to index faster while the screensaver is on")]
			internal bool FasterOnScreensaver (out string output, string [] args)
			{
				if (index_faster_on_screensaver)
					output = "Data will be indexed normally while on screensaver.";
				else
					output = "Data will be indexed faster while on screensaver.";
				index_faster_on_screensaver = !index_faster_on_screensaver;
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
				} catch {
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
				} catch {
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

			[ConfigOption (Description="Add a directory containing maildir emails. Use this when beagle is unable to determine the mimetype of files in this directory as message/rfc822",
				       Params=2,
				       ParamsDescription="path to the directory, extension (use * for any extension)")]
			internal bool AddMaildir (out string output, string[] args)
			{
				Maildir maildir = new Maildir ();
				maildir.Directory = args [0];
				maildir.Extension = ((args [1] == null || args [1] == String.Empty) ? "*" : args [1]);
				maildirs.Add (maildir);

				output = String.Format ("Added maildir directory: {0} with extension '{1}'", maildir.Directory, maildir.Extension);
				return true;
			}

			[ConfigOption (Description="Remove a directory from ListMaildirs",
				       Params=2,
				       ParamsDescription="path to the directory, extension")]
			internal bool DelMaildir (out string output, string[] args)
			{
				args [1] = ((args [1] == null || args [1] == String.Empty) ? "*" : args [1]);

				int count = -1;
				foreach (Maildir maildir in maildirs) {
					count ++;
					if (maildir.Directory == args [0] && maildir.Extension == args [1])
						break;
				}

				if (count != -1 && count != maildirs.Count) {
					maildirs.RemoveAt (count);
					output = "Maildir removed.";
					return true;
				}

				output = "Could not find requested maildir to remove.";
				return false;
			}

			[ConfigOption (Description="List user specified maildir directories", IsMutator=false)]
			internal bool ListMaildirs (out string output, string [] args)
			{
				output = "User-specified maildir directories:\n";
				foreach (Maildir maildir in maildirs)
					output += String.Format (" - {0} with extension '{1}'\n", maildir.Directory, maildir.Extension);
				return true;
			}

		}

		[ConfigSection (Name="networking")]
		public class NetworkingConfig : Section 
		{
			// Index sharing service is disabled by default
			private bool service_enabled = false;

			// Password protect our local indexes
			private bool password_required = true;

			// The name and password for the local network service
			private string service_name = String.Format ("{0} ({1})", UnixEnvironment.UserName, UnixEnvironment.MachineName);
			private string service_password = String.Empty;

			// This is a list of registered and paired nodes which
			// the local client can search
			private ArrayList network_services = new ArrayList ();
			
			public bool ServiceEnabled {
				get { return service_enabled; }
				set { service_enabled = value; }
			}

			public bool PasswordRequired {
				get { return password_required; }
				set { password_required = value; }
			}

			public string ServiceName {
				get { return service_name; }
				set { service_name = value; }
			}

			public string ServicePassword {
				get { return service_password; }
				set { service_password = value; }
			}

			[ConfigOption (Description="Toggles whether searching over network will be enabled the next time the daemon starts.")]
			internal bool NetworkSearch (out string output, string [] args)
			{
				if (service_enabled)
					output = "Network search will be disabled.";
				else
					output = "Network search will be enabled.";
				service_enabled = !service_enabled;
				return true;
			}

			[XmlArray]
			[XmlArrayItem (ElementName="NetworkService", Type=typeof (NetworkService))]
			public ArrayList NetworkServices {
				get { return network_services; }
				set { network_services = value; }
			}

			[ConfigOption (Description="List available network services for querying", IsMutator=false)]
			internal bool ListNetworkServices (out string output, string [] args)
			{
				output = "Currently registered network services:\n";

				foreach (NetworkService service in network_services)
					output += " - " + service.ToString () + "\n";

#if ENABLE_AVAHI
				output += "\n";
				output += "Available network services:\n";
				
				try {
				
				AvahiBrowser browser = new AvahiBrowser ();
				//browser.Start ();

				foreach (NetworkService service in browser.GetServicesBlocking ())
					output += " - " + service.ToString () + "\n";

				browser.Dispose ();

				} catch (Exception e) {
					Console.WriteLine ("Cannot connect to avahi service: " + e.Message);
				}
#endif

				return true;
			}

			[ConfigOption (Description="Add a network service for querying", Params=2, ParamsDescription="name, hostname:port")]
			internal bool AddNetworkService (out string output, string [] args)
			{
				string name = args [0];
				string uri = args [1];
				
				if (uri.Split (':').Length < 2)
					uri = uri.Trim() + ":4000";
				
				NetworkService service = new NetworkService (name, new Uri (uri), false, null);
				network_services.Add (service);
				
				output = "Network service '" + service + "' added";

				return true;
			}
			
			[ConfigOption (Description="Remove a network service from querying", Params=1, ParamsDescription="name")]
			internal bool RemoveNetworkService (out string output, string [] args)
			{
				string name = args[0];

				foreach (NetworkService service in network_services) {
					if (service.Name != name)
						continue;

					network_services.Remove (service);
					output = "Network service '" + service.Name + "' removed";
					
					return true;
				}

				output = "Network service '" + name + "' not found in registered services";

				return false;
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
