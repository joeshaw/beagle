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
using System.Threading;

using Mono.Data.SqliteClient;

using Beagle.Util;

namespace Beagle.Daemon {

	public class FileAttributesStore_Sqlite : IFileAttributesStore {

		// Version history:
		// 1: Original version
		// 2: Replaced LastIndexedTime with LastAttrTime
		const int VERSION = 2;

		private SqliteConnection connection;
		private BitArray path_flags;
		private int transaction_count = 0;

		enum TransactionState {
			None,
			Requested,
			Started
		}
		private TransactionState transaction_state;

		public FileAttributesStore_Sqlite (string directory, string index_fingerprint)
		{
			bool create_new_db = false;
			path_flags = new BitArray (65536);

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
					reader = ExecuteReaderOrWait (command);
				} catch (Exception ex) {
					create_new_db = true;
				}
				if (reader != null && ! create_new_db) {
					if (ReadOrWait (reader)) {
						stored_version = reader.GetInt32 (0);
						stored_fingerprint = reader.GetString (1);
					}
					reader.Close ();
				}
				command.Dispose ();

				if (VERSION != stored_version
				    || (index_fingerprint != null && index_fingerprint != stored_fingerprint))
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
					    "  last_attrtime  STRING NOT NULL,        " +
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

				reader = ExecuteReaderOrWait (command);

				while (ReadOrWait (reader)) {

					string dir = reader.GetString (0);
					string file = reader.GetString (1);
					string path = Path.Combine (dir, file);
					SetPathFlag (path);
					++count;
				}

				reader.Close ();
				command.Dispose ();

				DateTime dt2 = DateTime.Now;

				Logger.Log.Debug ("Loaded {0} records from {1} in {2:0.000}s", 
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
			c.ConnectionString = "version=" + ExternalStringsHack.SqliteVersion
				+ ",URI=file:" + GetDbPath (directory);
			try {
				c.Open ();
			} catch (ApplicationException) {
				Logger.Log.Error ("File attributes store is an incompatible sqlite database. ({0})", GetDbPath (directory));
				Logger.Log.Error ("We're trying to open with sqlite version {0}.", ExternalStringsHack.SqliteVersion);
				Logger.Log.Error ("Exiting immediately.");
				Environment.Exit (1);
			}
			return c;
		}

		private void DoNonQuery (string format, params object [] args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = String.Format (format, args);

			while (true) {
				try {
					command.ExecuteNonQuery ();
					break;
				} catch (SqliteException ex) {
					if (ex.SqliteError == SqliteError.BUSY)
						Thread.Sleep (50);
					else
						throw ex;
				}
			}

			command.Dispose ();
		}

		private SqliteCommand QueryCommand (string where_format, params object [] where_args)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText =
				"SELECT unique_id, directory, filename, last_mtime, last_attrtime, filter_name, filter_version " +
				"FROM file_attributes WHERE " + 
				String.Format (where_format, where_args);
			return command;
		}

		static private SqliteDataReader ExecuteReaderOrWait (SqliteCommand command)
		{
			SqliteDataReader reader = null;
			while (reader == null) {
				try {
					reader = command.ExecuteReader ();
				} catch (SqliteException ex) {
					if (ex.SqliteError == SqliteError.BUSY)
						Thread.Sleep (50);
					else
						throw ex;
				}
			}
			return reader;
		}

		static private bool ReadOrWait (SqliteDataReader reader)
		{
			while (true) {
				try {
					return reader.Read ();
				} catch (SqliteException ex) {
					if (ex.SqliteError == SqliteError.BUSY)
						Thread.Sleep (50);
					else
						throw ex;
				}
			}
		}

		private FileAttributes GetFromReader (SqliteDataReader reader)
		{
			FileAttributes attr = new FileAttributes ();

			attr.UniqueId = GuidFu.FromShortString (reader.GetString (0));
			attr.Path = System.IO.Path.Combine (reader.GetString (1), reader.GetString (2));
			attr.LastWriteTime = StringFu.StringToDateTime (reader.GetString (3));
			attr.LastAttrTime = StringFu.StringToDateTime (reader.GetString (4));
			attr.FilterName = reader.GetString (5);
			attr.FilterVersion = int.Parse (reader.GetString (6));

			if (attr.FilterName == "")
				attr.FilterName = null;

			return attr;
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
			return path_flags [hash];
		}

		private void SetPathFlag (string path)
		{
			int hash = GetPathHash (path);
			path_flags [hash] = true;
		}

		///////////////////////////////////////////////////////////////////

		public FileAttributes Read (string path)
		{
			SqliteCommand command;
			SqliteDataReader reader;

			if (! GetPathFlag (path))
				return null;

			FileAttributes attr = null;
			bool found_too_many = false;

			// We need to quote any 's that appear in the strings
			// (int particular, in the path)
			string directory = Path.GetDirectoryName (path).Replace ("'", "''");
			string filename = Path.GetFileName (path).Replace ("'", "''");
			lock (connection) {
				command = QueryCommand ("directory='{0}' AND filename='{1}'",
							directory, filename);
				reader = ExecuteReaderOrWait (command);
				
				if (ReadOrWait (reader)) {
					attr = GetFromReader (reader);
					
					if (ReadOrWait (reader))
						found_too_many = true;
				}
				reader.Close ();
				command.Dispose ();

				// If we found more than one matching record for a given
				// directory and filename, something has gone wrong.
				// Since we have no way of knowing which one is correct
				// and which isn't, we delete them all and return
				// null.  (Which in most cases will force a re-index.
				if (found_too_many) {
					DoNonQuery ("DELETE FROM file_attributes WHERE directory='{0}' AND filename='{1}'",
						    directory, filename);
				}
			}

			return attr;
		}

		public bool Write (FileAttributes fa)
		{
			SetPathFlag (fa.Path);

			// We need to quote any 's that appear in the strings
			// (in particular, in the path)
			lock (connection) {
				
				// If a transaction has been requested, start it now.
				MaybeStartTransaction ();

				string filter_name;
				filter_name = fa.FilterName;
				if (filter_name == null)
					filter_name = "";
				filter_name = filter_name.Replace ("'", "''");

				DoNonQuery ("INSERT OR REPLACE INTO file_attributes " +
					    " (unique_id, directory, filename, last_mtime, last_attrtime, filter_name, filter_version) " +
					    " VALUES ('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')",
					    GuidFu.ToShortString (fa.UniqueId),
					    fa.Directory.Replace ("'", "''"), fa.Filename.Replace ("'", "''"),
					    StringFu.DateTimeToString (fa.LastWriteTime),
					    StringFu.DateTimeToString (fa.LastAttrTime),
					    filter_name,
					    fa.FilterVersion);
			}
			return true;
		}

		public void Drop (string path)
		{
			// We don't want to "UnSetPathFlag" here, since we have no way of knowing
			// if another path hashes to the same value as this one.

			// We need to quote any 's that appear in the strings
			// (in particular, in the path)
			string directory = Path.GetDirectoryName (path).Replace ("'", "''");
			string filename = Path.GetFileName (path).Replace ("'", "''");
			lock (connection) {

				// If a transaction has been requested, start it now.
				MaybeStartTransaction ();

				DoNonQuery ("DELETE FROM file_attributes WHERE directory='{0}' AND filename='{1}'",
					    directory, filename);
			}
		}

		private void MaybeStartTransaction ()
		{
			if (transaction_state == TransactionState.Requested) {
				DoNonQuery ("BEGIN");
				transaction_state = TransactionState.Started;
			}
		}

		public void BeginTransaction ()
		{
			if (transaction_state == TransactionState.None)
				transaction_state = TransactionState.Requested;
		}

		public void CommitTransaction ()
		{
			if (transaction_state == TransactionState.Started) {
				lock (connection)
					DoNonQuery ("COMMIT");
			}
			transaction_state = TransactionState.None;
		}

		public void Flush ()
		{
			lock (connection) {
				if (transaction_count > 0) {
					Logger.Log.Debug ("Flushing requested -- committing sqlite transaction");
					DoNonQuery ("COMMIT");
					transaction_count = 0;
				}
			}
		}

		///////////////////////////////////////////////////////////////////

		// Return all attributes in the attributes database, used for merging

		private ICollection ReadAllAttributes () 
		{
			ArrayList attributes = new ArrayList ();
			
			SqliteCommand command;
			SqliteDataReader reader;
				
			lock (connection) {
				command = new SqliteCommand ();
				command.Connection = connection;
				command.CommandText =
					"SELECT unique_id, directory, filename, last_mtime, last_attrtime, filter_name, filter_version " +
					"FROM file_attributes";
				
				reader = ExecuteReaderOrWait (command);
				
				while (ReadOrWait (reader)) {
					attributes.Add (GetFromReader (reader));
				}
				reader.Close ();
				command.Dispose ();
			}
			
			return attributes;
		}

		// FIXME: Might wanna do this a bit more intelligently

		public void Merge (FileAttributesStore_Sqlite fa_sqlite_store_to_merge) 
		{
			ICollection attributes = fa_sqlite_store_to_merge.ReadAllAttributes ();
				
			BeginTransaction ();

			foreach (FileAttributes attribute in attributes)
				Write (attribute);

			CommitTransaction ();
		}
	}
}
