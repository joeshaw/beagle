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

using Mono.Posix;

namespace ImLogViewer {

	public class ImLogWindow {
		[Widget] Dialog log_dialog;
		[Widget] TreeView timelinetree;
		[Widget] Label time_title;
		[Widget] Entry search_entry;
		[Widget] Button search_button;
		[Widget] Button clear_button;
		[Widget] TextView conversation;
		
		private TreeStore tree_store;
		private Timeline timeline;

		private string log_path;
		private string first_selected_log;
		private bool have_first_selection_iter = false;
		private TreeIter first_selection_iter;
		private string speaking_to;

		private string highlight_text;
		private string search_text;

		public ImLogWindow (string path, string search, string highlight)
		{
			if (Directory.Exists (path)) {
				log_path = path;
				first_selected_log = null;
			} else if (File.Exists (path)) {
				log_path = Path.GetDirectoryName (path);
				first_selected_log = path;
				highlight_text = highlight;
			} else {
				Console.WriteLine ("ERROR: Log path doesn't exist - {0}", path);
				return;
			}

			timeline = new Timeline ();

			IndexLogs();

			if (speaking_to != null && speaking_to != "")
				ShowWindow (speaking_to);
		}

		private void SetStatusTitle (DateTime dt)
		{
			time_title.Markup = String.Format ("<b>{0}</b>", StringFu.DateTimeToPrettyString (dt));
		}
		
		private void BindKeys ()
		{
			Gtk.AccelGroup accel_group = new Gtk.AccelGroup ();
			GlobalKeybinder global_keys = new GlobalKeybinder (accel_group);

			global_keys.AddAccelerator (new EventHandler (this.OnWindowClose),
						    (uint) Gdk.Key.w, Gdk.ModifierType.ControlMask,
						    Gtk.AccelFlags.Visible);

			global_keys.AddAccelerator (new EventHandler (this.OnWindowClose),
						    (uint) Gdk.Key.Escape, 0,
						    Gtk.AccelFlags.Visible);

			log_dialog.AddAccelGroup (accel_group);
		}

		private void ShowWindow (string speaker)
		{
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "log_dialog", null);
			gxml.Autoconnect (this);

			BindKeys ();

			// Find the buddy
			ImBuddy buddy = new GaimBuddyListReader ().Search (speaker);

			if (speaker.EndsWith (".chat")) {
				log_dialog.Title = String.Format (Catalog.GetString ("Conversations in {0}"), speaker.Replace (".chat", ""));
			} else {
				string nick = speaker;

				if (buddy != null && buddy.Alias != "")
					nick = buddy.Alias;

				log_dialog.Title = String.Format (Catalog.GetString ("Conversations with {0}"), nick);
			}
								  
			conversation.PixelsAboveLines = 3;
			conversation.LeftMargin = 4;
			conversation.RightMargin = 4;

			TextTag boldtag = new TextTag ("bold");
			boldtag.Weight = Pango.Weight.Bold;
			conversation.Buffer.TagTable.Add (boldtag);

			TextTag highlight = new TextTag ("highlight");
			highlight.Background = "yellow";
			conversation.Buffer.TagTable.Add (highlight);

			tree_store = new TreeStore (new Type[] {typeof (string), typeof (string), typeof (object)});

			timelinetree.Model = tree_store;
			timelinetree.AppendColumn ("Date", new CellRendererText(), "markup", 0);
			timelinetree.AppendColumn ("Snippet", new CellRendererText(), "text", 1);
			timelinetree.Selection.Changed += OnConversationSelected; 

			PopulateTimelineWidget ();

			search_entry.Activated += OnSearchClicked;
			search_button.Clicked += OnSearchClicked;
			clear_button.Clicked += OnClearClicked;

			log_dialog.Response += new ResponseHandler (OnWindowResponse);

			if (have_first_selection_iter)
				timelinetree.Selection.SelectIter (first_selection_iter);
			
			Application.Run();
		}
		
		private bool LogContainsString (ImLog log, string text)
 		{
			string [] words = text.Split (null);
			
			//FIXME: This is very crude and EXPENSIVE!
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
			tree_store.Clear ();
			PopulateTimelineWidget ();
		}

		private void PopulateTimelineWidget ()
		{
			TreeIter parent;
			
			if (timeline.Today.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("Today")), "", null);
				PopulateTimeline (parent, timeline.Today, Catalog.GetString ("HH:mm"));
			}
			
			if (timeline.Yesterday.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("Yesterday")), "", null);
				PopulateTimeline (parent, timeline.Yesterday, Catalog.GetString ("HH:mm"));
			}
			
			if (timeline.ThisWeek.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("This Week")), "", null);
				PopulateTimeline (parent, timeline.ThisWeek, Catalog.GetString ("dddd"));
			}
		
			if (timeline.LastWeek.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("Last Week")), "", null);
				PopulateTimeline (parent, timeline.LastWeek, Catalog.GetString ("dddd"));
			}
			
			if (timeline.ThisMonth.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("This Month")), "", null);
				PopulateTimeline (parent, timeline.ThisMonth, Catalog.GetString ("MMM d"));
			}
			
			if (timeline.ThisYear.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("This Year")), "", null);
				PopulateTimeline (parent, timeline.ThisYear, Catalog.GetString ("MMM d"));
			}
			
			if (timeline.Older.Count != 0) {
				parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString ("Older")), "", null);
				PopulateTimeline (parent, timeline.Older, Catalog.GetString ("yyyy MMM d"));
			}

			timelinetree.ExpandAll();
		}

		private void PopulateTimeline (TreeIter parent, ArrayList list, string dateformat)
		{
			foreach (ImLog log in list) {
				if (search_text != null && search_text != "")
					if (! LogContainsString (log, search_text))
						continue;
					
				string date_str = log.StartTime.ToString (dateformat);
				TreeIter iter;
				iter = tree_store.AppendValues (parent, date_str, log.EllipsizedSnippet, log);
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
 				//SetStatusTitle (new DateTime ());
 				return;
 			}
 				
 			SetStatusTitle (im_log.StartTime);

			TextTag bold = buffer.TagTable.Lookup ("bold");

 			foreach (ImLog.Utterance utt in im_log.Utterances) {
				buffer.InsertWithTags (buffer.EndIter, utt.Who + ":", new TextTag[] {bold});
				buffer.Insert (buffer.EndIter, String.Format(" {0}\n", utt.Text));
			}

			if (highlight_text != null)
				HighlightSearchTerms (highlight_text);

			if (search_text != null && search_text != "")
				HighlightSearchTerms (search_text);
		}

		private void HighlightSearchTerms (string highlight)
		{
			TextBuffer buffer = conversation.Buffer;
			string note_text = buffer.GetText (buffer.StartIter, buffer.EndIter, false);

			string[] words = highlight.Split (' ');

			note_text = note_text.ToLower ();

			foreach (string word in words) {
				int idx = 0;
				bool this_word_found = false;

				if (word == String.Empty)
					continue;

				while (true) {					
					idx = note_text.IndexOf (word.ToLower (), idx);

					if (idx == -1) {
						if (this_word_found)
							break;
						else
							return;
					}

					this_word_found = true;

					Gtk.TextIter start = buffer.GetIterAtOffset (idx);
					Gtk.TextIter end = start;
					end.ForwardChars (word.Length);

					buffer.ApplyTag ("highlight", start, end);

					idx += word.Length;
				}
			}
		}

		private void OnWindowClose (object o, EventArgs args)
		{
			Application.Quit ();
		}

		private void OnWindowResponse (object o, ResponseArgs args)
		{
			Application.Quit ();
		}

		private void OnSearchClicked (object o, EventArgs args)
		{
			search_text = search_entry.Text;
			search_button.Visible = false;
			clear_button.Visible = true;
			search_entry.Sensitive = false;

			UpdateTimelineTree ();
		}
		
		private void OnClearClicked (object o, EventArgs args)
		{
			search_text = null;
			search_button.Visible = true;
			clear_button.Visible = false;
			search_entry.Sensitive = true;

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
	}
}
