//
// MonodocQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
//
// Authors:
//   Fredrik Hedberg (fredrik.hedberg@avafan.com)
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
using System.Xml;
using System.Text;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

using ICSharpCode.SharpZipLib.Zip;

namespace Beagle.Daemon.MonodocQueryable {

	[QueryableFlavor (Name="Monodoc", Domain=QueryDomain.Local, RequireInotify=false)]
	public class MonodocQueryable : LuceneQueryable {

		private static Logger log = Logger.Get ("MonodocQueryable");

		string monodoc_dir;
		int monodoc_wd;

		public MonodocQueryable () : base ("MonodocIndex")
		{
			monodoc_dir = "/usr/lib/monodoc/sources"; // FIXME Make use of autoconf
		}

		/////////////////////////////////////////////

		public override void Start () 
		{			
			if (! (Directory.Exists (monodoc_dir)))
				return;

			base.Start ();

			ExceptionHandlingThread.Start (new ThreadStart (StartWorker));
		}
		
		private void StartWorker () 
		{
			log.Info ("Scanning Monodoc sources");
			Stopwatch timer = new Stopwatch ();
			timer.Start ();

			int foundSources = 0;
			int foundTypes = 0;

			DirectoryInfo root = new DirectoryInfo (monodoc_dir);

			if (Inotify.Enabled) {
				monodoc_wd = Inotify.Watch (root.FullName, Inotify.EventType.CloseWrite | Inotify.EventType.CreateFile);
				Inotify.Event += OnInotifyEvent;
			} else {
				FileSystemWatcher fsw = new FileSystemWatcher ();
				fsw.Path = monodoc_dir;
				fsw.Filter = "*.zip";

				fsw.Changed += new FileSystemEventHandler (OnChangedEvent);
				fsw.Created += new FileSystemEventHandler (OnChangedEvent);

				fsw.EnableRaisingEvents = true;
			}

			foreach (FileInfo file in root.GetFiles ("*.zip")) {
 				int result = IndexArchive (file, Scheduler.Priority.Delayed);
				if (result != -1) {
					foundSources++;
					foundTypes += result;
				}
			}

			timer.Stop ();
			log.Info ("Found {0} types in {1} Monodoc sources in {2}", foundTypes, foundSources, timer);
		}

		/////////////////////////////////////////////

		private void OnInotifyEvent (int wd,
					     string path,
					     string subitem,
					     string srcpath,
					     Inotify.EventType type)
		{
			if (wd != monodoc_wd)
				return;

			if (subitem == "")
				return;

			if (Path.GetExtension (subitem) != ".zip")
				return;

			string full_path = Path.Combine (path, subitem);

			switch (type) {
			     case Inotify.EventType.CloseWrite:
			     case Inotify.EventType.CreateFile:
				IndexArchive (new FileInfo (full_path), Scheduler.Priority.Delayed);
				break;
			}
		}

		private void OnChangedEvent (object o, FileSystemEventArgs args)
		{
			IndexArchive (new FileInfo (args.FullPath), Scheduler.Priority.Delayed);
		}

		/////////////////////////////////////////////

		int IndexArchive (FileInfo file, Scheduler.Priority priority)
		{
                        if (this.FileAttributesStore.IsUpToDate (file.FullName))
                                return -1;

			log.Debug ("Scanning Monodoc source file " + file);

			Scheduler.TaskGroup group = NewMarkingTaskGroup (file.FullName, file.LastWriteTime);
			
			int countTypes = 0;			
			ZipFile archive = new ZipFile (file.ToString());
			
			foreach (ZipEntry entry in archive)
			{
				if (entry.Name.IndexOf (".") != -1)
					continue;

				XmlDocument document = new XmlDocument ();
				document.Load (archive.GetInputStream (entry));
			
				XmlNode type = document.SelectSingleNode ("/Type");

				if (type == null)
					continue;

				Indexable typeIndexable = TypeNodeToIndexable(type,file);
				
				Scheduler.Task typeTask = NewAddTask (typeIndexable);
				typeTask.Priority = priority;
				typeTask.SubPriority = 0;
				typeTask.AddTaskGroup (group);
				ThisScheduler.Add (typeTask);

				foreach(XmlNode member in type.SelectNodes("Members/Member"))
				{
					Indexable memberIndexable = MemberNodeToIndexable(
						member,
						file,
						type.Attributes["FullName"].Value);

					Scheduler.Task memberTask = NewAddTask (memberIndexable);
					memberTask.Priority = priority;
					memberTask.SubPriority = 0;
					memberTask.AddTaskGroup (group);
					ThisScheduler.Add (memberTask);
				}
				countTypes++;
			}

			return countTypes;
		}

		Indexable TypeNodeToIndexable(XmlNode node,FileInfo file)
		{
			Indexable indexable = new Indexable(
				new Uri ("monodoc:///" + file + ";item=T:"+node.Attributes["FullName"].Value));

			indexable.MimeType = "application/monodoc";
			indexable.Type = "Monodoc";

			indexable.AddProperty (Property.NewKeyword ("fixme:type", "type"));
			indexable.AddProperty (Property.NewKeyword ("fixme:name", "T:" + node.Attributes["FullName"].Value));

			string splitname = String.Join (" ", 
							StringFu.FuzzySplit (node.Attributes["FullName"].Value.ToString ()));
			indexable.AddProperty (Property.NewKeyword ("fixme:splitname",splitname));
			
			// Should we add other stuff here? Implemented interfaces etc?

			StringReader reader = new StringReader (node.SelectSingleNode ("Docs").InnerXml); 
                        indexable.SetTextReader (reader);

			return indexable;
		}
		
		Indexable MemberNodeToIndexable(XmlNode node, FileInfo file, string parentName)
		{
			char memberType = MemberTypeToChar (node.SelectSingleNode ("MemberType").InnerText);
			StringBuilder memberFullName = new StringBuilder ();
	
			memberFullName.Append (memberType + ":"+ parentName);

			if (memberType != 'C')
				memberFullName.Append ("." + node.Attributes["MemberName"].Value);

			if (memberType == 'C' || memberType == 'M' || memberType == 'E')
			{	
				memberFullName.Append ("(");
				bool inside = false;

				foreach (XmlNode parameter in node.SelectNodes ("Parameters/Parameter"))
				{	
					if (!inside) inside = true; else memberFullName.Append(",");
					memberFullName.Append (parameter.Attributes["Type"].Value);
				}

				memberFullName.Append (")");
			}

			Indexable indexable = new Indexable (
				new Uri ("monodoc:///" + file + ";item=" + memberFullName));

			indexable.MimeType = "application/monodoc";
			indexable.Type = "Monodoc";

			indexable.AddProperty (
				Property.NewKeyword ("fixme:type", node.SelectSingleNode ("MemberType").InnerText.ToLower ()));
			indexable.AddProperty (
				Property.New ("fixme:name",memberFullName));

			int indexHack = memberFullName.ToString ().IndexOf ("(");
			string splitname;
			
			if (indexHack == -1)
				splitname = String.Join (" ", StringFu.FuzzySplit (memberFullName.ToString ().Substring (2)));
			else 
				splitname = String.Join (" ", StringFu.FuzzySplit (memberFullName.ToString ().Substring(2,indexHack-2)));
			
			indexable.AddProperty (
				Property.NewKeyword ("fixme:splitname",splitname));
			
			StringReader reader = new StringReader (node.SelectSingleNode ("Docs").InnerXml); 
                        indexable.SetTextReader (reader);

			return indexable;			
		}

		char MemberTypeToChar (string memberType)
		{
			switch (memberType) {
			case "Constructor":
				return 'C';
			case "Event":
				return 'E';
			case "Property":
				return 'P';
			case "Field":
				return 'F';
			case "Method":
				return 'M';
			default:
				return 'U';
			}
		}
	}
}
