using System;
using System.Globalization;
using Mono.Unix;

namespace Search.Tiles {

	public struct TileGroupInfo {
		public TileGroup Group;
		public string Name;
		public int Rows;

		public TileGroupInfo (TileGroup group, string name, int rows)
		{
			Group = group;
			Name = name;
			Rows = rows;
		}
	};

	public static class Utils {

		public static TileGroupInfo[] GroupInfo = new TileGroupInfo[] {
			new TileGroupInfo (TileGroup.Application,
					   Catalog.GetString ("Application"), 1),
			new TileGroupInfo (TileGroup.Contact,
					   Catalog.GetString ("Contact"), 2),
			new TileGroupInfo (TileGroup.Folder,
					   Catalog.GetString ("Folder"), 2),
			new TileGroupInfo (TileGroup.Image,
					   Catalog.GetString ("Image"), 2),
			new TileGroupInfo (TileGroup.Audio,
					   Catalog.GetString ("Audio"), 2),
			new TileGroupInfo (TileGroup.Video,
					   Catalog.GetString ("Video"), 2),
			new TileGroupInfo (TileGroup.Documents,
					   Catalog.GetString ("Documents"), 2),
			new TileGroupInfo (TileGroup.Conversations,
					   Catalog.GetString ("Conversations"), 5),
			new TileGroupInfo (TileGroup.Website,
					   Catalog.GetString ("Website"), 2),
			new TileGroupInfo (TileGroup.Feed,
					   Catalog.GetString ("News Feed"), 2),
			new TileGroupInfo (TileGroup.Archive,
					   Catalog.GetString ("Archive"), 2),
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

		private static DateTimeFormatInfo DateTimeFormat = CultureInfo.CurrentCulture.DateTimeFormat;
		private static string ShortMonthDayPattern = DateTimeFormat.MonthDayPattern.Replace ("MMMM", "MMM");
		private static string ShortYearMonthPattern = DateTimeFormat.YearMonthPattern.Replace ("MMMM", "MMM");
		private static string MonthDayPattern = DateTimeFormat.MonthDayPattern;
		private static string LongDatePattern = DateTimeFormat.LongDatePattern.Replace ("dddd, ", "").Replace ("dddd ", "").Replace (" dddd", "");

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
				return Catalog.GetString ("Today");
			else if (span.TotalDays < 2)
				return Catalog.GetString ("Yesterday");
			else if (span.TotalDays < 7)
				return dt.ToString ("dddd"); // "Tuesday"
			else if (dt.Year == today.Year || span.TotalDays < 180)
				return dt.ToString (ShortMonthDayPattern); // "Jul 4"
			else
				return dt.ToString (ShortYearMonthPattern); // "Jan 2001"
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
				return Catalog.GetString ("Today");
			else if (span.TotalDays < 2)
				return Catalog.GetString ("Yesterday");
			else if (span.TotalDays < 7)
				return dt.ToString ("dddd"); // "Tuesday"
			else if (dt.Year == today.Year || span.TotalDays < 180)
				return dt.ToString (MonthDayPattern); // "July 4"
			else
				return dt.ToString (LongDatePattern); // January 7, 2001
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
				return Catalog.GetString ("Today");
			else if (span.TotalDays < 2)
				return Catalog.GetString ("Yesterday");
			else if (span.TotalDays < 7)
				return dt.ToString ("dddd"); // "Tuesday"
			else if (span.TotalDays < 30)
				return String.Format (Catalog.GetPluralString ("{0} week ago", "{0} weeks ago", span.Days / 7) + " ({1:MMMM d, yyyy})", span.Days / 7, dt);
			else if (span.TotalDays < 365 + 180) // Let's say a year and a half to stop saying months
				return String.Format (Catalog.GetPluralString ("{0} month ago", "{0} months ago", span.Days / 30) + " ({1:MMMM d, yyyy})", span.Days / 30, dt);
			else
				return String.Format (Catalog.GetPluralString ("{0} year ago", "{0} years ago", span.Days / 365) + " ({1:MMMM d, yyyy})", span.Days / 365, dt);
		}
	}
}
