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

using Beagle.Util;

namespace ImLogViewer {

	public class StoreLog : IComparable {
		public StoreLog () { }
		
		public DateTime Timestamp;
		public string Snippet;
		
		public ImLog ImLog;

		public int CompareTo (object obj)
		{
			StoreLog other = obj as StoreLog;
			if (other == null)
				return 1;

			// Sort into reverse chronological order
			return other.Timestamp.CompareTo (this.Timestamp);
		}
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

		private string log_path;
		private string first_selected_log;
		private bool have_first_selection_iter = false;
		private TreeIter first_selection_iter;

		private string speaking_to;
		
		public GaimLogViewer (string path) {
			
			if (Directory.Exists (path)) {
				log_path = path;
				first_selected_log = null;
			} else if (File.Exists (path)) {
				log_path = Path.GetDirectoryName (path);
				first_selected_log = path;
			}

			timeline = new Timeline ();
			IndexLogs();
			
			ShowWindow (speaking_to);
		}

		private void SetTitle (DateTime dt)
		{
			string str = "<b>Conversation with " + speaking_to;
			if (dt.Ticks > 0)
				str += ": " + StringFu.DateTimeToPrettyString (dt);
			str += "</b>";
			this.title.Markup = str;
		}
		
		private void ShowWindow (string speaker) {
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "window1", null);
			gxml.Autoconnect (this);

			gecko = new Gecko.WebControl();
			logwindow.AddWithViewport(gecko);
			gecko.RenderData("", "file:///tmp", "text/html");
			gecko.Show();
		
			this.treeStore = new TreeStore(new Type[] {typeof(string), typeof(string), typeof(object)});
			this.logsviewer.Model = this.treeStore;
			
			this.renderer = new CellRendererText();
			
			//FIXME: Hide the expanders in the timeline
			/*TreeViewColumn hidden = logsviewer.AppendColumn ("HiddenExpander", renderer , "text", 3);
			  hidden.Visible = false;
			  logsviewer.ExpanderColumn = hidden;*/
			
			logsviewer.AppendColumn ("Date", renderer , "markup", 0);
			logsviewer.AppendColumn ("Snippet", renderer , "text", 1);

			SetTitle (new DateTime ());

			// FIXME: We need to remove this widget from the glade
			// file.  Just hiding it is sort of silly.
			this.conversation.Hide ();
		
			populateLeftTree();

			logsviewer.ExpandAll();

			logsviewer.Selection.Changed += OnConversationSelected; 
			//search.Activated += OnSearchPressed;

			if (have_first_selection_iter)
				logsviewer.Selection.SelectIter (first_selection_iter);
			
			Application.Run();
		}
		
		private void populateLeftTree() {
			TreeIter parent;
			
			if (timeline.Today.Count != 0) {
				parent= treeStore.AppendValues ("<b>Today</b>", "", null);
				PopulateTimeline(parent, timeline.Today, "HH:mm");
			}
			
			if (timeline.Yesterday.Count != 0) {
				parent = treeStore.AppendValues ("<b>Yesterday</b>", "", null);
				PopulateTimeline(parent, timeline.Yesterday, "HH:mm");
			}
			
			if (timeline.ThisWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Week</b>", "", null);
				PopulateTimeline(parent, timeline.ThisWeek, "dddd");
			}
		
			if (timeline.LastWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>Last Week</b>", "", null);
				PopulateTimeline (parent, timeline.LastWeek, "dddd");
			}
			
			if (timeline.ThisMonth.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Month</b>", "", null);
				PopulateTimeline(parent, timeline.ThisMonth, "MMM d");
			}
			
			if (timeline.ThisYear.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Year</b>", "", null);
				PopulateTimeline (parent, timeline.ThisYear, "MMM d");
			}
			
			if (timeline.Older.Count != 0) {
				parent = treeStore.AppendValues ("<b>Older</b>", "", null);
				PopulateTimeline (parent, timeline.Older, "yyyy MMM d");
			}
		}

		private void PopulateTimeline (TreeIter parent, ArrayList list, string dateformat)
		{
			list.Sort ();
			foreach (StoreLog log in list) {
				TreeIter iter;
				iter = treeStore.AppendValues (parent, log.Timestamp.ToString (dateformat), log.Snippet, log);
				if (! have_first_selection_iter || log.ImLog.LogFile == first_selected_log) {
					have_first_selection_iter = true;
					first_selection_iter = iter;
				}
			}
		}
		
		public StoreLog ImLogParse (ImLog imlog)
		{
			StoreLog log = new StoreLog ();
			log.ImLog = imlog;
			log.Timestamp = imlog.StartTime;
			
			string snippet = "";
			
			foreach (ImLog.Utterance utt in imlog.Utterances) {
				if (snippet == null || snippet == "")
					snippet =  utt.Text;
				
				string[] words = utt.Text.Split (' ');
				
				if (words.Length > 3)  { snippet = utt.Text; break; }
			}

			log.Snippet = snippet;
			
			return log;
		}
		
		private void IndexLogs () {
		       	string [] files = Directory.GetFiles (log_path);
			
			foreach (string file in files) {

				// FIXME: gratuitous debug spew
				Console.WriteLine (file);

				ICollection logs = GaimLog.ScanLog (new FileInfo (file));
				
				foreach (ImLog gaimlog in logs) {

					if (speaking_to == null)
						speaking_to = gaimlog.SpeakingTo;

					StoreLog log = ImLogParse (gaimlog);
					timeline.Add (log, log.Timestamp);
				}
			}
		}
		
		private void RenderConversation (ImLog im_log)
		{
			if (im_log == null) {
				SetTitle (new DateTime ());
				gecko.RenderData ("", "file:///tmp/FIXME", "text/html");
				return;
			}
				
			SetTitle (im_log.StartTime);

			StringBuilder html = new StringBuilder ();

			foreach (ImLog.Utterance utt in im_log.Utterances) {
				//FIXME: We strip tags here!
				html.Append ("<p><b>" + utt.Who + ":</b>&nbsp;" + utt.Text + "</p>\n");
			}
				
			gecko.RenderData(html.ToString (), "file://"+im_log.LogFile, "text/html");
			
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
				StoreLog log = model.GetValue (iter, 2) as StoreLog;
				if (log != null)
					RenderConversation (log.ImLog);
				else
					RenderConversation (null);
			}
		}
		
		public static void Main (string[] args)
		{
			if (args.Length > 0)
			{
				GeckoUtils.Init ();
				GeckoUtils.SetSystemFonts ();

				new GaimLogViewer (args [0]);
			}
			else
				Console.WriteLine ("USAGE: beagle-imlogviewer" +
						   "/home/lukas/.gaim/logs/msn/lipkalukas@hotmail.com/bbulik@hotmail.com");
		}
	}
}
