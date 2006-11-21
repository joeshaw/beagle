//
// NotificationArea.cs
//
// Copyright (c) 2006 Novell, Inc.
//

using System;
using System.Collections;

using Gtk;

namespace Search {

	public class NotificationMessage : HBox {

		private Gtk.Image icon;
		private Gtk.Label title;
		private Gtk.Label message;
		private Gtk.Box action_box;

		public NotificationMessage () : this (null, null) { }

		public NotificationMessage (string t, string m) : base (false, 5)
		{
			BorderWidth = 5;

			icon = new Image (Stock.DialogInfo, IconSize.Dialog);
			this.PackStart (icon, false, true, 5);

			VBox vbox = new VBox (false, 5);
			this.PackStart (vbox, true, true, 0);

			title = new Label ();
			title.SetAlignment (0.0f, 0.5f);
			this.Title = t;
			vbox.PackStart (title, false, true, 0);

			message = new Label ();
			message.SetAlignment (0.0f, 0.0f);
			this.Message = m;			
			vbox.PackStart (message, true, true, 0);

			action_box = new HBox (false, 3);

			Button hide_button = new Button ("Hide");
			hide_button.Clicked += OnHideClicked;
			action_box.PackEnd (hide_button, false, true, 0);

			Alignment action_align = new Alignment (1.0f, 0.5f, 0.0f, 0.0f);
			action_align.Add (action_box);
			vbox.PackStart (action_align, false, true, 0);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			GdkWindow.DrawRectangle (Style.BackgroundGC (StateType.Selected), false,
						 Allocation.X,
						 Allocation.Y,
						 Allocation.Width,
						 Allocation.Height);

			GdkWindow.DrawRectangle (Style.LightGC (StateType.Normal), true,
						 Allocation.X + 1,
						 Allocation.Y + 1,
						 Allocation.Width - 2,
						 Allocation.Height - 2);

			return base.OnExposeEvent (e);
		}

		public void AddAction (string name, EventHandler e)
		{
			Button action = new Button (name);
			
			if (e != null)
				action.Clicked += e;

			action_box.PackStart (action, false, true, 0);
		}

		public void SetTimeout (uint timeout)
		{
			GLib.Timeout.Add (timeout, new GLib.TimeoutHandler (OnTimeout));
		}

		private void OnHideClicked (object o, EventArgs args)
		{
			Hide ();
		}

		private bool OnTimeout ()
		{
			Hide ();
			return false;
		}

		public string Title {
			get { return title.Text; }
			set { title.Markup = "<big><b>" + value + "</b></big>"; }
		}

		public string Message {
			get { return message.Text; }
			set { message.Markup = value; }
		}

		public string Icon {
			set { icon.SetFromStock (value, Gtk.IconSize.Dialog); }
		}

		public uint Timeout {
			set { SetTimeout (value); }
		}
	}

	public class NotificationArea : Alignment {
		
		public NotificationArea () : base (0.0f, 0.5f, 1.0f, 0.0f)
		{
		}

		public new void Display (NotificationMessage m)
		{
			m.ShowAll ();
			Add (m);
		}
	}
}
