//
// TextCache.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;
using System.IO;

using Mono.Data.SqliteClient;

using Beagle.Util;

namespace Beagle.Daemon {

	public class TextCache {

		static string text_cache_dir;
		static SqliteConnection connection;

		static TextCache () 
		{
			text_cache_dir = Path.Combine (PathFinder.RootDir, "TextCache");
			if (! Directory.Exists (text_cache_dir))
				Directory.CreateDirectory (text_cache_dir);

			// Create our cache subdirectories.
			for (int i = 0; i < 256; ++i) {
				string subdir = Path.Combine (text_cache_dir, i.ToString ("x"));
				if (! Directory.Exists (subdir))
					Directory.CreateDirectory (subdir);
			}

			// Create our Sqlite database
			string db_filename = Path.Combine (text_cache_dir, "TextCache.db");
			bool create_new_db = false;
			if (! File.Exists (db_filename))
				create_new_db = true;

			connection = new SqliteConnection ();
			connection.ConnectionString = "URI=file:" + db_filename;
			connection.Open ();

			if (create_new_db) {
				DoNonQuery ("CREATE TABLE uri_index (            " +
					    "  uri      STRING UNIQUE NOT NULL,  " +
					    "  filename STRING UNIQUE NOT NULL   " +
					    ")");
			}
		}

		private static SqliteCommand NewCommand (string format, params object [] args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (format, args);
			return command;
		}

		private static void DoNonQuery (string format, params object [] args)
		{
			SqliteCommand command = NewCommand (format, args);
			command.ExecuteNonQuery ();
			command.Dispose ();
		}

		public static string LookupPath (Uri uri, bool create_if_not_found)
		{
			SqliteCommand command;
			SqliteDataReader reader;
			string path = null;

			command = NewCommand ("SELECT filename FROM uri_index WHERE uri='{0}'", uri);
			reader = command.ExecuteReader ();
			if (reader.Read ())
				path = reader [0].ToString ();
			reader.Close ();
			command.Dispose ();

			if (path == null && create_if_not_found) {
				string guid = Guid.NewGuid ().ToString ();
				path = Path.Combine (guid.Substring (0, 2), guid.Substring (2));
				DoNonQuery ("INSERT INTO uri_index (uri, filename) VALUES ('{0}', '{1}')", uri, path);
			}

			return path != null ? Path.Combine (text_cache_dir, path) : null;
		}

		public static TextWriter GetWriter (Uri uri)
		{
			string path = LookupPath (uri, true);

			FileStream stream;
			stream = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.Read);

			StreamWriter writer;
			writer = new StreamWriter (stream);
			return writer;
		}

		public static TextReader GetReader (Uri uri)
		{
			string path = LookupPath (uri, false);
			if (path == null)
				return null;

			FileStream stream;
			try {
				stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.Read);
			} catch (FileNotFoundException ex) {
				return null;
			}
			
			StreamReader reader;
			reader = new StreamReader (stream);
			return reader;
		}

		public static void Delete (Uri uri)
		{
			string path = LookupPath (uri, false);
			if (path != null) {
				DoNonQuery ("DELETE FROM uri_index WHERE uri='{0}' AND filename='{1}'", uri, path); 
				File.Delete (path);
			}
		}
	}
}
