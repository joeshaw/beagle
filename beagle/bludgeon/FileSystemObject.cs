
using System;
using System.Collections;
using System.IO;
using System.Text;

using Beagle.Util;
using Beagle;

/*
  Policy:
  Changes to the file system are made when the object is added to
  a rooted tree.  Roots are automatically instantiated when they
  are created, and are never associated with any events.
*/

namespace Bludgeon {

	abstract public class FileSystemObject {

		const bool SearchInArchives = true;

		private static int next_id = 0;

		private FileSystemObject parent;		
		private string base_name;
		private int id;
		private string name;
		protected Hashtable children;
		protected DateTime timestamp;

		////////////////////////////////////////////////////

		static DateTime base_time;
		const int seconds_per_year = 60 * 60 * 24 * 365;

		static FileSystemObject ()
		{
			base_time = DateTime.Now;
		}

		static public DateTime PickTimestamp ()
		{
			Random random = new Random ();
			return base_time.AddSeconds (- random.Next (seconds_per_year));
		}

		static public void PickTimestampRange (out DateTime a, out DateTime b)
		{
			a = PickTimestamp ();
			b = PickTimestamp ();
			if (b < a) {
				DateTime tmp = a;
				a = b;
				b = tmp;
			}
		}

		protected FileSystemObject ()
		{
			PickName ();
		}

		protected void PickName ()
		{
			id = next_id;
			++next_id;

			base_name = Token.GetRandomWithUnicode ();

			name = null;
		}

		protected void ClearName ()
		{
			base_name = null;
		}

		////////////////////////////////////////////////////

		public FileSystemObject Parent {
			get { return parent; }
		}

		virtual public bool IsRoot {
			get { return false; }
		}

		// Returns true if this object is part of a tree with
		// a root at the top.
		public bool IsRooted {
			get {
				FileSystemObject curr;
				curr = this;
				while (curr != null) {
					if (curr.IsRoot)
						return true;
					curr = curr.Parent;
				}
				return false;
			}
		}

		// By definition, an object is an ancestor of itself.
		public bool IsAncestorOf (FileSystemObject fso)
		{
			if (! this.HasChildren)
				return false;
			while (fso != null) {
				if (this == fso)
					return true;
				fso = fso.Parent;
			}
			return false;
		}

		// Is this an archive?
		public bool IsArchive {
			get {
				return HasChildren && (this is FileObject);
			}
		}

		// Is this FileSystemObject actually a child of an archive?
		public bool IsInArchive {
			get {
				return Parent != null && (Parent is FileObject || Parent.IsInArchive);
			}
		}

		public bool IsReadable {
			get {
				Mono.Unix.Native.Stat stat;

				Mono.Unix.Native.Syscall.stat (Name, out stat);

				if ((stat.st_mode & Mono.Unix.Native.FilePermissions.S_IRUSR) != 0)
					return true;
				else
					return false;
			}
		}

		public bool IsWritable {
			get {
				Mono.Unix.Native.Stat stat;

				Mono.Unix.Native.Syscall.stat (Name, out stat);

				if ((stat.st_mode & Mono.Unix.Native.FilePermissions.S_IWUSR) != 0)
					return true;
				else
					return false;
			}
		}

		virtual public string Name {
			get {
				if (name == null)
					name = String.Format ("{0}.{1}{2}",
							      base_name, id, Extension != null ? Extension : "");
				return name;
			}
		}

		virtual public string ShortName {
			get {
				if (IsRoot)
					return Name;
				FileSystemObject fso = this;
				StringBuilder sb = new StringBuilder ();
				while (fso != null && ! fso.IsRoot) {
					if (sb.Length > 0)
						sb.Insert (0, "/");
					sb.Insert (0, fso.Name);
					fso = fso.Parent;
				}
				return sb.ToString ();
			}
		}

		virtual public string Extension {
			get { return null; }
		}

		virtual protected Uri GetChildUri (FileSystemObject child)
		{
			throw new Exception ("Invalid GetChildUri call");
		}

		virtual public Uri Uri {
			get {
				if (parent == null)
					return new Uri ("floating://" + Name);
				return parent.GetChildUri (this);
			}
		}

		// This should return null for objects that are not directly
		// represented on the file system (i.e. files inside an archive)
		virtual protected string GetChildFullName (FileSystemObject child)
		{
			return Path.Combine (this.FullName, child.Name);
		}

		virtual public string FullName {
			get {
				if (parent == null)
					return Name;
				return parent.GetChildFullName (this);
			}
		}

		abstract public string MimeType { get; }

		public DateTime Timestamp {
			get { return timestamp; }
		}

		public bool HasChildren {
			get { return children != null; }
		}

		public int ChildCount {
			get { return children != null ? children.Count : 0; }
		}

		public ICollection Children {
			get {
				if (children == null)
					throw new Exception ("Invalid request for children on " + Uri.ToString ());
				return children.Values;
			}
		}

		public FileSystemObject GetChild (string name)
		{
			if (children == null)
				throw new Exception ("Invalid request for child '" + name + "' on " + Uri.ToString ());
			return children [name] as FileSystemObject;
		}

		public virtual void AddChild (FileSystemObject child, EventTracker tracker)
		{
			if (children == null)
				throw new Exception ("Can't add a child to " + Uri.ToString ());
			if (child.parent != null)
				throw new Exception ("Can't add parented child " + child.Uri.ToString () + " to " + Uri.ToString ());

			// FIXME: Need to handle the case of the added child
			// clobbering another w/ the same name.

			child.parent = this;
			children [child.Name] = child;
			
			if (IsRooted)
				child.AddOnDisk (tracker);
		}

		public virtual void ClobberingAddChild (FileSystemObject child, FileSystemObject victim, EventTracker tracker)
		{
			if (children == null)
				throw new Exception ("Can't add a child to " + Uri.ToString ());
			if (child.parent != null)
				throw new Exception ("Can't add parented child " + child.Uri.ToString () + " to " + Uri.ToString ());
			if (victim.parent != this)
				throw new Exception ("Victim " + victim.Uri.ToString () + " is not a child of " + Uri.ToString ());
			if (child.Extension != victim.Extension)
				throw new Exception ("Extension mismatch: " + child.Extension + " vs. " + victim.Extension);

			victim.parent = null;
			child.parent = this;
			child.id = victim.id;
			child.base_name = victim.base_name;
			child.name = null;
			children [child.Name] = child;

			if (IsRooted)
				child.AddOnDisk (tracker);
		}

		public virtual void RemoveChild (FileSystemObject child, EventTracker tracker)
		{
			if (child.parent != this)
				throw new Exception (child.Uri.ToString () + " is not a child of " + Uri.ToString ());

			if (IsRooted)
				child.DeleteOnDisk (tracker);

			child.parent = null;
			children.Remove (child.Name);
		}

		public virtual void MoveChild (FileSystemObject child, FileSystemObject new_parent, EventTracker tracker)
		{
			if (child.parent != this)
				throw new Exception (child.Uri.ToString () + " is not a child of " + Uri.ToString ());

			if (new_parent == null || new_parent == child.parent)
				return;

			// We can't move child into new_parent if child is
			// already above new_parent in the tree.
			if (child.IsAncestorOf (new_parent))
				throw new Exception ("Can't move " + child.Uri.ToString () + " to " + new_parent.Uri.ToString ());
			
			string old_full_name;
			old_full_name = child.FullName;

			// FIXME: We need to handle the case of the moved
			// child clobbering another w/ the same name.

			child.parent = new_parent;
			this.children.Remove (child.Name);
			new_parent.children [child.Name] = child;

			// FIXME: What if this is not rooted, but new_parent is?
			if (new_parent.IsRooted)
				child.MoveOnDisk (old_full_name, tracker);
		}

		////////////////////////////////////////////////////

		protected void AllowChildren ()
		{
			children = new Hashtable ();
		}

		////////////////////////////////////////////////////

		// n.b. These shouldn't be public, but they have to be so that directories
		// can manipulate their children.

		// We assume that the FileSystemObject is in the tree when this is called.
		virtual public void AddOnDisk (EventTracker tracker)
		{
			throw new Exception ("AddOnDisk undefined for " + FullName);
		}

		// We assume that the FileSystemObject is still in the tree (has a .parent
		// set, etc.) when we call this.
		virtual public void DeleteOnDisk (EventTracker tracker)
		{
			throw new Exception ("DeleteOnDisk undefined for " + FullName);	
		}

		// We assume that the FileSystemObject is in the tree, it its new position
		// when we call this.
		virtual public void MoveOnDisk (string old_full_name, EventTracker tracker)
		{
			throw new Exception ("MoveOnDisk undefined for " + FullName);
		}

		// This checks that our on-disk state matches our tree state.
		virtual public bool VerifyOnDisk ()
		{
			throw new Exception ("VerifyOnDisk undefined for " + FullName);
		}

		////////////////////////////////////////////////////

		abstract protected bool MatchesQueryPart (QueryPart part);

		// Returns:
		//   1 if it is a match
		//   0 if it doesn't apply
		//  -1 if it doesn't match
		private int MatchesMetadata (QueryPart abstract_part)
		{
			int is_match = 0;

			if (abstract_part is QueryPart_Text) {

				QueryPart_Text part;
				part = (QueryPart_Text) abstract_part;

				if (part.SearchTextProperties && part.Text == base_name)
					is_match = 1;
					
			} else if (abstract_part is QueryPart_Property) {

				QueryPart_Property part;
				part = (QueryPart_Property) abstract_part;

				if (part.Key == "beagle:MimeType") {
					is_match = (part.Value == this.MimeType) ? 1 : -1;
				} else if (part.Key == "beagle:Filename") {
					is_match = (part.Value == base_name) ? 1 : -1;
				} else if (part.Key == "beagle:ExactFilename") {
					is_match = (part.Value == Name) ? 1 : -1;
				}
					
			} else if (abstract_part is QueryPart_DateRange) {

				QueryPart_DateRange part;
				part = (QueryPart_DateRange) abstract_part;

				is_match = (part.StartDate <= Timestamp && Timestamp <= part.EndDate) ? 1 : -1;
			}

			return is_match;
		}

		virtual public bool MatchesQuery (Query query)
		{
			foreach (QueryPart abstract_part in query.Parts) {

				int is_match = 0;
				
				// Note that this works because we don't
				// allow nested or queries.
				if (abstract_part is QueryPart_Or) {
					QueryPart_Or part;
					part = (QueryPart_Or) abstract_part;

					is_match = -1;
					foreach (QueryPart sub_part in part.SubParts) {
						if (MatchesMetadata (sub_part) == 1
						    || MatchesQueryPart (sub_part)) {
							is_match = 1;
							break;
						}
					}
				} else {
					// Handle certain query parts related to file system metadata.
					is_match = MatchesMetadata (abstract_part);
				
					if (is_match == 0)
						is_match = MatchesQueryPart (abstract_part) ? 1 : -1;
				}

				if (abstract_part.Logic == QueryPartLogic.Prohibited)
					is_match = - is_match;
				
				if (is_match < 0)
					return false;
				else if (is_match == 0)
					throw new Exception ("This will never happen");
			}
			
			return true;
		}

		private void DoRecursiveQuery (Query query, ArrayList matches)
		{
			if (this.MatchesQuery (query))
				matches.Add (this);

			if (IsArchive && !SearchInArchives)
				return;

			if (this.HasChildren)
				foreach (FileSystemObject child in this.Children)
					child.DoRecursiveQuery (query, matches);
		}

		public ICollection RecursiveQuery (Query query)
		{
			ArrayList matches;
			matches = new ArrayList ();
			DoRecursiveQuery (query, matches);
			return matches;
		}
	}
}
