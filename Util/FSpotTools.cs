//
// FSpotTools.cs
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

namespace Beagle.Util {

	public class FSpotTools {
		
		public class Photo {
			public uint Id;
			public string Path;
			public string Description;
			// FIXME: Need to support tags
		}

		static private string PhotoStorePath {
			get {
				string home = Environment.GetEnvironmentVariable ("HOME");
				return Path.Combine (home, ".gnome2/f-spot/photos.db");
			}
		}

		static private SqliteConnection connection;
		static private SqliteConnection PhotoStoreConnection {
			get {
				if (connection == null && File.Exists (PhotoStorePath)) {
					connection = new SqliteConnection ();
					connection.ConnectionString = "URI=file:" + PhotoStorePath;
					connection.Open ();
				}
			
				return connection;
			}
		}

		static private bool HavePhotoStore {
			get {
				return PhotoStoreConnection != null;
			}
		}

		static Hashtable directoryCache = null;
		static bool IsPossibleDirectory (string directory)
		{
			if (! HavePhotoStore)
				return false;

			if (directoryCache == null) {
				directoryCache = new Hashtable ();
				
				SqliteCommand command = new SqliteCommand ();
				command.Connection = PhotoStoreConnection;
				command.CommandText = "SELECT DISTINCT directory_path FROM photos";
				SqliteDataReader reader = command.ExecuteReader ();

				while (reader.Read ()) {
					directoryCache [reader [0]] = true;
				}

				command.Dispose ();
			}

			return directoryCache.Contains (directory);
		}

		static public Photo GetPhoto (string path)
		{
			if (! HavePhotoStore)
				return null;

			path = Path.GetFullPath (path);
			string dir = Path.GetDirectoryName (path);
			string name = Path.GetFileName (path);
			
			if (! IsPossibleDirectory (dir))
				return null;

			SqliteCommand command = new SqliteCommand ();
			command.Connection = PhotoStoreConnection;
			command.CommandText = String.Format ("SELECT id, description         " +
							     "FROM photos                    " +
							     "WHERE directory_path = \"{0}\" " +
							     "  AND name = \"{1}\"",
							     dir, name);

			Photo photo = null;
			SqliteDataReader reader = command.ExecuteReader ();
			if (reader.Read ()) {
				photo = new Photo ();
				photo.Path = path;
				photo.Id = Convert.ToUInt32 (reader [0]);
				photo.Description = (string) reader [1];
			}

			command.Dispose ();

			return photo;
		}

	}
}
