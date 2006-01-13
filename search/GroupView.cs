using System;
using System.Collections;

using Mono.Posix;
using Gtk;
using Beagle;
using Search.Tiles;

namespace Search {

	public delegate void TileHandler (Tiles.Tile tile);

	public enum MatchType {
		None,
		NoneInScope,
		Matched
	}

	public class GroupView : VBox {

		public event TileHandler TileSelected;
		private Gtk.SizeGroup tileSizeGroup;
		private Hashtable categories;
		private Gtk.Widget selection;

		public GroupView () : base (false, 0)
		{
			categories = new Hashtable ();
			tileSizeGroup = new Gtk.SizeGroup (Gtk.SizeGroupMode.Both);
			Category box = null;

			foreach (Tiles.TileGroupInfo info in Tiles.Utils.GroupInfo) {
								
				if (info.Group == Tiles.TileGroup.Conversations)
					box = new ConversationCategory (info.Name, info.MoreString, info.FewRows, info.ManyRows);
				else
					box = new TileCategory (info.Name, info.MoreString, info.FewRows, info.ManyRows, tileSizeGroup);

				PackStart (box, false, false, 0);
				box.NoShowAll = true;
				categories [info.Group] = box;
			}

			// FIXME: Add the Best match category
		}
		
		public void AddHit (Tiles.Tile tile)
		{
			tile.Show ();
			tile.Selected += OnTileSelected;
			tile.Deselected += OnTileDeselected;

			Category box = (Category)categories [tile.Group];
			box.Add (tile);

			if (GroupInScope (tile.Group))
				box.Show ();
		}

		public void SubtractHit (Uri uri)
		{
			// FIXME
		}

		public void Finished (bool grabFocus)
		{
			if (!grabFocus)
				return;

			foreach (Category category in categories.Values) {
				if (category.Visible) {
					// We don't use DirectionType.TabForward
					// because that might select the "More" button
					category.ChildFocus (DefaultDirection == TextDirection.Ltr ? DirectionType.Right : DirectionType.Left);
					return;
				}
			}
		}

		public MatchType MatchState {
			get {
				bool hiddenMatches = false;
				foreach (Category category in categories.Values) {
					if (category.Visible)
						return MatchType.Matched;
					else if (!category.Empty)
						hiddenMatches = true;
				}

				return hiddenMatches ? MatchType.NoneInScope : MatchType.None;
			}
		}

		private void OnTileSelected (object tile, EventArgs args)
		{
			if (tile == selection)
				return;

			if (TileSelected != null)
				TileSelected ((Tiles.Tile)tile);

			if (selection != null)
				selection.State = StateType.Normal;
			selection = (Gtk.Widget)tile;
			if (selection != null)
				selection.State = StateType.Selected;
		}

		private void OnTileDeselected (object tile, EventArgs args)
		{
			if (tile != selection)
				return;
			OnTileSelected (null, args);
		}

		private ScopeType scope;
		public ScopeType Scope {
			set {
				scope = value;
				foreach (TileGroup group in categories.Keys) {
					Category category = (Category)categories[group];
					if (!GroupInScope (group))
						category.Hide ();
					else if (!category.Empty)
						category.Show ();
				}
			}
		}

		private bool GroupInScope (TileGroup group)
		{
			switch (scope) {
			case ScopeType.Everywhere:
				return true;

			case ScopeType.Applications:
				return group == TileGroup.Application;

			case ScopeType.Contacts:
				return group == TileGroup.Contact;

			case ScopeType.Documents:
				return group == TileGroup.Documents;

			case ScopeType.Conversations:
				return group == TileGroup.Conversations;

			case ScopeType.Images:
				return group == TileGroup.Image;

			case ScopeType.Media:
				return group == TileGroup.Audio || group == TileGroup.Video;
			}

			return false;
		}

		public SortType Sort {
			set {
				foreach (Category category in categories.Values)
					category.Sort = value;
			}
		}

		public void Clear ()
		{
			foreach (Category box in categories.Values) {
				box.Clear ();
				box.Hide ();
			}
		}
	}
}
