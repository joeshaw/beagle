//
// SanityCheck.cs
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

using Beagle.Util;

namespace Beagle.Daemon.FileSystemQueryable {

	public class SanityCheck {

		public class ExpectedObject {
			public string Name;
			public Guid   UniqueId;
			public bool   IsFile;

			public bool IsDirectory { get { return ! IsFile; } }
		}

		public enum DefectType {
			Missing,   // An object is missing from the index
			Extra,     // There is an object in the index that shouldn't be there
			WrongName, // An object in the index is misnamed, but otherwise correct
			Broken     // None of the above, but something is very wrong
		}

		public class Defect {

			public Defect (DefectType type)
			{
				Type = type;
			}

			public Defect (DefectType type, ExpectedObject expected)
			{
				Type      = type;
				TrueName  = expected.Name;
				UniqueId  = expected.UniqueId;
				IsFile    = expected.IsFile;

				if (type != DefectType.WrongName)
					IndexName = expected.Name;
			}

			public DefectType Type;

			public string     TrueName;  // name on disk
			public string     IndexName; // name in index

			public Guid       UniqueId;

			public bool       IsFile;
			public bool       IsDirectory { get { return ! IsFile; } }

			public string     Comment;   // a human-readable note
		}


		static public ICollection GetExpectedObjects (string              directory_to_check,
							      FileAttributesStore file_attr_store)
		{
			// FIXME: please implement this
			return null;
		}

		static public ICollection CheckDirectory (string              directory_to_check,
							  ICollection         list_of_expected_objects,
							  FileSystemModel     model)
		{
			// Convenient abbreviations
			UniqueIdStore unique_id_store = model.UniqueIdStore;
			NameIndex name_index = model.NameIndex;

			ArrayList defects = new ArrayList ();

			//
			// We start up by pulling a bunch of information out of our
			// models and stuffing it into various data structures.
			//

			FileSystemModel.Directory dir;
			dir = model.GetDirectoryByPath (directory_to_check);

			// We don't have any record of this directory in the model,
			// just return null --- this is a lost cause.
			if (dir == null) 
				return null;

			// Pull all of the uid information for this directory, and
			// put them in a hashtable keyed off the uids.
			// And while we are at it, write the uids into an array
			// to be used when calling NameIndex.GetManyByUniqueId.
			ICollection unique_id_records;
			unique_id_records = unique_id_store.GetRecordsByParentId (dir.UniqueId);

			Hashtable unique_id_hash = new Hashtable ();
			Guid [] all_unique_ids = new Guid [unique_id_records.Count];

			int i = 0;
			foreach (UniqueIdStore.Record uid_rec in unique_id_records) {
				unique_id_hash [uid_rec.UniqueId] = uid_rec;
				all_unique_ids [i++] = uid_rec.UniqueId;
			}


			// Get all of the necessary records out of the name index.
			NameIndex.Record [] name_index_records;
			name_index_records = name_index.GetManyByUniqueId (all_unique_ids);


			//
			// OK, time to get to work.  First we do sanity checks between our
			// various bits of internal data.
			//

			// Check that the NameIndex records contain the same names as the UniqueIdStore
			// records.
			if (name_index_records.Length != unique_id_records.Count) {
				// We must be missing some name index records!
				// FIXME: Generate a defect
				Logger.Log.Debug ("name_index_records.Length != unique_id_records.Count ({0} vs. {1})",
						  name_index_records.Length, unique_id_records.Count);
			}
			for (i = 0; i < name_index_records.Length; ++i) {
				UniqueIdStore.Record uid_rec;
				uid_rec = unique_id_hash [name_index_records [i].UniqueId] as UniqueIdStore.Record;
				if (uid_rec == null) {
					// In theory this shouldn't happen, because of the previous check,
					// unless multiple documents w/ the same uid ended up in the the NameIndex.
					// FIXME: Generate a defect
					Logger.Log.Debug ("NameIndex record {0} '{1}' has no corresponding UniqueIdStore record",
							  GuidFu.ToShortString (name_index_records [i].UniqueId),
							  name_index_records [i].Name);
				} else if (uid_rec.Name != name_index_records [i].Name) {
					// FIXME: Generate a defect
					Logger.Log.Debug ("NameIndex/UniqueIdStore name mismatch for {0}: '{1}' vs. '{2}'",
							  GuidFu.ToShortString (uid_rec.UniqueId),
							  uid_rec.Name, name_index_records [i].Name);
				}
			}

			// Make sure that the child info in the FileSystemModel matches up with 
			// the UniqueIdStore.
			foreach (FileSystemModel.Directory subdir in dir.Children) {
				UniqueIdStore.Record uid_rec;
				uid_rec = unique_id_hash [subdir.UniqueId] as UniqueIdStore.Record;
				if (uid_rec == null) {
					// FIXME: Generate a defect?  Is this a bug in the code?
					Logger.Log.Debug ("FileSystemModel object {0} '{1}' is not in the UniqueIdStore",
							  GuidFu.ToShortString (subdir.UniqueId),
							  subdir.FullName);
				} else if (uid_rec.Name != subdir.Name) {
					// FIXME: Generate a defect
					Logger.Log.Debug ("FileSystemModel/UniqueIdStore name mismatch for {0}: '{1}' vs. '{2}'",
							  GuidFu.ToShortString (uid_rec.UniqueId),
							  uid_rec.Name, subdir.Name);
				}
			}

			// If the list of expected objects is null, just return
			// the defects we found during these internal sanity checks.
			if (list_of_expected_objects == null)
				return defects;


			//
			// Now compare our internal state with the expected objects.
			//

			// Walk across the list of expected objects, checking each
			// against the UniqueIdStore and FileSystemModel.
			foreach (ExpectedObject expected in list_of_expected_objects) {
				UniqueIdStore.Record uid_rec;
				uid_rec = unique_id_hash [expected.UniqueId] as UniqueIdStore.Record;
				if (uid_rec == null) {
					// Maybe we think it is in a different directory?
					string path = unique_id_store.GetPathById (expected.UniqueId);
					if (path != null) {
						// FIXME: generate a defect
						Logger.Log.Debug ("UniqueIdStore indicates that expected object {0} '{1}' is at path {2}",
								  GuidFu.ToShortString (expected.UniqueId),
								  expected.Name, path);
					} else {
						// FIXME: generate a defect
						Logger.Log.Debug ("No UniqueIdStore record for expected object {0} '{1}'",
								  GuidFu.ToShortString (expected.UniqueId),
								  expected.Name);
					}
				} else if (uid_rec.Name != expected.Name) {
					// FIXME: Generate a defect
					Logger.Log.Debug ("Expected/UniqueIdStore name mismatch for {0}: '{1}' vs. '{2}'",
							  GuidFu.ToShortString (expected.UniqueId),
							  uid_rec.Name, expected.Name);
				}

				if (uid_rec != null)
					unique_id_hash.Remove (uid_rec.UniqueId);

				// If the expected object is a directory, make sure it is in the FileSystemModel.
				if (expected.IsDirectory && ! dir.HasChildWithName (expected.Name)) {
					// FIXME: Generate a defect
					Logger.Log.Debug ("Expected directory {0} '{1}' not in FileSystemModel",
							  GuidFu.ToShortString (expected.UniqueId), expected.Name);
				}
			}
			
			
			return defects;
		}
	}
}

