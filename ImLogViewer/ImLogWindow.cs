//
// ImLogWindow.cs
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
using System.Threading;
using Beagle.Util;
using Mono.Posix;

namespace ImLogViewer {

	public class ImLogWindow {
		[Widget] Window imviewer;
		[Widget] TreeView timelinetree;
		[Widget] Label time_title;
		[Widget] Entry search_entry;
		[Widget] Button search_button;
		[Widget] Button clear_button;
		[Widget] TextView conversation;
		
		private string selected_log;

		private string speaking_to;
		private string log_path;
		private string highlight_text;
		private string search_text;

		private TreeStore tree_store;
		private ThreadNotify index_thread_notify;
		private Timeline timeline = new Timeline ();

		public ImLogWindow (string path, string search, string highlight)
		{
			if (Directory.Exists (path)) {
				log_path = path;
			} else if (File.Exists (path)) {
				log_path = Path.GetDirectoryName (path);
				selected_log = path;
			} else {
				Console.WriteLine ("ERROR: Log path doesn't exist - {0}", path);
				return;
			}

			highlight_text = highlight;
			search_text = search;				
			
			ShowWindow ();

		}

		private void SetStatusTitle (DateTime dt)
		{
			time_title.Markup = String.Format ("<b>{0}</b>", StringFu.DateTimeToPrettyString (dt));
		}
		
		private void SetWindowTitle (string speaker)
		{
			if (speaker == null || speaker == "")
				return;

			// Find the buddy
			ImBuddy buddy = new GaimBuddyListReader ().Search (speaker);
			
			if (speaker.EndsWith (".chat")) {
				imviewer.Title = String.Format (Catalog.GetString ("Conversations in {0}"), speaker.Replace (".chat", ""));
			} else {
				string nick = speaker;

				if (buddy != null && buddy.Alias != "")
					nick = buddy.Alias;

				imviewer.Title = String.Format (Catalog.GetString ("Conversations with {0}"), nick);
			}

			speaking_to = speaker;
		}

		private void ShowWindow ()
		{
			Application.Init();
			
			Glade.XML gxml = new Glade.XML (null, "ImLogViewer.glade", "imviewer", null);
			gxml.Autoconnect (this);

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

			if (highlight_text != null)
				search_entry.Text = highlight_text;

			if (search_text != null)
				Search (search_text);

			search_entry.Activated += OnSearchClicked;
			search_button.Clicked += OnSearchClicked;
			clear_button.Clicked += OnClearClicked;
			imviewer.DeleteEvent += new DeleteEventHandler (OnWindowDelete);

			AccelGroup accel_group = new AccelGroup ();
			GlobalKeybinder global_keys = new GlobalKeybinder (accel_group);
			global_keys.AddAccelerator (OnWindowClose, (uint) Gdk.Key.Escape, 0, Gtk.AccelFlags.Visible);
			imviewer.AddAccelGroup (accel_group);

			// Index the logs
			index_thread_notify = new ThreadNotify (new ReadyEvent (RepopulateTimeline));
			Thread t = new Thread (new ThreadStart (IndexLogs));
			t.Start ();

			Application.Run();
		}

		private void IndexLogs ()
		{
			foreach (string file in Directory.GetFiles (log_path)) {
				ICollection logs = GaimLog.ScanLog (new FileInfo (file));
				
				foreach (ImLog log in logs) {
					if (speaking_to == null)
						SetWindowTitle (log.SpeakingTo);

					timeline.Add (log, log.StartTime);
				}
			}

			index_thread_notify.WakeupMain ();
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

		private class ImLogPreview
		{
			public string Snippet;
			public ImLog Log;

			public ImLogPreview (ImLog log)
			{
				Snippet = log.EllipsizedSnippet;
				Log = log;
			}
		}

		private void AddCategory (ArrayList list, string name, string date_format)
		{
			if (list.Count > 0) {
				ArrayList previews = GetPreviews (list);
				if (previews.Count > 0) {
					TreeIter parent = tree_store.AppendValues (String.Format ("<b>{0}</b>", Catalog.GetString (name)), "", null);
 					AddPreviews (parent, previews, Catalog.GetString (date_format));
				}
			}
		}

		private void RepopulateTimeline ()
		{
			tree_store.Clear ();

			AddCategory (timeline.Today, "Today", "HH:mm");
			AddCategory (timeline.Yesterday, "Yesterday", "HH:mm");
			AddCategory (timeline.ThisWeek, "This Week", "dddd");
			AddCategory (timeline.LastWeek, "Last Week", "dddd");
			AddCategory (timeline.ThisMonth, "This Month", "MMM d");
			AddCategory (timeline.ThisYear, "This Year", "MMM d");
			AddCategory (timeline.Older, "Older", "yyy MMM d");
		
			timelinetree.ExpandAll();
		}

		private void AddPreviews (TreeIter parent, ArrayList previews, string date_format)
		{
			foreach (ImLogPreview preview in previews) {
				string date = preview.Log.StartTime.ToString (date_format);
				tree_store.AppendValues (parent, date, preview.Snippet, preview.Log);
	
				if (selected_log == null || selected_log == preview.Log.LogFile) {
					selected_log = preview.Log.LogFile;
					RenderConversation (preview.Log);
					ScrollToLog (preview.Log.LogFile);
				}
			}
		}

		private ArrayList GetPreviews (ArrayList list)
		{
			ArrayList logs = new ArrayList ();

			foreach (ImLog log in list) {
				if (search_text != null && search_text != "")
					if (! LogContainsString (log, search_text))
						continue;

				ImLogPreview preview = new ImLogPreview (log);
				logs.Add (preview);
			}

			return logs;
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
			string text = buffer.GetText (buffer.StartIter, buffer.EndIter, false).ToLower ();
			string [] words = highlight.Split (' ');

			foreach (string word in words) {
				int idx = 0;

				if (word == String.Empty)
					continue;

				while ((idx = text.IndexOf (word.ToLower (), idx)) != -1) {					
					Gtk.TextIter start = buffer.GetIterAtOffset (idx);
					Gtk.TextIter end = start;
					end.ForwardChars (word.Length);

					buffer.ApplyTag ("highlight", start, end);

					idx += word.Length;
				}
			}
		}

		private void Search (string text)
		{
			search_entry.Text = text;
			search_button.Visible = false;
			clear_button.Visible = true;
			search_entry.Sensitive = false;

			search_text = text;
			highlight_text = null;
			selected_log = null;
		}

		private void OnConversationSelected (object o, EventArgs args) 
		{
			TreeIter iter;
			TreeModel model;
			
			if (((TreeSelection)o).GetSelected (out model, out iter)) {
				ImLog log = model.GetValue (iter, 2) as ImLog;

				if (log == null)
					return;

				selected_log = log.LogFile;
				RenderConversation (log);
			}
		}

		private void OnWindowClose (object o, EventArgs args)
		{
			Application.Quit ();
		}

		private void OnWindowDelete (object o, DeleteEventArgs args)
		{
			Application.Quit ();
		}

		private void OnSearchClicked (object o, EventArgs args)
		{
			if (search_entry.Text == null || search_entry.Text == "")
				return;

			Search (search_entry.Text);
			RepopulateTimeline ();
		}

		private void ScrollToLog (string scroll_log)
		{
			TreeIter root_iter;
			tree_store.GetIterFirst (out root_iter);
			
			do {
				if (tree_store.IterHasChild (root_iter)) {
					TreeIter child;
					tree_store.IterNthChild (out child, root_iter, 0);
					
					do {
						ImLog log = tree_store.GetValue (child, 2) as ImLog;
						
						if (log.LogFile == scroll_log) {
							TreePath path = tree_store.GetPath (child);
							timelinetree.ExpandToPath (path);
							timelinetree.Selection.SelectPath (path);
							timelinetree.ScrollToCell (path, null, true, 0.5f, 0.0f);
						}
					} while (tree_store.IterNext (ref child));
				}
			} while (tree_store.IterNext (ref root_iter));
		}
		
		private void OnClearClicked (object o, EventArgs args)
		{
			highlight_text = search_text = null;
			search_button.Visible = true;
			clear_button.Visible = false;
			search_entry.Sensitive = true;

			RepopulateTimeline ();

			ScrollToLog (selected_log);
		}
	}
}
