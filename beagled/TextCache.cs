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
using System.Threading;

using Mono.Data.SqliteClient;

using Beagle.Util;

namespace Beagle.Daemon {

	public class TextCache {

		static public bool Debug = false;

		public const string SELF_CACHE_TAG = "*self*";

		private string text_cache_dir;
		private SqliteConnection connection;

		private enum TransactionState {
			None,
			Requested,
			Started
		}
		private TransactionState transaction_state;

		private static TextCache user_cache = null;

		public static TextCache UserCache {
			get {
				if (user_cache == null)
					user_cache = new TextCache (PathFinder.StorageDir);
				
				return user_cache;
			}
		}

		public TextCache (string storage_dir) 
		{
			text_cache_dir = Path.Combine (storage_dir, "TextCache");
			if (! Directory.Exists (text_cache_dir)) {
				Directory.CreateDirectory (text_cache_dir);
				
				// Create our cache subdirectories.
				for (int i = 0; i < 256; ++i) {
					string subdir = i.ToString ("x");
					if (i < 16)
						subdir = "0" + subdir;
					subdir = Path.Combine (text_cache_dir, subdir);
					Directory.CreateDirectory (subdir);
				}
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
					    "  filename STRING NOT NULL          " +
					    ")");
			}
		}

		private static string UriToString (Uri uri)
		{
			return uri.ToString ().Replace ("'", "''");
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
			if (Debug)
				Logger.Log.Debug ("Executing command '{0}'", String.Format (format, args));
			SqliteCommand command = NewCommand (format, args);
			while (true) {
				try {
					command.ExecuteNonQuery ();
					break;
				} catch (SqliteException ex) {
					// FIXME: should we eventually time out?
					if (ex.SqliteError == SqliteError.BUSY)
						Thread.Sleep (50);
					else
						throw ex;
				}
			}
			command.Dispose ();
		}

		private SqliteDataReader ExecuteReaderOrWait (SqliteCommand command)
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

		private bool ReadOrWait (SqliteDataReader reader)
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

		private void Insert (Uri uri, string filename)
		{
			lock (connection) {
				MaybeStartTransaction_Unlocked ();
				DoNonQuery ("INSERT OR REPLACE INTO uri_index (uri, filename) VALUES ('{0}', '{1}')",
					    UriToString (uri), filename);
			}
		}

		private string LookupPathRawUnlocked (Uri uri, bool create_if_not_found)
		{
			SqliteCommand command;
			SqliteDataReader reader = null;
			string path = null;

			command = NewCommand ("SELECT filename FROM uri_index WHERE uri='{0}'", 
			                      UriToString (uri));
			reader = ExecuteReaderOrWait (command);
			if (ReadOrWait (reader))
				path = reader.GetString (0);
			reader.Close ();
			command.Dispose ();

			if (path == null && create_if_not_found) {
				string guid = Guid.NewGuid ().ToString ();
				path = Path.Combine (guid.Substring (0, 2), guid.Substring (2));
				Insert (uri, path);
			}

			if (path == SELF_CACHE_TAG)
				return SELF_CACHE_TAG;

			return path != null ? Path.Combine (text_cache_dir, path) : null;
		}

		// Don't do this unless you know what you are doing!  If you
		// do anything to the path you get back other than open and
		// read the file, you will almost certainly break something.
		// And it will be evidence that you are a bad person and that
		// you deserve whatever horrible fate befalls you.
		public string LookupPathRaw (Uri uri)
		{
			lock (connection)
				return LookupPathRawUnlocked (uri, false);
		}

		private string LookupPath (Uri uri, bool create_if_not_found)
		{
			lock (connection) {
				string path = LookupPathRawUnlocked (uri, create_if_not_found);
				if (path == SELF_CACHE_TAG) {
					// FIXME: How do we handle URI remapping for self-cached items?
#if false
					if (uri_remapper != null)
						uri = uri_remapper (uri);
#endif
					if (! uri.IsFile) {
						string msg = String.Format ("Non-file uri {0} flagged as self-cached", uri);
						throw new Exception (msg);
					}
					return uri.LocalPath;
				}
				return path;
			}
		}
		
		public void MarkAsSelfCached (Uri uri)
		{
			lock (connection)
				Insert (uri, SELF_CACHE_TAG);
		}

		public TextWriter GetWriter (Uri uri)
		{
			// FIXME: Uri remapping?
			string path = LookupPath (uri, true);

			FileStream stream;
			stream = new FileStream (path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);

			// We don't expect to need this again in the near future.
			FileAdvise.FlushCache (stream);

			StreamWriter writer;
			writer = new StreamWriter (stream);
			return writer;
		}

		public void WriteFromReader (Uri uri, TextReader reader)
		{
			TextWriter writer;
			writer = GetWriter (uri);
			string line;
			while ((line = reader.ReadLine ()) != null)
				writer.WriteLine (line);
			writer.Close ();
		}

		public void WriteFromString (Uri uri, string str)
		{
			if (str == null) {
				Delete (uri);
				return;
			}

			TextWriter writer;
			writer = GetWriter (uri);
			writer.WriteLine (str);
			writer.Close ();
		}

		// FIXME: Uri remapping?
		public TextReader GetReader (Uri uri)
		{
			string path = LookupPath (uri, false);
			if (path == null)
				return null;

			FileStream stream;
			try {
				stream = new FileStream (path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			} catch (FileNotFoundException ex) {
				return null;
			}
			
			StreamReader reader;
			reader = new StreamReader (stream);
			return reader;
		}

		public void Delete (Uri uri)
		{
			lock (connection) {
				string path = LookupPathRawUnlocked (uri, false);
				if (path != null) {
					MaybeStartTransaction_Unlocked ();
					DoNonQuery ("DELETE FROM uri_index WHERE uri='{0}' AND filename='{1}'", 
					            UriToString (uri), path);
					if (path != SELF_CACHE_TAG)
						File.Delete (path);
				}
			}
		}

		private void MaybeStartTransaction_Unlocked ()
		{
			if (transaction_state == TransactionState.Requested)
				DoNonQuery ("BEGIN");
			transaction_state = TransactionState.Started;
		}

		public void BeginTransaction ()
		{
			lock (connection) {
				if (transaction_state == TransactionState.None)
					transaction_state = TransactionState.Requested;
			}
		}

		public void CommitTransaction ()
		{
			lock (connection) {
				if (transaction_state == TransactionState.Started)
					DoNonQuery ("COMMIT");
				transaction_state = TransactionState.None;
			}
		}
	}
}
