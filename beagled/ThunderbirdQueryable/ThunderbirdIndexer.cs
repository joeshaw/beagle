//
// ThunderbirdIndexer.cs: This class launches IndexableGenerators and makes sure instant-updates work
//
// Copyright (C) 2006 Pierre Östlund
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
using System.IO;
using System.Collections;
using System.Reflection;

using Beagle.Util;
using Beagle.Daemon;
using TB = Beagle.Util.Thunderbird;

namespace Beagle.Daemon.ThunderbirdQueryable {

	public class ThunderbirdIndexer {
		private ThunderbirdQueryable queryable;

		private bool init_phase, first_lap;
		private string[] root_paths;
		private Hashtable supported_types;
		private ArrayList account_list;
		private ThunderbirdInotify inotify;
	
		public delegate void NotificationEventHandler (object o, NotificationEventArgs args);
	
		public ThunderbirdIndexer (ThunderbirdQueryable queryable, string[] root_paths)
		{
			this.queryable = queryable;
			this.root_paths = root_paths;
			this.supported_types = new Hashtable ();
			this.init_phase = true;
			this.first_lap = true;
			this.account_list = new ArrayList ();
			this.inotify = new ThunderbirdInotify ();
			
			LoadSupportedTypes ();
			
			foreach (string path in root_paths) {
				Inotify.Subscribe (path, OnInotifyEvent, 
					Inotify.EventType.Delete | 
					Inotify.EventType.MovedTo | 
					Inotify.EventType.Modify |
					Inotify.EventType.Create);
			}
			
			inotify.InotifyEvent += OnInotifyEvent;
		}
		
		// Loads all supported types, checks if they have a correct constructor and is enabled
		private void LoadSupportedTypes ()
		{
			Assembly assembly = Assembly.GetCallingAssembly ();
			
			foreach (Type type in ReflectionFu.ScanAssemblyForInterface (assembly, typeof (IIndexableGenerator))) {
			
				foreach (ThunderbirdIndexableGeneratorAttribute attr in 
					ReflectionFu.ScanTypeForAttribute (type, typeof (ThunderbirdIndexableGeneratorAttribute))) {

					foreach (ConstructorInfo constructor in type.GetConstructors ()) {
					
						ParameterInfo[] parameters = constructor.GetParameters ();
						if(parameters.Length != 3)
							continue;
						
						if (parameters [0].ParameterType.Equals (typeof (ThunderbirdIndexer)) &&
							parameters [1].ParameterType.Equals (typeof (TB.Account)) &&
							parameters [2].ParameterType.Equals (typeof (string))) {
							
							// Make sure we should enable this type
							if (attr.Enabled)
								supported_types [attr.Type]  = type;
							
						} else
							Logger.Log.Debug ("{0} has an invalid constructor!", type.ToString ());
					}
				}
			}	
		}
		
		public void Crawl ()
		{
			int launched = 0;
			TB.AccountReader reader = null;
			
			foreach (string path in root_paths) {
				try {
					reader = new TB.AccountReader (path);
				} catch (Exception e) {
					Logger.Log.Warn (e, "Failed to load accounts: {0}", e.Message);
					continue;
				}
				
				foreach (TB.Account account in reader) {
					if (Shutdown.ShutdownRequested)
						return;
					
					if (supported_types [account.Type] == null)
						continue;
					
					IndexAccount (account);
					launched++;
				}
			}
			
			init_phase = false;
			Logger.Log.Info ("Indexing {0} ({1}) Thunderbird account(s) spread over {2} profile(s)", 
				launched, account_list.Count, root_paths.Length);
			
			// Clean out old stuff in case no IndexableGenerator was launched
			if (launched == 0)
				ChildComplete ();
		}
		
		public void IndexAccount (TB.Account account)
		{
			TB.Account stored_account = GetParentAccount (account.Path);

			// We need to act upon changes made to accounts during Thunderbird runtime.
			// The user might change from plain to SSL, which leads to a new port number
			// that has to be taken in account for when indexing.
			if (stored_account == null && Directory.Exists (account.Path) && supported_types [account.Type] != null) {
				account_list.Add (account);
				IndexDirectory (account.Path);
				//Logger.Log.Info ("Indexing {0} account {1}", account.Type.ToString (), account.Server);
			
			} else if (stored_account == null && File.Exists (account.Path) && supported_types [account.Type] != null) {
				account_list.Add (account);
				IndexFile (account.Path);
				//Logger.Log.Info ("Indexing {0} account {1}", account.Type.ToString (), account.Server);
				
			} else if (stored_account != null &&
				(stored_account.Server != account.Server ||
				stored_account.Port != account.Port ||
				stored_account.Type != account.Type ||
				stored_account.Delimiter != account.Delimiter)) {

				account_list.Remove (stored_account);
				account_list.Add (account);
				
				// Make sure all running indexables are aware of this since it can affect the way they index
				NotificationEventArgs args;
				args = new NotificationEventArgs (NotificationType.UpdateAccountInformation, stored_account);
				args.Data = (object) account;
				OnNotification (args);
				
				Logger.Log.Info ("Updated {0} with new account details", account.Server);
			}
		}
		
		public void IndexFile (string file)
		{
			TB.Account account = GetParentAccount (file);
			
			if (account == null || supported_types [account.Type] == null || Thunderbird.GetFileSize (file) < 1)
				return;
			
			object[] param = new object[] {this, account, file};
			ThunderbirdIndexableGenerator generator = Activator.CreateInstance (
				(Type) supported_types [account.Type], param) as ThunderbirdIndexableGenerator;
			
			AddIIndexableTask (generator, file);
		}
		
		private void IndexDirectory (string directory)
		{
			Queue pending = new Queue ();
			
			pending.Enqueue (directory);
			while (pending.Count > 0) {
				string dir = pending.Dequeue () as string;
				
				foreach (string subdir in DirectoryWalker.GetDirectories (dir)) {
					if (Shutdown.ShutdownRequested)
						return;
						
					pending.Enqueue (subdir);
				}

				if (Inotify.Enabled) {
						inotify.Watch (dir, 
						Inotify.EventType.Modify | 
						Inotify.EventType.Create |
						Inotify.EventType.Delete |
						Inotify.EventType.MovedFrom |
						Inotify.EventType.MovedTo);
				}
				
				foreach (string file in DirectoryWalker.GetItems 
					(dir, new DirectoryWalker.FileFilter (Thunderbird.IsMorkFile))) {
					if (Shutdown.ShutdownRequested)
						return;
					
					IndexFile (file);
				}
			}
		}
		
		public void RemoveAccount (TB.Account account)
		{
			TB.Account acc = GetParentAccount (account.Path);
			
			if (acc == null)
				return;
			
			ScheduleRemoval (Property.NewKeyword ("fixme:account", acc.Server), Scheduler.Priority.Delayed);
			OnNotification (new NotificationEventArgs (NotificationType.StopIndexing, account));
			account_list.Remove (acc);
		}

		private void AddIIndexableTask (IIndexableGenerator generator, string tag)
		{
			if (queryable.ThisScheduler.ContainsByTag (tag)) {
				Logger.Log.Debug ("Not adding a Task for already running: {0}", tag);
				return;
			}

			Scheduler.Task task = queryable.NewAddTask (generator);
			task.Tag = tag;
			queryable.ThisScheduler.Add (task);
		}
		
		private void ScheduleRemoval (Property prop, Scheduler.Priority priority)
		{
			if (queryable.ThisScheduler.ContainsByTag (prop.ToString ())) {
				Logger.Log.Debug ("Not adding a Task for already running: {0}", prop.ToString ());
				return;
			}
			
			Scheduler.Task task = queryable.NewRemoveByPropertyTask (prop);
			task.Priority = priority;
			task.SubPriority = 0;
			queryable.ThisScheduler.Add (task);
		}

		public void ScheduleRemoval (Uri[] uris, string tag, Scheduler.Priority priority)
		{
			if (queryable.ThisScheduler.ContainsByTag (tag)) {
				Logger.Log.Debug ("Not adding a Task for already running: {0}", tag);
				return;
			}

			Scheduler.Task task = queryable.NewAddTask (new UriRemovalIndexableGenerator (uris));
			task.Priority = priority;
			task.SubPriority = 0;
			queryable.ThisScheduler.Add (task);
		}
		
		public void UpdateAccounts (string root_path)
		{
			TB.AccountReader new_accounts = null;

			try {
				new_accounts = new TB.AccountReader (root_path);
			} catch (Exception e) {
				Logger.Log.Warn ("Failed when reading Thunderbird accounts: {0}, account may have been added or removed", e);
				return;
			}

			// Add all accounts again to make sure things are updated the way they should
			foreach (TB.Account account in new_accounts)
				IndexAccount (account);

			// Remove non-existing accounts
			foreach (TB.Account existing in account_list) {
				bool found = false;
			
				foreach (TB.Account new_account in new_accounts) {
					if (existing.Path == new_account.Path) {
						found = true;
						break;
					}
				}

				if (!found) 
						RemoveAccount (existing);
			}
		}
		
		public TB.Account GetParentAccount (string directory)
		{
			foreach (TB.Account acc in account_list) {
				if (directory.StartsWith (acc.Path))
					return acc;
			}
			
			return null;
		}

		private void OnInotifyEvent (Inotify.Watch watch,
						string path,
						string subitem,
						string srcpath,
						Inotify.EventType type)
		{
			if (subitem == null)
				return;
				
			string full_path = Path.Combine (path, subitem);
			
			// If prefs.js is deleted... then we have nothing at all to index
			if (((type & Inotify.EventType.MovedTo) != 0 && srcpath == Path.Combine (path, "prefs.js")) ||
				((type & Inotify.EventType.Delete) != 0 && subitem == "prefs.js")) {
				
				foreach (TB.Account account in account_list)
					RemoveAccount (account);
				return;
			}
			
			// Update in case an account was removed or added
			// Thunderbird saves prefs.js with a different name and then replacing the old one
			// by "moving" it over the existing prefs.js. That's why MoveTo is used as inotfy type.
			if ((((type & Inotify.EventType.Modify) != 0 || (type & Inotify.EventType.MovedTo) != 0 || 
				(type & Inotify.EventType.Create) != 0) && subitem == "prefs.js")) {
				
				UpdateAccounts (path);
				return;
			}
			
			// In case the address book file have been moved or deleted, we have to stop indexing it
			if (((type & Inotify.EventType.MovedTo) != 0 && srcpath == Path.Combine (path, "abook.mab")) ||
				((type & Inotify.EventType.Delete) != 0 && subitem == "abook.mab")) {
				
				TB.Account account = GetParentAccount (full_path);
				
				if (account != null)					
					RemoveAccount (account);
					
				return;
			}
			
			// In case of a newly created addressbook, the current address book is modified or an old 
			// address book is moved to where the address book can be found: either start indexing 
			// or restart an already indexing IndeaxbleGenerator.
			if ((((type & Inotify.EventType.Modify) != 0 || (type & Inotify.EventType.MovedTo) != 0 || 
				(type & Inotify.EventType.Create) != 0) && subitem == "abook.mab")) {
				
				TB.Account account = GetParentAccount (full_path);
				
				if (account == null && File.Exists (full_path)) {
					UpdateAccounts (path);
					return;
				} else if (account == null)
					return;

				// Tell any running indexable about this or start a new one
				if (queryable.ThisScheduler.ContainsByTag (full_path))
					OnNotification (new NotificationEventArgs (NotificationType.RestartIndexing, account));
				else
					IndexFile (full_path);
				
				return;
			}
			
			// Re-index files when needed
			if ((type & Inotify.EventType.Modify) != 0) {
				TB.Account account = GetParentAccount (full_path);
			
				if (account == null || !Thunderbird.IsMorkFile (path, subitem))
					return;
				
				// In case we have a running IndexableGenerator, tell it that we have a file that needs to 
				// be re-indexed.
				if (queryable.ThisScheduler.ContainsByTag (full_path))
					OnNotification (new NotificationEventArgs (NotificationType.RestartIndexing, account));
				else
					IndexFile (full_path);
					
				return;
			}
			
			// Index newly created directories
			if ((type & Inotify.EventType.Create) != 0 && (type & Inotify.EventType.IsDirectory) != 0) {
				if (GetParentAccount (full_path) != null && Inotify.Enabled)
					Inotify.Subscribe (full_path, OnInotifyEvent, Inotify.EventType.All);
					
				return;
			}
		}

		public void ChildComplete ()
		{
			if (NotificationEvent != null || init_phase || !first_lap)
				return;
			
			if (Thunderbird.Debug)
				Logger.Log.Debug ("Removing old Thunderbird objects");

			Scheduler.Task task = queryable.NewRemoveTaskByDate (ThunderbirdQueryable.IndexingStart);
			task.Priority = Scheduler.Priority.Idle;
			task.Tag = "RemoveOldThunderbirdMails";
			queryable.ThisScheduler.Add (task);
			
			// This makes sure that ChildComplete will only clean out all mails once in a lifetime
			// (of the Thunderbird backend that is)
			first_lap = false;
		}

		protected virtual void OnNotification(NotificationEventArgs args)
		{
			if (NotificationEvent != null)
				NotificationEvent (this, args);
		}

		public event NotificationEventHandler NotificationEvent;
		
		public LuceneAccess Lucene {
			get { return queryable.Lucene; }
		}
	}
	
	/////////////////////////////////////////////////////////////////////////////////////
	
	public enum NotificationType {
		StopIndexing,
		RestartIndexing,
		UpdateAccountInformation
	}
	
	/////////////////////////////////////////////////////////////////////////////////////
	
	public class NotificationEventArgs  : EventArgs
	{
		private NotificationType type;
		private TB.Account account;
		private object data;

		public NotificationEventArgs (NotificationType type, TB.Account account)
		{
			this.type = type;
			this.account = account;
		}
		
		public NotificationType Type {
			get { return type; }
		}
		
		public TB.Account Account {
			get { return account; }
		}
		
		public object Data {
			get { return data; }
			set { data =value; }
		}
	}
	
}
