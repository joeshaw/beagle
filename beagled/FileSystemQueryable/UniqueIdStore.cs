//
// UniqueIdStore.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Text;

using Mono.Data.SqliteClient;

using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class UniqueIdStore {

		const int VERSION = 1;

		SqliteConnection connection;
		Hashtable roots = new Hashtable ();
		Hashtable cache = new Hashtable ();

		public UniqueIdStore (string directory, string index_fingerprint)
		{
			if (File.Exists (GetDbPath (directory)))
				connection = GetDbConnection (directory);

			if (connection == null || ! CheckVersion (index_fingerprint)) {
				
				if (connection != null)
					connection.Dispose ();
				File.Delete (GetDbPath (directory));

				connection = GetDbConnection (directory);
				SaveVersion (index_fingerprint);

				// FIXME: There should probably be some indexes on this db,
				// at least for the id and name/parent_id.
				DoNonQuery ("CREATE TABLE unique_ids (                     " +
					    "  id              STRING UNIQUE NOT NULL,     " +
					    "  parent_id       STRING,                     " +
					    "  name            STRING NOT NULL,            " +
					    "  please_cache    BOOL                        " +
					    ")");

				//DoNonQuery ("CREATE UNIQUE INDEX id_index ON unique_ids ( id )");
				//DoNonQuery ("CREATE UNIQUE INDEX parent_and_name_index ON unique_ids ( parent_id, name )");
			}
			
			SetPragmaSynchronous (false);

			PopulateCache ();
		}

		private class Record {
			public Guid Id;
			public Guid ParentId;
			public string Name;
		}

		private Record GetRecordById (Guid id)
		{
			lock (connection) {
				if (cache.Contains (id))
					return cache [id] as Record;

				SqliteCommand command;
				command = NewCommand ("SELECT parent_id, name, please_cache FROM unique_ids WHERE id='{0}'",
						      GuidFu.ToShortString (id));

				SqliteDataReader reader;
				reader = command.ExecuteReader ();

				Record record = null;
				bool please_cache = false;
				if (reader.Read ()) {
					record = new Record ();
					record.Id = id;
					record.ParentId = GuidFu.FromShortString (reader [0].ToString ());
					record.Name = reader [1].ToString ();
					please_cache = (reader [2].ToString () == "1");
				}
				reader.Close ();
				command.Dispose ();
				
				if (record != null) {
					if (record.ParentId == Guid.Empty)
						roots [record.Name] = record;
					if (please_cache)
						cache [id] = record;
				}

				return record;
			}
		}

		private Record GetRecordByNameAndParentId (string name, Guid parent_id)
		{
			lock (connection) {

				SqliteCommand command;
				command = NewCommand ("SELECT id FROM unique_ids WHERE name='{0}' and parent_id='{1}'",
						      name.Replace ("'", "''"), 
						      GuidFu.ToShortString (parent_id));

				SqliteDataReader reader;
				reader = command.ExecuteReader ();

				Record record = null;
				if (reader.Read ()) {
					record = new Record ();
					record.Id = GuidFu.FromShortString (reader [0].ToString ());
					record.ParentId = parent_id;
					record.Name = name;
				}
				reader.Close ();
				command.Dispose ();

				return record;
			}
		}

		public bool IsCached (Guid id)
		{
			return cache.Contains (id);
		}

		private void PopulateCache ()
		{
			int count = 0;
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			lock (connection) {

				SqliteCommand command;
				command = NewCommand ("SELECT id, parent_id, name FROM unique_ids where please_cache='1'");

				SqliteDataReader reader;
				reader = command.ExecuteReader ();

				while (reader.Read ()) {
					Record record = new Record ();
					record.Id = GuidFu.FromShortString (reader [0].ToString ());
					record.ParentId = GuidFu.FromShortString (reader [1].ToString ());
					record.Name = reader [2].ToString ();

					cache [record.Id] = record;
					++count;
				}
				reader.Close ();
				command.Dispose ();
			}

			sw.Stop ();
			Logger.Log.Debug ("Pre-populated UniqueIdStore cache with {0} items in {1}", count, sw);

		}

		public string GetPathById (Guid id)
		{
			lock (connection) {
				string name = null;
				while (id != Guid.Empty) {
					Record record = GetRecordById (id);
					if (record == null)
						return null;
					if (name == null)
						name = record.Name;
					else
						name = Path.Combine (record.Name, name);
					id = record.ParentId;
				}
				return name;
			}
		}

		public Guid GetIdByNameAndParentId (string name, Guid parent_id)
		{
			lock (connection) {
				Record record = GetRecordByNameAndParentId (name, parent_id);
				return record != null ? record.Id : Guid.Empty;
			}
		}

		// A Uid Uri is of the form uid:38cd460e-96db-404b-965a-0e5d79412ce7

		public string GetPathByUidUri (Uri uri)
		{
			return GetPathById (GuidFu.FromUri (uri));
		}

		public Uri GetFileUriByUidUri (Uri uri)
		{
			string path = GetPathById (GuidFu.FromUri (uri));
			return path != null ? UriFu.PathToFileUri (path) : null;
		}

		public void Add (Guid id, Guid parent_id, string name, bool please_cache)
		{
			lock (connection) {
				Record record;

				record = cache [id] as Record;
				if (record != null 
				    && record.ParentId == parent_id
				    && record.Name == name)
						return;
				
				record = new Record ();
				record.Id = id;
				record.ParentId = parent_id;
				record.Name = name;
				
				DoNonQuery ("INSERT OR REPLACE INTO unique_ids (id, parent_id, name, please_cache) VALUES ('{0}', '{1}', '{2}', '{3}')",
					    GuidFu.ToShortString (record.Id),
					    GuidFu.ToShortString (record.ParentId),
					    record.Name.Replace ("'", "''"),
					    please_cache ? "1" : "");

				if (parent_id == Guid.Empty) {
					roots [name] = record;
					please_cache = true;
				}

				if (please_cache || cache.Contains (record.Id))
					cache [record.Id] = record;
			}
		}

		public void AddRoot (Guid id, string name, bool please_cache)
		{
			Add (id, Guid.Empty, name, please_cache);
		}

		public void Drop (Guid id)
		{
			lock (connection) {
				DoNonQuery ("DELETE FROM unique_ids WHERE id='{0}'", GuidFu.ToShortString (id));
				cache.Remove (id);
			}
		}

		///////////////////////////////////////////////////////////////////
			
		//
		// Some Sqlite convenience functions
		//

		private string GetDbPath (string directory)
		{
			return Path.Combine (directory, "UniqueIdStore.db");
		}

		private SqliteConnection GetDbConnection (string directory)
		{
			SqliteConnection c;			
			c = new SqliteConnection ();
			c.ConnectionString = "URI=file:" + GetDbPath (directory);
			c.Open ();

			return c;
		}

		private SqliteCommand NewCommand (string format, params object [] args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (format, args);
			return command;
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

		private void SetPragmaSynchronous (bool value)
		{
			SqliteCommand command = NewCommand ("PRAGMA synchronous = {0}", value ? "ON" : "OFF");
			command.ExecuteScalar ();
 			command.Dispose ();
		}

		///////////////////////////////////////////////////////////////////

		//
		// Check and set the database version
		//

		private bool CheckVersion (string index_fingerprint)
		{
			SqliteCommand command;
			SqliteDataReader reader = null;
			int stored_version = 0;
			string stored_fingerprint = null;
			
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = "SELECT version, fingerprint FROM db_info";

			try {
				reader = command.ExecuteReader ();
			} catch (Exception ex) {
				// If we catch an exception, the reader will be null,
				// so stored_version and stored_fingerprint will not
				// be set.
			}
			
			if (reader != null) {
				if (reader.Read ()) {
					stored_version = int.Parse (reader [0].ToString ());
					stored_fingerprint = reader [1].ToString ();
				}
				reader.Close ();
			}
			command.Dispose ();

			return VERSION == stored_version && index_fingerprint == stored_fingerprint;
		}
		
		private void SaveVersion (string index_fingerprint)
		{
			DoNonQuery ("CREATE TABLE db_info (             " +
				    "  version       INTEGER NOT NULL,  " +
				    "  fingerprint   STRING NOT NULL    " +
				    ")");

			DoNonQuery ("INSERT INTO db_info (version, fingerprint) VALUES ({0}, '{1}')",
				    VERSION, index_fingerprint);
		}
	}
}
