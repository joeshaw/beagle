//
// ImLogViewer.cs
//
// Lukas Lipka <lukas@pmad.net>
//
// Copyright (C) 2005 Novell, Inc.
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

	public class GaimLogViewer {
		[Widget] Window window1;
		[Widget] TreeView logsviewer;
		[Widget] ScrolledWindow logwindow;
		[Widget] Button   searchbutton;
		[Widget] Label    title;
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
		
		Gtk.AccelGroup accel_group;
		GlobalKeybinder global_keys;

		private void ShowWindow (string speaker) {
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "window1", null);
			gxml.Autoconnect (this);

			accel_group = new Gtk.AccelGroup ();
			window1.AddAccelGroup (accel_group);
			global_keys = new GlobalKeybinder (accel_group);


			// Close window (Ctrl-W)
			global_keys.AddAccelerator (new EventHandler (this.HideWindow),
						    (uint) Gdk.Key.w, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Close window (Escape)
			global_keys.AddAccelerator (new EventHandler (this.HideWindow),
						    (uint) Gdk.Key.Escape, 
						    0,
						    Gtk.AccelFlags.Visible);

			gecko = new Gecko.WebControl();
			logwindow.AddWithViewport(gecko);
			gecko.RenderData("", "file:///tmp", "text/html");
			gecko.Show();
		
			this.treeStore = new TreeStore(new Type[] {typeof(string), typeof(string), typeof(object)});
			this.logsviewer.Model = this.treeStore;
			
			this.renderer = new CellRendererText();
			
			//FIXME: Hide the expanders in the timeline widget
			/*TreeViewColumn hidden = logsviewer.AppendColumn ("HiddenExpander", renderer , "text", 3);
			  hidden.Visible = false;
			  logsviewer.ExpanderColumn = hidden;*/
			
			logsviewer.AppendColumn ("Date", renderer , "markup", 0);
			logsviewer.AppendColumn ("Snippet", renderer , "text", 1);

			SetTitle (new DateTime ());

			PopulateTimelineWidget ();

			logsviewer.ExpandAll();

			logsviewer.Selection.Changed += OnConversationSelected; 
			//search.Activated += OnSearchPressed;

			if (have_first_selection_iter)
				logsviewer.Selection.SelectIter (first_selection_iter);
			
			Application.Run();
		}
		
		private void PopulateTimelineWidget ()
		{
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

		private class ReverseLogComparer : IComparer {
			
			public int Compare (object x, object y)
			{
				return ((ImLog) y).StartTime.CompareTo (((ImLog) x).StartTime);
			}
		}

		static ReverseLogComparer rev_cmp = new ReverseLogComparer ();

		private void PopulateTimeline (TreeIter parent, ArrayList list, string dateformat)
		{
			list.Sort (rev_cmp);
			foreach (ImLog log in list) {
				string date_str = log.StartTime.ToString (dateformat);
				TreeIter iter;
				iter = treeStore.AppendValues (parent, date_str, log.EllipsizedSnippet, log);
				if (! have_first_selection_iter || log.LogFile == first_selected_log) {
					have_first_selection_iter = true;
					first_selection_iter = iter;
				}
			}
		}
		
		private void IndexLogs () {
		       	string [] files = Directory.GetFiles (log_path);
			
			foreach (string file in files) {


				ICollection logs = GaimLog.ScanLog (new FileInfo (file));
				
				foreach (ImLog log in logs) {

					if (speaking_to == null)
						speaking_to = log.SpeakingTo;

					timeline.Add (log, log.StartTime);
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
				//FIXME: We strip html tags here!
				string who = utt.Who;
				who = who.Replace (" ", "&nbsp;");
				html.Append ("<p><b>" + who + ":</b> " + utt.Text + "</p>\n");
			}
				
			gecko.RenderData(html.ToString (), StringFu.PathToQuotedFileUri (im_log.LogFile), "text/html");
			
		}

		private void HideWindow (object o, EventArgs args)
		{
			Application.Quit ();
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
				ImLog log = model.GetValue (iter, 2) as ImLog;
				RenderConversation (log);
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
				Console.WriteLine ("USAGE: beagle-imlogviewer " +
						   "/home/lukas/.gaim/logs/msn/lipkalukas@hotmail.com/bbulik@hotmail.com");
		}
	}
}
