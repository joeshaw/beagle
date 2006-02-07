using System;
using System.Collections;
using System.IO;
using System.Threading;

namespace Search.Tiles {

	public class ThumbnailFactory : Gtk.Object {

		private Gnome.ThumbnailFactory factory = new Gnome.ThumbnailFactory (Gnome.ThumbnailSize.Normal);
		private Thread thread;
		private ArrayList in_queue = new ArrayList ();
		private ArrayList out_queue = new ArrayList ();

		public ThumbnailFactory ()
		{
			Gtk.Quit.AddDestroy (1, this);
		}

		protected override void OnDestroyed ()
		{
			// Force the thumbnailing thread to exit cleanly once it
			// finishes the current thumbnail
			lock (in_queue)
				in_queue.Clear ();
		}

		private class ThumbnailRequest {
			public Gtk.Image Image;
			public Beagle.Hit Hit;
			public int Size;

			public ThumbnailRequest (Gtk.Image image, Beagle.Hit hit, int size)
			{
				Image = image;
				Hit = hit;
				Size = size;
			}

			public static ThumbnailRequest Select (IEnumerable thumbnails)
			{
				ThumbnailRequest first = null;
				Gtk.Widget w;

				foreach (ThumbnailRequest req in thumbnails) {

					// FIXME: we ought to be able to just look at
					// req.Image.IsMapped. But for some reason, the
					// images are still showing up as mapped even
					// when their parents aren't, which seems
					// impossible to me from looking at the code,
					// but...
					for (w = req.Image; w != null; w = w.Parent) {
						if (!w.IsMapped)
							break;
					}
					if (w == null)
						return req;

					if (first == null)
						first = req;
				}
				return first;
			}
		}

		public bool SetThumbnailIcon (Gtk.Image image, Beagle.Hit hit, int size)
		{
			string thumbnail = Gnome.Thumbnail.PathForUri (hit.UriAsString, Gnome.ThumbnailSize.Normal);
			if (!File.Exists (thumbnail)) {
				lock (in_queue) {
					in_queue.Add (new ThumbnailRequest (image, hit, size));
					if (thread == null) {
						thread = new Thread (GenerateThumbnails);
						thread.Start ();
					}
				}
				return false;
			}

			Gdk.Pixbuf icon = new Gdk.Pixbuf (thumbnail);
			if (icon == null)
				return false;

			int width = icon.Width, height = icon.Height;
			if (icon.Height > size) {
				if (icon.Width > icon.Height) {
					width = size;
					height = (size * icon.Height) / icon.Width;
				} else {
					height = size;
					width = (size * icon.Width) / icon.Height;
				}
			} else if (icon.Width > size) {
				width = size;
				height = (size * icon.Height) / icon.Width;
			}
			icon = icon.ScaleSimple (width, height, Gdk.InterpType.Bilinear);

			image.Pixbuf = icon;
			return true;
		}

		private void GenerateThumbnails ()
		{
			ThumbnailRequest req;

			while (true) {
				lock (in_queue) {
					if (in_queue.Count == 0) {
						thread = null;
						return;
					}

					req = ThumbnailRequest.Select (in_queue);
					in_queue.Remove (req);
				}

				Gdk.Pixbuf icon = factory.GenerateThumbnail (req.Hit.UriAsString, req.Hit.MimeType);
				FileInfo fi = new FileInfo (req.Hit.Uri.LocalPath);

				if (icon == null)
					factory.CreateFailedThumbnail (req.Hit.UriAsString, fi.LastWriteTime);
				else
					factory.SaveThumbnail (icon, req.Hit.UriAsString, DateTime.Now);

				lock (out_queue)
					out_queue.Add (req);
				GLib.Idle.Add (FinishSetThumbnail);
			}
		}

		private bool FinishSetThumbnail ()
		{
			ThumbnailRequest req;
			lock (out_queue) {
				req = (ThumbnailRequest)out_queue[0];
				out_queue.RemoveAt (0);
			}

			SetThumbnailIcon (req.Image, req.Hit, req.Size);
			return false;
		}
	}
}
