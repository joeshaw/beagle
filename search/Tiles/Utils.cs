using System;
using Mono.Unix;

namespace Search.Tiles {

	public struct TileGroupInfo {
		public TileGroup Group;
		public string Name, MoreString;
		public int FewRows, ManyRows;

		public TileGroupInfo (TileGroup group, string name, string moreString, int fewRows, int manyRows)
		{
			Group = group;
			Name = name;
			MoreString = moreString;
			FewRows = fewRows;
			ManyRows = manyRows;
		}
	};

	public static class Utils {

		public static TileGroupInfo[] GroupInfo = new TileGroupInfo[] {
			new TileGroupInfo (TileGroup.Application,
					   Catalog.GetString ("Application"),
					   Catalog.GetString ("More application results..."),
					   1, 2),
			new TileGroupInfo (TileGroup.Contact,
					   Catalog.GetString ("Contact"),
					   Catalog.GetString ("More contact results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Folder,
					   Catalog.GetString ("Folder"),
					   Catalog.GetString ("More folder results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Image,
					   Catalog.GetString ("Image"),
					   Catalog.GetString ("More image results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Audio,
					   Catalog.GetString ("Audio"),
					   Catalog.GetString ("More audio results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Video,
					   Catalog.GetString ("Video"),
					   Catalog.GetString ("More video results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Documents,
					   Catalog.GetString ("Documents"),
					   Catalog.GetString ("More document results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Conversations,
					   Catalog.GetString ("Conversations"),
					   Catalog.GetString ("More conversation results..."),
					   5, 10),
			new TileGroupInfo (TileGroup.Website,
					   Catalog.GetString ("Website"),
					   Catalog.GetString ("More website results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Feed,
					   Catalog.GetString ("News Feed"),
					   Catalog.GetString ("More news feed results..."),
					   2, 4),
			new TileGroupInfo (TileGroup.Archive,
					   Catalog.GetString ("Archive"),
					   Catalog.GetString ("More archive results..."),
					   2, 4),
		};

		public static DateTime ParseTimestamp (string timestamp)
		{
			DateTime dt;
			dt = new DateTime (Int32.Parse (timestamp.Substring (0, 4)),
					   Int32.Parse (timestamp.Substring (4, 2)),
					   Int32.Parse (timestamp.Substring (6, 2)),
					   Int32.Parse (timestamp.Substring (8, 2)),
					   Int32.Parse (timestamp.Substring (10, 2)),
					   Int32.Parse (timestamp.Substring (12, 2)));
			return dt;
		}

		public static string NiceShortDate (string timestamp)
		{
			DateTime dt;

			try {
				dt = ParseTimestamp (timestamp);
			} catch {
				return "";
			}

			return NiceShortDate (dt);
		}

		public static string NiceShortDate (DateTime dt)
		{
			if (dt.Year <= 1970)
				return "-";

			dt = dt.ToLocalTime ();
			DateTime today = DateTime.Today;
			TimeSpan span = today - dt;

			if (span.TotalDays < 1)
				return "Today";
			else if (span.TotalDays < 2)
				return "Yesterday";
			else if (span.TotalDays < 7)
				return dt.ToString ("dddd"); // "Tuesday"
			else if (dt.Year == today.Year || span.TotalDays < 180)
				return dt.ToString ("MMM d"); // "Jul 4"
			else
				return dt.ToString ("MMM yyyy"); // Jan 2001
		}

		public static string NiceLongDate (string timestamp)
		{
			DateTime dt;

			try {
				dt = ParseTimestamp (timestamp);
			} catch {
				return "";
			}

			return NiceLongDate (dt);
		}

		public static string NiceLongDate (DateTime dt)
		{
			if (dt.Year <= 1970)
				return "-";

			dt = dt.ToLocalTime ();
			DateTime today = DateTime.Today;
			TimeSpan span = today - dt;

			if (span.TotalDays < 1)
				return "Today";
			else if (span.TotalDays < 2)
				return "Yesterday";
			else if (span.TotalDays < 7)
				return dt.ToString ("dddd"); // "Tuesday"
			else if (dt.Year == today.Year || span.TotalDays < 180)
				return dt.ToString ("MMMM d"); // "July 4"
			else
				return dt.ToString ("MMMM d, yyyy"); // January 7, 2001
		}

		public static string NiceVeryLongDate (string timestamp)
		{
			DateTime dt;

			try {
				dt = ParseTimestamp (timestamp);
			} catch {
				return "";
			}

			return NiceVeryLongDate (dt);
		}

		public static string NiceVeryLongDate (DateTime dt)
		{
			if (dt.Year <= 1970)
				return "-";

			dt = dt.ToLocalTime ();
			DateTime today = DateTime.Today;
			TimeSpan span = today - dt;

			if (span.TotalDays < 1)
				return String.Format ("Today ({0:MMMM d, yyyy})", dt);
			else if (span.TotalDays < 2)
				return String.Format ("Yesterday ({0:MMMM d, yyyy})", dt);
			else if (span.TotalDays < 7)
				return String.Format ("{0:dddd} ({0:MMMM d, yyyy})", dt);
			else if (span.TotalDays < 30)
				return String.Format (Catalog.GetPluralString ("{0} week ago", "{0} weeks ago", span.Days / 7) + " ({1:MMMM d, yyyy})", span.Days / 7, dt);
			else if (span.TotalDays < 365 + 180) // Lets say a year and a half to stop saying months
				return String.Format (Catalog.GetPluralString ("{0} month ago", "{0} months ago", span.Days / 30) + " ({1:MMMM d, yyyy})", span.Days / 30, dt);
			else
				return String.Format (Catalog.GetPluralString ("{0} year ago", "{0} years ago", span.Days / 365) + " ({1:MMMM d, yyyy})", span.Days / 365, dt);
		}
	}
}
