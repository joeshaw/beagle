//
// ImLogViewer.cs
//
// Lukas Lipka <lukas@pmad.net>
// Raphael  Slinckx <rslinckx@gmail.com>
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
		[Widget] Dialog log_dialog;
		[Widget] TreeView timelinetree;
		[Widget] ScrolledWindow scrollwindow;
		[Widget] Label time_title;
		[Widget] Entry search_entry;
		[Widget] Label find_label;
		[Widget] TextView conversation;
		
		private TreeStore treeStore;
		private CellRendererText renderer;

		private Timeline timeline;

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
			} else {
				Console.WriteLine ("Log path doesn't exist - {0}", path);
				return;
			}

			timeline = new Timeline ();
			IndexLogs();
			
			if (speaking_to != null && speaking_to != "")
				ShowWindow (speaking_to);
		}

		private void SetTitle (DateTime dt)
		{
			if (dt.Ticks > 0)
				time_title.Markup = String.Format ("<b>{0}</b>",StringFu.DateTimeToPrettyString (dt));
		}
		
		Gtk.AccelGroup accel_group;
		GlobalKeybinder global_keys;

		private void ShowWindow (string speaker)
		{
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "log_dialog", null);
			gxml.Autoconnect (this);

			log_dialog.Response += OnWindowClose;

			// FIXME: Hide the find bar until further notice. We want highlighing and queries using Beagle
			search_entry.Visible = false;
			find_label.Visible = false;

			accel_group = new Gtk.AccelGroup ();
			log_dialog.AddAccelGroup (accel_group);
			global_keys = new GlobalKeybinder (accel_group);

			// Find the buddy
			GaimBuddyListReader list = new GaimBuddyListReader ();
			ImBuddy buddy = list.Search (speaker);
			
			if (buddy != null && buddy.Alias != "") {
				log_dialog.Title = String.Format ("Conversations with {0}", buddy.Alias);
			} else {
				if (speaker.EndsWith (".chat"))
					log_dialog.Title = String.Format ("Conversations in {0}", speaker.Replace (".chat",""));
				else
					log_dialog.Title = String.Format ("Conversation with {0}", speaker);
			}

			SetTitle (new DateTime ());

			// Close window (Ctrl-W)
			global_keys.AddAccelerator (new EventHandler (this.OnWindowClose),
						    (uint) Gdk.Key.w, 
						    Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			// Close window (Escape)
			global_keys.AddAccelerator (new EventHandler (this.OnWindowClose),
						    (uint) Gdk.Key.Escape, 
						    0,
						    Gtk.AccelFlags.Visible);

			conversation.PixelsAboveLines = 3;
			conversation.LeftMargin = 4;
			conversation.RightMargin = 4;

			TextTag boldtag = new TextTag ("bold");
			boldtag.Weight = Pango.Weight.Bold;
			conversation.Buffer.TagTable.Add (boldtag);

			TextTag highlight = new TextTag ("highlight");
			highlight.Background = "yellow";
			conversation.Buffer.TagTable.Add (highlight);

			treeStore = new TreeStore(new Type[] {typeof(string), typeof(string), typeof(object)});
			timelinetree.Model = this.treeStore;
			renderer = new CellRendererText();
			
			//FIXME: Hide the expanders in the timeline widget
			/*TreeViewColumn hidden = timelinetree.AppendColumn ("HiddenExpander", renderer , "text", 3);
			  hidden.Visible = false;
			  timelinetree.ExpanderColumn = hidden;*/
			
			timelinetree.AppendColumn ("Date", renderer , "markup", 0);
			timelinetree.AppendColumn ("Snippet", renderer , "text", 1);

			PopulateTimelineWidget ();

			timelinetree.Selection.Changed += OnConversationSelected; 
			search_entry.Changed += OnTypeAhead;

			if (have_first_selection_iter)
				timelinetree.Selection.SelectIter (first_selection_iter);
			
			Application.Run();
		}
		
		private bool LogContainsString (ImLog log, string text)
 		{
			string [] words = text.Split (null);
			
			//FIXME: This is quite crude and EXPENSIVE!
			foreach (string word in words)	{
				bool match = false;

				foreach (ImLog.Utterance utt in log.Utterances)	{
					if (utt.Text.ToLower ().IndexOf (word.ToLower ()) != -1) {
						match = true;
						break;
					}
				}
			
				if (!match) return false;
			}
									
			return true;
		}

		private void UpdateTimelineTree ()
		{
			//FIXME: Run this in a thread
			treeStore = new TreeStore(new Type[] {typeof(string), typeof(string), typeof(object)});
			timelinetree.Model = this.treeStore;
			PopulateTimelineWidget ();
		}

		private void PopulateTimelineWidget ()
		{
			TreeIter parent;
			
			if (timeline.Today.Count != 0) {
				parent = treeStore.AppendValues ("<b>Today</b>", "", null);
				PopulateTimeline (parent, timeline.Today, "HH:mm");
			}
			
			if (timeline.Yesterday.Count != 0) {
				parent = treeStore.AppendValues ("<b>Yesterday</b>", "", null);
				PopulateTimeline (parent, timeline.Yesterday, "HH:mm");
			}
			
			if (timeline.ThisWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Week</b>", "", null);
				PopulateTimeline (parent, timeline.ThisWeek, "dddd");
			}
		
			if (timeline.LastWeek.Count != 0) {
				parent = treeStore.AppendValues ("<b>Last Week</b>", "", null);
				PopulateTimeline (parent, timeline.LastWeek, "dddd");
			}
			
			if (timeline.ThisMonth.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Month</b>", "", null);
				PopulateTimeline (parent, timeline.ThisMonth, "MMM d");
			}
			
			if (timeline.ThisYear.Count != 0) {
				parent = treeStore.AppendValues ("<b>This Year</b>", "", null);
				PopulateTimeline (parent, timeline.ThisYear, "MMM d");
			}
			
			if (timeline.Older.Count != 0) {
				parent = treeStore.AppendValues ("<b>Older</b>", "", null);
				PopulateTimeline (parent, timeline.Older, "yyyy MMM d");
			}

			timelinetree.ExpandAll();
		}

		private void PopulateTimeline (TreeIter parent, ArrayList list, string dateformat)
		{
			foreach (ImLog log in list) {
				if (search_entry.Text != null && search_entry.Text != "")
					if (!LogContainsString (log, search_entry.Text))
						continue;
					
				string date_str = log.StartTime.ToString (dateformat);
				TreeIter iter;
				iter = treeStore.AppendValues (parent, date_str, log.EllipsizedSnippet, log);
				if (! have_first_selection_iter || log.LogFile == first_selected_log) {
					have_first_selection_iter = true;
					first_selection_iter = iter;
				}
			}
		}
		
		private void IndexLogs ()
		{
			foreach (string file in Directory.GetFiles (log_path)) {
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
			TextBuffer buffer = conversation.Buffer;
			buffer.Delete (buffer.StartIter, buffer.EndIter);
			
 			if (im_log == null) {
 				SetTitle (new DateTime ());
 				return;
 			}
 				
 			SetTitle (im_log.StartTime);
			TextTag bold = buffer.TagTable.Lookup ("bold");

 			foreach (ImLog.Utterance utt in im_log.Utterances) {
 				string who = utt.Who;
				
				buffer.InsertWithTags (buffer.EndIter, who + ":", new TextTag[] {bold});
				buffer.Insert (buffer.EndIter, String.Format(" {0}\n", utt.Text));
			}
		}

		private void OnWindowClose (object o, EventArgs args)
		{
			Application.Quit ();
		}

		private void OnTypeAhead (object o, EventArgs args)
		{
			UpdateTimelineTree ();
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
			if (args.Length > 0) {
				new GaimLogViewer (args [0]);
			} else {
				Console.WriteLine ("USAGE: beagle-imlogviewer " +
						   "/home/lukas/.gaim/logs/msn/joe@joe.com/jane@love.com");
			}
		}
	}
}
