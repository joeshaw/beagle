using System;
using Gtk;
using Gdk;

namespace Search {

	public class Spinner : Gtk.Image {

        	private Pixbuf idlePixbuf;
        	private Pixbuf [] frames;
        	private int currentFrame;
        	private uint timeoutId;

		private const int targetSize = 24;
		private const int refreshRate = 125;

		~Spinner ()
		{
			Stop ();
		}

		Gtk.IconTheme theme;

		protected override void OnRealized ()
		{
			base.OnRealized ();

			theme = Gtk.IconTheme.GetForScreen (Screen);
			theme.Changed += ThemeChanged;
			LoadImages ();
		}

		private void ThemeChanged (object obj, EventArgs args)
		{
			LoadImages ();
		}

        	private void LoadImages ()
        	{
			int iconSize = targetSize;

#if false
			// This code requires gtk-sharp 2.6, which we don't (yet) require
			foreach (int size in theme.GetIconSizes ("gnome-spinner-rest")) {
				if (size >= targetSize) {
					iconSize = size;
					break;
				}
			}
#endif

			idlePixbuf = theme.LoadIcon ("gnome-spinner-rest", iconSize, 0);
			if (idlePixbuf == null) {
				Console.Error.WriteLine ("Could not load idle spinner image");
				frames = null;
				Pixbuf = null;
				return;
			}

			Gdk.Pixbuf framesPixbuf = theme.LoadIcon ("gnome-spinner", iconSize, 0);
			if (framesPixbuf == null) {
				Console.Error.WriteLine ("Could not load spinner image");
				frames = null;
				Pixbuf = idlePixbuf;
				return;
			}

			int frameWidth = idlePixbuf.Width, frameHeight = idlePixbuf.Height;
        		int width = framesPixbuf.Width, height = framesPixbuf.Height;
        		if (width % frameWidth != 0 || height % frameHeight != 0) {
				Console.Error.WriteLine ("Spinner image is wrong size");
				frames = null;
				Pixbuf = idlePixbuf;
				return;
			}

			int rows = height / frameHeight, cols = width / frameWidth;

        		frames = new Pixbuf[rows * cols];

        		for (int y = 0, n = 0; y < rows; y++) {
        			for (int x = 0; x < cols; x++, n++) {
        				frames[n] = new Pixbuf (framesPixbuf,
								x * frameWidth,
								y * frameHeight,
								frameWidth,
								frameHeight);
        			}
        		}

        		currentFrame = 0;
			if (timeoutId != 0)
				Pixbuf = frames[currentFrame];
			else
				Pixbuf = idlePixbuf;
        	}

		public void Start ()
		{
			if (!IsRealized)
				return;
			if (frames == null || frames.Length == 0)
				return;
			if (timeoutId != 0)
				return;

			timeoutId = GLib.Timeout.Add (refreshRate, TimeoutHandler);
		}

		public void Stop ()
		{
			if (timeoutId == 0)
				return;

			GLib.Source.Remove (timeoutId);
			timeoutId = 0;
			Pixbuf = idlePixbuf;
		}

        	private bool TimeoutHandler ()
        	{
        		Pixbuf = frames[currentFrame];
			currentFrame = (currentFrame + 1) % frames.Length;
        		return true;
        	}
	}
}
