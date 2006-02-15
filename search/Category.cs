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
		Gtk.Label headerLabel, position;
		Gtk.Button prev, next;
		int fewRows, manyRows, columns;
		int few, many;
		bool expanded;

		public Category (string name, int rows, int columns)
		{
			WidgetFlags |= WidgetFlags.NoWindow;

			header = new Gtk.HBox (false, 0);

			headerLabel = new Gtk.Label ();
			headerLabel.Markup = "<big><b>" + GLib.Markup.EscapeText (name) + "</b></big>";
			headerLabel.SetAlignment (0.0f, 0.5f);
			headerLabel.Show ();
			header.PackStart (headerLabel, true, true, 0);

			position = new Gtk.Label ();
			position.ModifyFg (Gtk.StateType.Normal, position.Style.Base (Gtk.StateType.Selected));
			header.PackStart (position, false, false, 0);
			position.Show ();

			prev = MakeButton (header, Gtk.Stock.GoBack, OnPrev);
			next = MakeButton (header, Gtk.Stock.GoForward, OnNext);

			header.Show ();
			header.Parent = this;
			header.SizeRequested += HeaderSizeRequested;

			tiles = new SortedTileList (SortType.Relevance);
			page = 0;

			fewRows = rows;
			manyRows = rows * 2;
			Columns = columns;

			UpdateButtons ();
		}

		private Gtk.Button MakeButton (Gtk.HBox header, string icon, EventHandler handler)
		{
			Gtk.Button button = new Gtk.Button ();
			Gtk.Image img = new Gtk.Image (icon, Gtk.IconSize.Button);
			button.Add (img);
			button.Relief = Gtk.ReliefStyle.None;
			button.Clicked += handler;

			header.PackStart (button, false, false, 0);
			button.ShowAll ();

			return button;
		}

		protected int Columns {
			get {
				return columns;
			}
			set {
				HideTiles ();

				if (page > 0) {
					// Adjust page so that the first visible tile
					// remains visible
					if (expanded)
						page = FirstVisible / (manyRows * value);
					else
						page = ((FirstVisible - few) / (manyRows * value)) + 1;
				}

				columns = value;
				few = fewRows * columns;
				many = manyRows * columns;

				UpdateButtons ();
				ShowTiles ();
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
			if (tiles.Count <= FirstVisible && page > 0) {
				// The page we were viewing disappeared
				page--;
			}

			prev.Sensitive = (page != 0);
			next.Sensitive = (tiles.Count > LastVisible + 1);

			if (tiles.Count > 0)
				position.LabelProp = String.Format (Catalog.GetString ("{0}-{1} of {2}"), FirstVisible + 1, LastVisible + 1, tiles.Count);
			else
				position.LabelProp = "";
		}

		protected override void OnAdded (Gtk.Widget widget)
		{
			widget.ChildVisible = false;
			HideTiles ();
			tiles.Add ((Tiles.Tile)widget);
			widget.Parent = this;
			ShowTiles ();

			UpdateButtons ();
		}

		protected override void OnRemoved (Gtk.Widget widget)
		{
			tiles.Remove ((Tiles.Tile)widget);
			widget.Unparent ();
			ShowTiles ();

			UpdateButtons ();
		}

		void HideTiles ()
		{
			foreach (Widget tile in VisibleTiles)
				tile.ChildVisible = false;
			QueueResize ();
		}

		void ShowTiles ()
		{
			foreach (Widget tile in VisibleTiles)
				tile.ChildVisible = true;
			QueueResize ();
		}

		private bool showingMany {
			get {
				// Show extra tiles on every page after the first, unless
				// there are only two pages and the second one only has
				// enough tiles to fit the "fewer" size.
				return (page > 0 && tiles.Count > 2 * few) || expanded;
			}
		}

		void OnPrev (object obj, EventArgs args)
		{
			HideTiles ();
			page--;
			ShowTiles ();
			UpdateButtons ();
		}

		void OnNext (object obj, EventArgs args)
		{
			HideTiles ();
			page++;
			ShowTiles ();
			UpdateButtons ();
		}

		protected int PageSize {
			get {
				return Math.Min (showingMany ? many : few, tiles.Count);
			}
		}

		protected int FirstVisible {
			get {
				if (page == 0)
					return 0;
				else if (expanded)
					return page * many;
				else
					return few + (page - 1) * many;
			}
		}

		protected int LastVisible {
			get {
				return Math.Min (FirstVisible + PageSize, tiles.Count) - 1;
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
				HideTiles ();
				tiles.Sort = value;
				ShowTiles ();
			}
		}

		public void Select (bool focus, bool expanded)
		{
			if (focus && !Empty)
				((Gtk.Widget)VisibleTiles[0]).GrabFocus ();
			this.expanded = expanded;
		}
	}
}
