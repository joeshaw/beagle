//
// ImLogViewer.cs
//
// Lukas Lipka <lukas@pmad.net>
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;
using Gtk;
using Glade;
using System.Text;
using System.Xml;

using BU = Beagle.Util;

namespace ImLogViewer {

	public class StoreLog
	{
		public StoreLog () { }
		
		public string File;
		public DateTime Timestamp;
		public string Snippet;
		
		public BU.GaimLog ImLog;
	}
	
	public class GaimLogViewer {
		
		[Widget] TreeView logsviewer;
		[Widget] ScrolledWindow logwindow;
		[Widget] Button   searchbutton;
		[Widget] Label    title;
		[Widget] Label    conversation;
		[Widget] Entry    search;
		
		private Timeline timeline;
		
		private TreeStore treeStore;
		private CellRendererText renderer;

		private Gecko.WebControl gecko;
		
		private string logsDir;
		private string logPath;
		
		public GaimLogViewer (string path) {
			logsDir = Path.GetDirectoryName (path);
			logPath = path;

			timeline = new Timeline ();
			IndexLogs();
			string speaker = extractSpeaker(logsDir);
			
			ShowWindow (speaker);
		}
		
		private void ShowWindow (string speaker) {
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "window1", null);
			gxml.Autoconnect (this);
			
			this.treeStore = new TreeStore(new Type[] {typeof(string), typeof(string), typeof(string), typeof(string)});
			this.logsviewer.Model = this.treeStore;
			
			this.renderer = new CellRendererText();
			
			//FIXME: Hide the expanders in the timeline
			/*TreeViewColumn hidden = logsviewer.AppendColumn ("HiddenExpander", renderer , "text", 3);
			  hidden.Visible = false;
			  logsviewer.ExpanderColumn = hidden;*/
			
			logsviewer.AppendColumn ("Date", renderer , "markup", 0);
			logsviewer.AppendColumn ("Snippet", renderer , "text", 1);
			
			populateLeftTree();
			
			this.title.Markup = "<b>Conversation with " + speaker + "</b>";
			this.conversation.Text = "";
			
			logsviewer.ExpandAll();
			
			gecko = new Gecko.WebControl();
			logwindow.AddWithViewport(gecko);
			gecko.RenderData("", "file:///tmp", "text/html");
			gecko.Show();
		
			this.conversation.Markup = "";
		
			logsviewer.Selection.Changed += OnConversationSelected; 
			//search.Activated += OnSearchPressed;
			
			if (File.Exists (logPath))
				ShowConversation (logPath);

			Application.Run();
		}
		
		private void populateLeftTree() {
			TreeIter parent;
			
			if (timeline.Today.Count != 0) {
				parent= treeStore.AppendValues ("<b>Today</b>", "", "", "");
				PopulateTimeline(parent, timeline.Today, "HH:mm");
			}
			
			if (timeline.Yesterday.Count != 0) {
				parent = treeStore.AppendValues ("<b>Yesterday</b>", "", "", "");
				PopulateTimeline(parent, timeline.Yesterday, "HH:mm");
			}
			
			if (timeline.ThisWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Week</b>", "", "", "");
				PopulateTimeline(parent, timeline.ThisWeek, "dddd");
			}
		
			if (timeline.LastWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>Last Week</b>", "", "", "");
				PopulateTimeline (parent, timeline.LastWeek, "dddd");
			}
			
			if (timeline.ThisMonth.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Month</b>", "", "", "");
				PopulateTimeline(parent, timeline.ThisMonth, "MMM d");
			}
			
			if (timeline.ThisYear.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Year</b>", "", "", "");
				PopulateTimeline (parent, timeline.ThisYear, "MMM d");
			}
			
			if (timeline.Older.Count != 0) {
				parent = treeStore.AppendValues ("<b>Older</b>", "", "", "");
				PopulateTimeline (parent, timeline.Older, "yyyy MMM d");
			}
		}

		private void PopulateTimeline (TreeIter parent, ArrayList list, string dateformat)
		{
			foreach (StoreLog log in list)
				treeStore.AppendValues (parent, log.Timestamp.ToString (dateformat) , log.Snippet, log.File, "");
		}
		
		private string extractSpeaker (string logDir)
		{
			//FIXME: Get properly the directory name
			//Or search for the real name of the speaker instead of adress
			int i = logDir.Substring(0, logDir.Length -1).LastIndexOf("/");
			return logDir.Substring(i+1);
		}
		
		public StoreLog ImLogParse (BU.ImLog imlog)
		{
			StoreLog log = new StoreLog ();
			log.File = imlog.LogFile;
			log.Timestamp = imlog.StartTime;
			
			string snippet = "";
			
			foreach (BU.ImLog.Utterance utt in imlog.Utterances) {
				if (snippet == null || snippet == "")
					snippet =  utt.Text;
				
				string[] words = utt.Text.Split (' ');
				
				if (words.Length > 3)  { snippet = utt.Text; break; }
			}

			log.Snippet = snippet;
			
			return log;
		}
		
		private void IndexLogs () {
		       	string [] files = Directory.GetFiles (logsDir);
			
			foreach (string file in files) {
				ICollection logs = BU.GaimLog.ScanLog (new FileInfo (file));
				
				foreach (BU.ImLog gaimlog in logs) {
					StoreLog log = ImLogParse (gaimlog);
					timeline.Add (log, log.Timestamp);
				}
			}
		}
		
		private void ShowConversation (string path)
		{
			RenderConversation (path, "FIXME: Date");
		}

		private void RenderConversation (string file, string date)
		{
			conversation.Markup = "<b>Conversation " + date  + "</b>";
			string html = "";

			ICollection logs = BU.GaimLog.ScanLog (new FileInfo (file));
			
			foreach (BU.ImLog gaimlog in logs)
			{
				foreach (BU.ImLog.Utterance utt in gaimlog.Utterances) {
					//FIXME: We strip tags here!
					html += "<p><b>" + utt.Who + ":</b>&nbsp;" + utt.Text + "</p>\n";
				}
				
			}
				
			gecko.RenderData(html, "file://"+file, "text/html");
			
		}
		
		private void OnWindowDeleteEvent (object o, DeleteEventArgs args)
		{
			Application.Quit ();
			args.RetVal = true;
		}
		
		private void OnConversationSelected (object o, EventArgs args) 
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter)) {
				string log = (string)model.GetValue (iter, 2);
				if (log != "")
					RenderConversation (log, (string)model.GetValue (iter, 0));
			}
		}
		
		public static void Main (string[] args)
		{
			if (args.Length > 0)
			{
				BU.GeckoUtils.Init ();
				BU.GeckoUtils.SetSystemFonts ();

				new GaimLogViewer (args [0]);
			}
			else
				Console.WriteLine ("USAGE: beagle-imlogviewer" +
						   "/home/lukas/.gaim/logs/msn/lipkalukas@hotmail.com/bbulik@hotmail.com");
		}
	}
}
