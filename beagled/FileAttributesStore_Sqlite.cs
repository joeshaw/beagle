//
// FileAttributesStore_Sqlite.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.IO;

using Mono.Data.SqliteClient;

using Beagle.Util;

namespace Beagle.Daemon {

	public class FileAttributesStore_Sqlite : IFileAttributesStore {

		const int VERSION = 1;

		private SqliteConnection connection;
		private byte[] path_flags;

		public FileAttributesStore_Sqlite (string directory, string index_fingerprint)
		{
			bool create_new_db = false;

			path_flags = new byte [8192];

			if (! File.Exists (GetDbPath (directory))) {
				create_new_db = true;
			} else {
				connection = Open (directory);

				SqliteCommand command;
				SqliteDataReader reader = null;
				int stored_version = 0;
				string stored_fingerprint = null;


				command = new SqliteCommand ();
				command.Connection = connection;
				command.CommandText =
					"SELECT version, fingerprint FROM db_info";
				try {
					reader = command.ExecuteReader ();
				} catch (Exception ex) {
					create_new_db = true;
				}
				if (reader != null && ! create_new_db) {
					if (reader.Read ()) {
						stored_version = int.Parse (reader [0].ToString ());
						stored_fingerprint = reader [1].ToString ();
					}
					reader.Close ();
				}
				command.Dispose ();

				if (VERSION != stored_version
				    || index_fingerprint != stored_fingerprint)
					create_new_db = true;
			}

			if (create_new_db) {
				if (connection != null)
					connection.Dispose ();
				File.Delete (GetDbPath (directory));
				connection = Open (directory);

				DoNonQuery ("CREATE TABLE db_info (             " +
					    "  version       INTEGER NOT NULL,  " +
					    "  fingerprint   STRING NOT NULL    " +
					    ")");

				DoNonQuery ("INSERT INTO db_info (version, fingerprint) VALUES ({0}, '{1}')",
					    VERSION, index_fingerprint);

				DoNonQuery ("CREATE TABLE file_attributes (           " +
					    "  unique_id      STRING UNIQUE,          " +
					    "  directory      STRING NOT NULL,        " +
					    "  filename       STRING NOT NULL,        " +
					    "  last_mtime     STRING NOT NULL,        " +
					    "  last_indexed   STRING NOT NULL,        " +
					    "  filter_name    STRING NOT NULL,        " +
					    "  filter_version STRING NOT NULL         " +
					    ")");
			} else {
				SqliteCommand command;
				SqliteDataReader reader;
				int count = 0;

				DateTime dt1 = DateTime.Now;

				// Select all of the files and use them to populate our bit-vector.
				command = new SqliteCommand ();
				command.Connection = connection;
				command.CommandText = "SELECT directory, filename FROM file_attributes";
				reader = command.ExecuteReader ();

				while (reader.Read ()) {
					string dir = reader [0].ToString ();
					string file = reader [1].ToString ();
					string path = Path.Combine (dir, file);
					SetPathFlag (path, true);
					++count;
				}
				reader.Close ();
				command.Dispose ();

				DateTime dt2 = DateTime.Now;

				Logger.Log.Info ("Loaded {0} records from {1} in {2:0.000}s", 
						 count, GetDbPath (directory), (dt2 - dt1).TotalSeconds);
			}
		}

		///////////////////////////////////////////////////////////////////

		private string GetDbPath (string directory)
		{
			return Path.Combine (directory, "FileAttributesStore.db");
		}

		private SqliteConnection Open (string directory)
		{
			SqliteConnection c;			
			c = new SqliteConnection ();
			c.ConnectionString = "URI=file:" + GetDbPath (directory);
			c.Open ();
			return c;
		}

		private void DoNonQuery (string format, params object [] args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (format, args);
			command.ExecuteNonQuery ();
			command.Dispose ();
		}

		private SqliteCommand QueryCommand (string where_format, params object [] where_args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText =
				"SELECT unique_id, directory, filename, last_mtime, last_indexed, filter_name, filter_version " +
				"FROM file_attributes WHERE " + 
				String.Format (where_format, where_args);
			return command;
		}

		private FileAttributes GetFromReader (SqliteDataReader reader)
		{
			FileAttributes attr = new FileAttributes ();

			attr.UniqueId = reader [0].ToString ();
			attr.Path = System.IO.Path.Combine (reader [1].ToString (), reader [2].ToString ());
			attr.LastWriteTime = StringFu.StringToDateTime (reader [3].ToString ());
			attr.LastIndexedTime = StringFu.StringToDateTime (reader [4].ToString ());
			attr.FilterName = reader [5].ToString ();
			attr.FilterVersion = int.Parse (reader [6].ToString ());

			return attr;
		}

		private SqliteCommand PathQueryCommand ()
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = "SELECT directory, filename FROM file_attributes";
			return command;
		}

		///////////////////////////////////////////////////////////////////

		private int GetPathHash (string path)
		{
			uint hash = 0xdeadbeef;
			foreach (char c in path)
				hash = 17 * hash + (uint) c;
			// Fold the 32 bits in 16.
			return (int) ((hash & 0xffff) ^ (hash >> 16));
		}

		private bool GetPathFlag (string path)
		{
			int hash = GetPathHash (path);
			int index = hash >> 3;
			byte mask = (byte) (1 << (hash & 0x7));
			return (path_flags [index] & mask) != 0;
		}

		private void SetPathFlag (string path, bool value)
		{
			int hash = GetPathHash (path);
			int index = hash >> 3;
			byte mask = (byte) (1 << (hash & 0x7));

			if (value)
				path_flags [index] |= mask;
			else
				path_flags [index] &= (byte) ~mask;
		}

		///////////////////////////////////////////////////////////////////

		// FIXME: Since the Sqlite store will (in general) not have many
		// entries, we should probably optimize for the case of store_sqlite.Read (path)
		// returning null.  Maybe we could keep a cache of the stored paths in memory? 

		public FileAttributes Read (string path)
		{
			SqliteCommand command;
			SqliteDataReader reader;

			if (! GetPathFlag (path))
				return null;

			FileAttributes attr = null;

			// We need to quote any 's that appear in the strings
			// (int particular, in the path)
			string directory = Path.GetDirectoryName (path);
			string filename = Path.GetFileName (path);
			lock (connection) {
				command = QueryCommand ("directory='{0}' AND filename='{1}'",
							directory.Replace ("'", "''"),
							filename.Replace ("'", "''"));
				reader = command.ExecuteReader ();
				
				if (reader.Read ()) {
					attr = GetFromReader (reader);
					
					if (reader.Read ())
						throw new Exception ("Matched multiple items when only expecting one: path=" + path);
				}
				reader.Close ();
				command.Dispose ();
			}

			if (attr == null)
				SetPathFlag (path, false);
				
			return attr;
		}

		public bool Write (FileAttributes fa)
		{
			SetPathFlag (fa.Path, true);

			// We need to quote any 's that appear in the strings
			// (in particular, in the path)
			lock (connection) {
				DoNonQuery ("INSERT OR REPLACE INTO file_attributes " +
					    " (unique_id, directory, filename, last_mtime, last_indexed, filter_name, filter_version) " +
					    " VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
					    fa.UniqueId, fa.Directory.Replace ("'", "''"), fa.Filename.Replace ("'", "''"),
					    StringFu.DateTimeToString (fa.LastWriteTime),
					    StringFu.DateTimeToString (fa.LastIndexedTime),
					    fa.FilterName,
					    fa.FilterVersion);
			}
			return true;
		}

		public void Drop (string path)
		{
			// We don't want to SetPathFlag (path, false) here, since we have no way of knowing
			// if another path hashes to the same value as this one.

			// We need to quote any 's that appear in the strings
			// (in particular, in the path)
			string directory = Path.GetDirectoryName (path);
			string filename = Path.GetFileName (path);
			lock (connection) {
				DoNonQuery ("DELETE FROM file_attributes WHERE directory='{0}' AND filename='{1}'",
					    directory.Replace ("'", "''"),
					    filename.Replace ("'", "''"));
			}
		}
	}
}
