
using System;
using System.Collections;
using System.IO;

using Mono.Unix.Native;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class DirectoryObject : FileSystemObject {

		private bool is_root = false;
		private string root_path = null;
		
		int n_files_below = 0;
		int n_dirs_below = 0;

		public DirectoryObject ()
		{
			timestamp = FileSystemObject.PickTimestamp ();
			AllowChildren ();
		}

		public DirectoryObject (string root_path)
		{
			ClearName ();

			this.is_root = true;
			this.root_path = Path.GetFullPath (root_path);

			AllowChildren ();
			AddOnDisk (null);
		}

		override public bool IsRoot {
			get { return is_root; }
		}

		override public string Name {
			get {
				if (is_root)
					return root_path;
				return base.Name;
			}
		}

		override protected string GetChildUri (FileSystemObject child)
		{
			return Path.Combine (this.Uri, child.Name);
		}

		override public string Uri {
			get {
				if (is_root)
					return "file://" + root_path;
				return base.Uri;
			}
		}

		override public string Extension {
			get { return is_root ? null : ".dir"; }
		}

		override public string MimeType {
			get { return "directory/inode"; }
		}

		///////////////////////////////////////////////////////////////////////

		static private void GetCountDeltas (FileSystemObject fso, out int d_files, out int d_dirs)
		{
			if (fso is DirectoryObject) {
				DirectoryObject dir = (DirectoryObject) fso;
				d_files = dir.n_files_below;
				d_dirs = dir.n_dirs_below + 1; // add one for ourself
			} else if (fso is FileObject) {
				d_files = 1; // just ourself
				d_dirs = 0;
			} else {
				throw new Exception ("Unknown type " + fso);
			}
		}

		public override void AddChild (FileSystemObject child, EventTracker tracker)
		{
			// Every time we add a child, we walk up the tree
			// and adjust the n_*_below counts for every node
			// above us.

			int d_files, d_dirs;
			GetCountDeltas (child, out d_files, out d_dirs);

			DirectoryObject curr;
			curr = this;
			while (curr != null) {
				curr.n_files_below += d_files;
				curr.n_dirs_below += d_dirs;
				curr = (DirectoryObject) curr.Parent;
			}
				
			base.AddChild (child, tracker);
		}

		public override void RemoveChild (FileSystemObject child, EventTracker tracker)
		{
			// Likewise, we have to walk up the tree and adjust
			// the n_*_below counts when we remove a child.

			int d_files, d_dirs;
			GetCountDeltas (child, out d_files, out d_dirs);

			DirectoryObject curr;
			curr = this;
			while (curr != null) {
				curr.n_files_below -= d_files;
				curr.n_dirs_below -= d_dirs;
				curr = (DirectoryObject) curr.Parent;
			}

			base.RemoveChild (child, tracker);
		}

		public override void MoveChild (FileSystemObject child, FileSystemObject new_parent, EventTracker tracker)
		{
			int d_files, d_dirs;
			GetCountDeltas (child, out d_files, out d_dirs);

			DirectoryObject curr;
			curr = this;
			while (curr != null) {
				curr.n_files_below -= d_files;
				curr.n_dirs_below -= d_dirs;
				curr = (DirectoryObject) curr.Parent;
			}
			curr = (DirectoryObject) new_parent;
			while (curr != null) {
				curr.n_files_below += d_files;
				curr.n_dirs_below += d_dirs;
				curr = (DirectoryObject) curr.Parent;
			}
			
			base.MoveChild (child, new_parent, tracker);
		}

		///////////////////////////////////////////////////////////////////////

		override public void AddOnDisk (EventTracker tracker)
		{
			string full_name;
			full_name = FullName;
			if (full_name == null)
				throw new Exception ("Attempt to instantiate something other than a real file: " + Uri);

			if (is_root) {
				// Root directories must already exist.
				if (! Directory.Exists (full_name))
					throw new Exception ("Missing root directory " + full_name);
			} else {
				Directory.CreateDirectory (full_name);
				timestamp = Directory.GetLastWriteTime (full_name);
			}

			if (tracker != null)
				tracker.ExpectingAdded (this.Uri);

			// Recursively add the children
			foreach (FileSystemObject fso in children.Values)
				fso.AddOnDisk (tracker);
		}

		override public void DeleteOnDisk (EventTracker tracker)
		{
			// Recursively delete the children
			foreach (FileSystemObject fso in children.Values)
				fso.DeleteOnDisk (tracker);

			// Then delete ourselves
			Syscall.rmdir (FullName);
		}

		override public void MoveOnDisk (string old_full_name, EventTracker tracker)
		{
			Syscall.rename (old_full_name, FullName);
		}

		override public bool VerifyOnDisk ()
		{
			// Make sure the directory exists.
			if (! Directory.Exists (FullName)) {
				Log.Failure ("Missing directory '{0}'", FullName);
				return false;
			}

			// Make sure all of the children exist.
			Hashtable name_hash = new Hashtable ();
			foreach (FileSystemObject fso in children.Values) {
				name_hash [fso.FullName] = fso;
				if (! fso.VerifyOnDisk ())
					return false;
			}

			// Make sure there is nothing in the directory that shouldn't be there.
			foreach (string name in Directory.GetFileSystemEntries (FullName)) {
				name_hash.Remove (name);
			}
			if (name_hash.Count > 0) {
				Log.Failure ("Extra items in directory '{0}'", FullName);
				foreach (string name in name_hash.Keys)
					Log.Failure ("   extra item: '{0}'", name);
				return false;
			}

			return true;
		}

		///////////////////////////////////////////////////////////////////////

		// All of the query parts than can possibly match a directory are handled
		// in FileSystemObject.MatchesQuery.
		override protected bool MatchesQueryPart (QueryPart abstract_part)
		{
			return false;
		}

		///////////////////////////////////////////////////////////////////////

		static Random random = new Random ();

		public FileObject PickChildFile ()
		{
			if (n_files_below == 0)
				return null;

			int i;
			i = random.Next (n_files_below);

			foreach (FileSystemObject fso in Children) {
				if (fso is FileObject) {
					if (i == 0)
						return (FileObject) fso;
					--i;
				} else if (fso is DirectoryObject) {
					int nfb;
					nfb = ((DirectoryObject) fso).n_files_below;
					if (i < nfb)
						return ((DirectoryObject) fso).PickChildFile ();
					i -= nfb;
				}
			}

			throw new Exception ("This shouldn't happen!");
		}

		public DirectoryObject PickChildDirectory ()
		{
			if (n_dirs_below == 0)
				return null;

			int i;
			i = random.Next (n_dirs_below);

			foreach (FileSystemObject fso in Children) {
				
				DirectoryObject dir = fso as DirectoryObject;
				if (dir == null)
					continue;
				
				if (i == 0)
					return dir;
				--i;

				if (i < dir.n_dirs_below)
					return dir.PickChildDirectory ();
				i -= dir.n_dirs_below;
			}

			throw new Exception ("This also shouldn't happen!");
		}
		
		// Returns a directory, either a child directory or itself.
		public DirectoryObject PickDirectory ()
		{
			int i;
			i = random.Next (n_dirs_below + 1);
			if (i == 0)
				return this;
			return PickChildDirectory ();
		}
	}
}
