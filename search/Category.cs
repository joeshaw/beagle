using Gtk;
using Gdk;
using System;
using System.Collections;
using Mono.Unix;

namespace Search {

	public abstract class Category : Container {

		SortedTileList tiles;
		int page;

		protected Gtk.HBox header;
		Gtk.Label headerLabel;
		Gtk.Button more, fewer, prev, next;
		int fewRows, manyRows, columns;
		int few, many;

		public Category (string name, string moreString, int fewRows, int manyRows, int columns)
		{
			WidgetFlags |= WidgetFlags.NoWindow;

			header = new Gtk.HBox (false, 0);

			headerLabel = new Gtk.Label ();
			headerLabel.Markup = "<big><b>" + GLib.Markup.EscapeText (name) + "</b></big>";
			headerLabel.SetAlignment (0.0f, 0.5f);
			headerLabel.Show ();
			header.PackStart (headerLabel, true, true, 0);

			more = MakeButton (header, OnMore);
			SetButtonLabel (more, moreString);
			fewer = MakeButton (header, OnFewer);
			prev = MakeButton (header, OnPrev);
			next = MakeButton (header, OnNext);

			header.Show ();
			header.Parent = this;
			header.SizeRequested += HeaderSizeRequested;

			tiles = new SortedTileList (SortType.Relevance);
			page = 0;

			this.fewRows = fewRows;
			this.manyRows = manyRows;
			Columns = columns;
		}

		private Gtk.Button MakeButton (Gtk.HBox header, EventHandler handler)
		{
			Gtk.Button button = Gtk.Button.NewWithLabel ("");
			button.Relief = Gtk.ReliefStyle.None;
			button.NoShowAll = true;
			button.Clicked += handler;

			header.PackStart (button, false, false, 0);

			return button;
		}

		private void SetButtonLabel (Gtk.Button button, string text)
		{
			Gtk.Label label = (Gtk.Label)(button.Child);
			label.Markup = "<u>" + GLib.Markup.EscapeText (text) + "</u>";
			label.ModifyFg (Gtk.StateType.Normal, label.Style.Base (Gtk.StateType.Selected));
			label.ModifyFg (Gtk.StateType.Prelight, label.Style.Base (Gtk.StateType.Selected));
		}

		protected int Columns {
			get {
				return columns;
			}
			set {
				if (page > 0) {
					// Adjust page so that the first visible tile
					// remains visible
					page = (many * page) / (manyRows * value);
				}

				columns = value;
				few = fewRows * columns;
				many = manyRows * columns;

				SetButtonLabel (fewer, String.Format (Catalog.GetPluralString ("Top {0} Result", "Top {0} Results", few), few));
				SetButtonLabel (prev, String.Format (Catalog.GetPluralString ("Previous {0}", "Previous {0}", many), many));
				SetButtonLabel (next, String.Format (Catalog.GetPluralString ("Next {0}", "Next {0}", many), many));

				UpdateButtons ();
				UpdateTileVisibility ();
				QueueResize ();
			}
		}

		void HeaderSizeRequested (object obj, Gtk.SizeRequestedArgs args)
		{
			Gtk.Requisition req = args.Requisition;
			Gtk.Requisition labelReq = headerLabel.ChildRequisition;

			req.Height = (int)(labelReq.Height * 1.5);

			args.Requisition = req;
		}

		void UpdateButtons ()
		{
			if (tiles.Count <= few) {
				if (fewer.Visible) {
					fewer.Hide ();
					next.Hide ();
					prev.Hide ();
				} else if (more.Visible)
					more.Hide ();
				page = 0;
				return;
			}

			if (!showingMany) {
				if (!more.Visible)
					more.Show ();
				return;
			}

			if (tiles.Count <= page * many) {
				// The page we were viewing disappeared
				page--;
			}

			prev.Sensitive = (page != 0);
			next.Sensitive = (tiles.Count > (page + 1) * many);
		}

		protected override void OnAdded (Gtk.Widget widget)
		{
			widget.ChildVisible = false;
			tiles.Add ((Tiles.Tile)widget);
			widget.Parent = this;

			UpdateButtons ();
			UpdateTileVisibility ();
		}

		protected override void OnRemoved (Gtk.Widget widget)
		{
			tiles.Remove ((Tiles.Tile)widget);
			widget.Unparent ();

			UpdateButtons ();
			UpdateTileVisibility ();
		}

		void UpdateTileVisibility ()
		{
			int first = FirstVisible, last = LastVisible, i;

			for (i = first - 1; i > 0 && tiles[i].ChildVisible; i--)
				tiles[i].ChildVisible = false;

			for (i = first; i <= last; i++)
				tiles[i].ChildVisible = true;

			for (i = last + 1; i < tiles.Count && tiles[i].ChildVisible; i++)
				tiles[i].ChildVisible = false;

			QueueResize ();
		}

		private bool showingMany {
			get {
				return fewer.Visible;
			}
		}

		void OnMore (object obj, EventArgs args)
		{
			more.Hide ();
			fewer.Show ();
			prev.Show ();
			prev.Sensitive = false;
			next.Show ();
			next.Sensitive = (tiles.Count > many);

			UpdateTileVisibility ();
		}

		void OnFewer (object obj, EventArgs args)
		{
			// UpdateTileVisibility won't DTRT in this case
			foreach (Widget tile in VisibleTiles)
				tile.ChildVisible = false;

			fewer.Hide ();
			next.Hide ();
			prev.Hide ();
			more.Show ();
			page = 0;
			UpdateTileVisibility ();
		}

		void OnPrev (object obj, EventArgs args)
		{
			foreach (Gtk.Widget w in VisibleTiles)
				w.ChildVisible = false;
			page--;
			foreach (Gtk.Widget w in VisibleTiles)
				w.ChildVisible = true;

			if (page == 0)
				prev.Sensitive = false;
			next.Sensitive = true;
			UpdateTileVisibility ();
		}

		void OnNext (object obj, EventArgs args)
		{
			foreach (Gtk.Widget w in VisibleTiles)
				w.ChildVisible = false;
			page++;
			foreach (Gtk.Widget w in VisibleTiles)
				w.ChildVisible = true;

			if (many * (page + 1) >= tiles.Count)
				next.Sensitive = false;
			prev.Sensitive = true;
			UpdateTileVisibility ();
		}

		protected int PageSize {
			get {
				return Math.Min (showingMany ? many : few, tiles.Count);
			}
		}

		protected int FirstVisible {
			get {
				return page * many;
			}
		}

		protected int LastVisible {
			get {
				if (showingMany)
					return Math.Min ((page + 1) * many - 1, tiles.Count - 1);
				else
					return Math.Min (few - 1, tiles.Count - 1);
			}
		}

		protected IList VisibleTiles {
			get {
				return tiles.GetRange (FirstVisible, LastVisible - FirstVisible + 1);
			}
		}

		public IEnumerable AllTiles {
			get {
				return tiles;
			}
		}

		protected override void ForAll (bool include_internals, Callback callback)
		{
			foreach (Widget w in (SortedTileList)tiles.Clone ())
				callback (w);
			if (include_internals)
				callback (header);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			Rectangle headerRect, bgRect;
			int headerHeight = header.Allocation.Height;
			Gdk.Color white, bg, mid;

			headerRect = bgRect = Allocation;
			headerRect.Height = headerHeight;
			bgRect.Y += headerHeight;
			bgRect.Height -= headerHeight;

			GdkWindow.DrawRectangle (Style.BaseGC (State), true, bgRect);

			white = Style.Base (State);
			bg = Style.Background (State);

			mid = new Gdk.Color ();
			mid.Red = (ushort)((white.Red + bg.Red) / 2);
			mid.Green = (ushort)((white.Green + bg.Green) / 2);
			mid.Blue = (ushort)((white.Blue + bg.Blue) / 2);
			Style.BaseGC (State).RgbFgColor = mid;
			GdkWindow.DrawRectangle (Style.BaseGC (State), true, headerRect);
			Style.BaseGC (State).Foreground = white;

			return base.OnExposeEvent (evt);
		}

		public void Clear ()
		{
			while (tiles.Count > 0)
				Remove (tiles[tiles.Count - 1]);
		}

		public bool Empty {
			get {
				return tiles.Count == 0;
			}
		}

		public int Count {
			get {
				return tiles.Count;
			}
		}

		public Search.SortType Sort {
			set {
				tiles.Sort = value;
				UpdateTileVisibility ();
			}
		}
	}
}
