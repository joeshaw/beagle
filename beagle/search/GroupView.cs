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
					box = new ConversationCategory (info);
				else
					box = new TileCategory (info, tileSizeGroup);

				PackStart (box, false, false, 0);
				box.NoShowAll = true;
				box.CategoryToggle += OnCategoryToggle;
				categories [info.Group] = box;
			}

			// FIXME: Add the Best match category
		}
		
		public void AddHit (Tiles.Tile tile)
		{
			tile.Show ();
			tile.Selected += OnTileSelected;

			Category box = (Category)categories [tile.Group];
			box.Add (tile);

			if (GroupInScope (tile.Group))
				box.Show ();
		}

		public void SubtractHit (Uri uri)
		{
			foreach (Category box in categories.Values) {
				foreach (Tile tile in box.AllTiles) {
					if (tile.Hit.Uri.Equals (uri)) {
						if (tile.State == StateType.Selected)
							OnTileSelected (null, EventArgs.Empty);
						box.Remove (tile);
						return;
					}
				}
			}
		}

		public void Finished (bool grabFocus)
		{
			Category first = null;
			bool others = false;

			foreach (Category category in categories.Values) {
				if (category.Visible) {
					if (first == null)
						first = category;
					else {
						others = true;
						break;
					}
				}
			}

			if (first != null)
				first.Select (grabFocus, !others);
		}

		public int TileCount {
			get {
				int count = 0;

				foreach (Category category in categories.Values) {
					if (category.Visible)
						count += category.Count;
				}

				return count;
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

		private ScopeType scope;
		public ScopeType Scope {
			set {
				scope = value;
				foreach (TileGroup group in categories.Keys) {
					Category category = (Category)categories[group];
					if (!GroupInScope (group))
						category.Expanded = false;
					else if (!category.Empty)
						category.Expanded = true;
				}
			}
		}

		private bool GroupInScope (TileGroup group)
		{
			ScopeType scopetype = Utils.TileGroupToScopeType (group);

			return (scope & scopetype) == scopetype;
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

		private void OnCategoryToggle (ScopeType catScope)
		{
			// we're not using the set function cause we directly
			// close/open the expander in Category.cs
			scope = scope ^ catScope;
			CategoryToggled (catScope);
		}

		public delegate void CategoryToggledDelegate (ScopeType catScope);
		public event CategoryToggledDelegate CategoryToggled;

		
		
	}
}