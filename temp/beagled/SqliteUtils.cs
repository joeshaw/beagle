//
// SqliteUtils.cs
//
// Copyright (C) 2004-2006 Novell, Inc.
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
using System.Threading;

using Mono.Data.SqliteClient;

namespace Beagle.Util {

	public class SqliteUtils {

		// static class
		private SqliteUtils () { }

		public static int DoNonQuery (SqliteConnection connection, string command_text)
		{
			SqliteCommand command;
			command = new SqliteCommand ();
			command.Connection = connection;
			command.CommandText = command_text;
			int ret = 0;

			while (true) {
				try {
					ret = command.ExecuteNonQuery ();
					break;
				} catch (SqliteBusyException ex) {
					Thread.Sleep (50);
				}
			}

			command.Dispose ();
			return ret;
		}
			
		public static int DoNonQuery (SqliteConnection connection, string format, params object [] args)
		{
			return DoNonQuery (connection, String.Format (format, args));
		}

		public static SqliteCommand QueryCommand (SqliteConnection connection, string where_format, params object [] where_args)
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

		public static SqliteDataReader ExecuteReaderOrWait (SqliteCommand command)
		{
			SqliteDataReader reader = null;
			while (reader == null) {
				try {
					reader = command.ExecuteReader ();
				} catch (SqliteBusyException ex) {
					Thread.Sleep (50);
				}
			}
			return reader;
		}

		public static bool ReadOrWait (SqliteDataReader reader)
		{
			while (true) {
				try {
					return reader.Read ();
				} catch (SqliteBusyException ex) {
					Thread.Sleep (50);
				}
			}
		}

		public static string Sanitize (string item)
		{
			return item.Replace ("'", "''");
		}
	}
}

		
