using System;
using System.Collections;

namespace Search {

	public class SortedTileList : IEnumerable, ICloneable {

		ArrayList tiles;

		public SortedTileList (Search.SortType sort) : this (sort, new ArrayList ()) {}

		SortedTileList (Search.SortType sort, ArrayList tiles)
		{
			this.tiles = tiles;
			Sort = sort;
		}

		public int Add (Tiles.Tile tile)
		{
			int index = tiles.BinarySearch (tile, comparer);
			if (index >= 0)
				throw new ArgumentException ("duplicate");

			tiles.Insert (~index, tile);
			return ~index;
		}

		public void Clear ()
		{
			tiles.Clear ();
		}

		public bool Contains (Tiles.Tile tile)
		{
			return tiles.Contains (tile);
		}

		public int IndexOf (Tiles.Tile tile)
		{
			return tiles.IndexOf (tile);
		}

		public void Remove (Tiles.Tile tile)
		{
			int index = tiles.BinarySearch (tile, comparer);
			if (index >= 0)
				tiles.RemoveAt (index);
		}

		public void RemoveAt (int index)
		{
			tiles.RemoveAt (index);
		}

		public Tiles.Tile this[int index] {
			get {
				return (Tiles.Tile)tiles[index];
			}
		}

		public int Count {
			get {
				return tiles.Count;
			}
		}

		public IEnumerator GetEnumerator ()
		{
			return tiles.GetEnumerator ();
		}

		public object Clone ()
		{
			return new SortedTileList (sort, (ArrayList)tiles.Clone ());
		}

		public IList GetRange (int index, int count)
		{
			return tiles.GetRange (index, count);
		}

		Search.SortType sort;
		IComparer comparer;

		public Search.SortType Sort {
			get {
				return sort;
			}
			set {
				sort = value;
				switch (sort) {
				case SortType.Relevance:
				default:
					comparer = new RelevanceComparer ();
					break;
				case SortType.Name:
					comparer = new NameComparer ();
					break;
				case SortType.Modified:
					comparer = new DateComparer ();
					break;
				}

				tiles.Sort (comparer);
			}
		}
	}

	abstract class TileComparer : IComparer {
		public int Compare (object x, object y)
		{
			Tiles.Tile tx = (Tiles.Tile)x, ty = (Tiles.Tile)y;
			int ret;

			ret = Compare (tx, ty);
			if (ret == 0)
				ret = -tx.Timestamp.CompareTo (ty.Timestamp);
			if (ret == 0)
				ret = tx.GetHashCode ().CompareTo (ty.GetHashCode ());
			return ret;
		}

		public abstract int Compare (Tiles.Tile tx, Tiles.Tile ty);
	}

	class RelevanceComparer : TileComparer {
		public override int Compare (Tiles.Tile tx, Tiles.Tile ty)
		{
			return -tx.Score.CompareTo (ty.Score);
		}
	}

	class NameComparer : TileComparer {
		public override int Compare (Tiles.Tile tx, Tiles.Tile ty)
		{
			return String.Compare (tx.Title, ty.Title, true);
		}
	}

	class DateComparer : TileComparer {
		public override int Compare (Tiles.Tile tx, Tiles.Tile ty)
		{
			return -tx.Timestamp.CompareTo (ty.Timestamp);
		}
	}
}
