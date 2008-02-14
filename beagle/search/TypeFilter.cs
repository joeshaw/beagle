using System;
using System.Collections;
using System.Text.RegularExpressions;

using Mono.Unix;

using Beagle.Search.Tiles;

// FIXME: it would be nicer to create QueryParts to do the filtering beagled-side

// FIXME: Whoa! This fucking thing is a complete ghetto. Needs a rewrite.

namespace Beagle.Search {

	public class TypeFilter {

		static Hashtable aliases;
		static Regex parser;

		delegate bool CustomFilter (Tile tile, string query);

		static void AddAliases (string aliasesString, object value)
		{
			foreach (string alias in aliasesString.Split ('\n'))
				aliases[alias.ToLower ()] = value;
		}

		static TypeFilter () {
			parser = new Regex ("^(.*\\s)?(?:type:|kind:)(\\w+|\"[^\"]+\"|'[^\']+')(\\s.*)?$");

			aliases = new Hashtable ();

			/* Translators: the strings in TypeFilter.cs are for when the user
			 * wants to limit search results to a specific type. You don't need
			 * to give an exact translation of the exact same set of English
			 * words. Just provide a list of likely words the user might use
			 * after the "type:" keyword to refer to each type of match.
			 */
			AddAliases (Catalog.GetString ("file\nfiles"), typeof (Tiles.TileFile));
			AddAliases (Catalog.GetString ("mail\nemail\ne-mail"), typeof (Tiles.MailMessage));
			AddAliases (Catalog.GetString ("im\nims\ninstant message\ninstant messages\ninstant-message\ninstant-messages\nchat\nchats"), typeof (Tiles.IMLog));
			AddAliases (Catalog.GetString ("presentation\npresentations\nslideshow\nslideshows\nslide\nslides"), typeof (Tiles.Presentation));

			AddAliases (Catalog.GetString ("application\napplications\napp\napps"), TileGroup.Application);
			AddAliases (Catalog.GetString ("contact\ncontacts\nvcard\nvcards"), TileGroup.Contact);
			AddAliases (Catalog.GetString ("folder\nfolders"), TileGroup.Folder);
			AddAliases (Catalog.GetString ("image\nimages\nimg"), TileGroup.Image);
			AddAliases (Catalog.GetString ("audio"), TileGroup.Audio);
			AddAliases (Catalog.GetString ("video"), TileGroup.Video);
			AddAliases (Catalog.GetString ("media"), new CustomFilter (MediaFilter));
			AddAliases (Catalog.GetString ("document\ndocuments\noffice document\noffice documents"), TileGroup.Documents);
			AddAliases (Catalog.GetString ("conversation\nconversations"), TileGroup.Conversations);
			AddAliases (Catalog.GetString ("web\nwww\nwebsite\nwebsites"), TileGroup.Website);
			AddAliases (Catalog.GetString ("feed\nnews\nblog\nrss"), TileGroup.Feed);
			AddAliases (Catalog.GetString ("archive\narchives"), TileGroup.Archive);
			AddAliases (Catalog.GetString ("person\npeople"), new CustomFilter (PersonFilter));


			AddAliases ("avi", "video/x-msvideo");
			AddAliases ("bz", "application/x-bzip");
			AddAliases ("bz2", "application/x-bzip");
			AddAliases ("bzip", "application/x-bzip");
			AddAliases ("bzip2", "application/x-bzip");
			AddAliases ("csv", "text/x-comma-separated-values");
			AddAliases ("deb", "application/x-deb");
			AddAliases ("doc", "application/msword");
			AddAliases ("eps", "image/x-eps");
			AddAliases ("excel", "application/vnd.ms-excel");
			AddAliases ("gif", "image/gif");
			AddAliases ("gimp", "image/x-xcf");
			AddAliases ("gz", "application/x-gzip");
			AddAliases ("gzip", "application/x-gzip");
			AddAliases ("html", "text/html");
			AddAliases ("impress", "application/vnd.sun.xml.impress");
			AddAliases ("jpeg", "image/jpeg");
			AddAliases ("jpg", "image/jpeg");
			AddAliases ("mp3", "audio/mpeg");
			AddAliases ("mpeg", "video/mpeg");
			AddAliases ("msword", "application/msword");
			AddAliases ("ods", "application/vnd.oasis.opendocument.spreadsheet");
			AddAliases ("odt", "application/vnd.oasis.opendocument.text");
			AddAliases ("ogg", "application/ogg"); // FIXME when this changes
			AddAliases ("ots", "application/vnd.oasis.opendocument.spreadsheet-template");
			AddAliases ("ott", "application/vnd.oasis.opendocument.text-template");
			AddAliases ("pdf", "application/pdf");
			AddAliases ("photoshop", "image/x-psd");
			AddAliases ("png", "image/png");
			AddAliases ("postscript", "application/postscript");
			AddAliases ("powerpoint", "application/vnd.ms-powerpoint");
			AddAliases ("ppt", "application/vnd.ms-powerpoint");
			AddAliases ("ps", "application/postscript");
			AddAliases ("psd", "image/x-psd");
			AddAliases ("real", "audio/x-pn-realaudio");
			AddAliases ("realaudio", "audio/x-pn-realaudio");
			AddAliases ("rpm", "application/x-rpm");
			AddAliases ("rtf", "application/rtf");
			AddAliases ("sgi", "image/x-sgi");
			AddAliases ("stc", "application/vnd.sun.xml.calc.template");
			AddAliases ("stw", "application/vnd.sun.xml.writer.template");
			AddAliases ("svg", "image/svg+xml");
			AddAliases ("sxc", "application/vnd.sun.xml.calc");
			AddAliases ("sxi", "application/vnd.sun.xml.impress");
			AddAliases ("sxw", "application/vnd.sun.xml.writer");
			AddAliases ("tar", "application/x-tar");
			AddAliases ("tar.gz", "application/x-compressed-tar");
			AddAliases ("text", "text/plain");
			AddAliases ("tgz", "application/x-compressed-tar");
			AddAliases ("theora", "application/ogg"); // FIXME when this changes
			AddAliases ("tif", "image/tiff");
			AddAliases ("tiff", "image/tiff");
			AddAliases ("txt", "text/plain");
			AddAliases ("vorbis", "application/ogg"); // FIXME when this changes
			AddAliases ("wav", "audio/x-wav");
			AddAliases ("word", "application/msword");
			AddAliases ("xcf", "image/x-xcf");
			AddAliases ("xls", "application/vnd.ms-excel");
			AddAliases ("xlt", "application/vnd.ms-excel");
			AddAliases ("xml", "text/xml");
		}

		static bool MediaFilter (Tile tile, string query)
		{
			return tile.Group == TileGroup.Audio || tile.Group == TileGroup.Video;
		}

		// Check that each word in @query is a substring of at least one
		// of the values of @property in @hit
		static bool MatchWords (string query, Beagle.Hit hit, string property)
		{
			string[] vals = hit.GetProperties (property);
			if (vals == null) {
				vals = hit.GetProperties ("parent:" + property);
				if (vals == null)
					return false;
			}

			foreach (string word in query.ToLower().Split (' ')) {
				bool matched = false;

				foreach (string match in vals) {
					if (match.ToLower().IndexOf (word) != -1) {
						matched = true;
						break;
					}
				}
				if (!matched)
					return false;
			}
			return true;
		}

		static bool PersonFilter (Tile tile, string query)
		{
			if (tile is Tiles.Contact)
				return true;
			else if (tile is Tiles.MailMessage) {
				if (MatchWords (query, tile.Hit, "fixme:from"))
					return true;
				if (MatchWords (query, tile.Hit, "fixme:to"))
					return true;
				if (MatchWords (query, tile.Hit, "fixme:cc"))
					return true;
			} else if (tile is Tiles.IMLog) {
				if (MatchWords (query, tile.Hit, "fixme:speakingto"))
					return true;
				if (MatchWords (query, tile.Hit, "fixme:speakingto_alias"))
					return true;
			}
			return false;
		}

		public static TypeFilter MakeFilter (ref string query)
		{
			Match match = parser.Match (query);
			if (!match.Success)
				return null;

			query = match.Groups[1].Value + match.Groups[3].Value;
			try {
				return new TypeFilter (match.Groups[2].Value, query);
			} catch {
				// FIXME
				Console.Error.WriteLine ("Ignoring unrecognized type alias {0}", match.Groups[2].Value);
				return null;
			}
		}

		object filter;
		string query;

		public TypeFilter (string type, string query)
		{
			filter = aliases[type.ToLower ()];
			if (filter == null)
				throw new ArgumentException ("Unrecognized type alias");
			this.query = query;
		}

		public bool Filter (Tile tile)
		{
			if (filter is Type) {
				return (tile.GetType () == (Type)filter ||
					tile.GetType ().IsSubclassOf ((Type)filter));
			} else if (filter is TileGroup) {
				return tile.Group == (TileGroup)filter;
			} else if (filter is string) {
				return tile.Hit.MimeType == (string)filter;
			} else if (filter is CustomFilter) {
				return ((CustomFilter)filter) (tile, query);
			} else
				return true;
		}
	}
}
