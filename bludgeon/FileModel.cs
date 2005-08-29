
using System;
using System.Collections;
using System.IO;

using Beagle.Util;
using Beagle;

namespace Bludgeon {
	
	public class FileModel {

		static Random random = new Random ();		
		static private ArrayList roots = new ArrayList ();

		static public ICollection Roots { get { return roots; } }

		static public void AddRoot (FileModel root)
		{
			if (! root.IsRoot)
				throw new Exception ("Attempted to add non-root as root");

			roots.Add (root);
		}

		static public FileModel PickRoot ()
		{
			if (roots.Count == 0)
				return null;
			else if (roots.Count == 1)
				return roots [0] as FileModel;

			return roots [random.Next (roots.Count)] as FileModel;
		}
		
		//
		// Note that the probability of a FileModel being picked
		// by any of these "picker" methods is not uniform.
		// The two-step process where we first pick a root messes
		// that up.
		//


		// Can be either a file or directory
		static public FileModel PickNonRoot ()
		{
			FileModel root;
			root = PickRoot ();
			return root.PickDescendant ();
		}

		static public FileModel PickFile ()
		{
			FileModel root;
			root = PickRoot ();
			return root.PickFileDescendant ();
		}

		static public FileModel PickAnyDirectory ()
		{
			FileModel root;
			root = PickRoot ();
			return root.PickDirectory ();
		}

		static public FileModel PickNonRootDirectory ()
		{
			FileModel root;
			root = PickRoot ();
			return root.PickDirectoryDescendant ();
		}

		//////////////////////////////////////////////////////////////

		// Properties of the root directory:
		// name contains full path of BEAGLE_HOME
		// parent is null
		// body is null
		// children is non-null

		// Properties of a non-root directory:
		// name is a token
		// parent is non-null
		// body is null
		// children is non-null

		// Properties of a file:
		// name is a token
		// parent is non-null, and is a directory
		// body is non-null
		// children is null

		private string name = null;
		private FileModel parent = null;
		private string [] body = null;
		private Hashtable children = null;

		//////////////////////////////////////////////////////////////

		public bool IsRoot {
			get { return parent == null; }
		}

		public bool IsDirectory {
			get { return body == null; }
		}

		public bool IsFile {
			get { return body != null; }
		}

		public string Name {
			get { return name; }
		}
		
		public string FullName {
			get { return IsRoot ? Name : Path.Combine (parent.FullName, name); }
		}

		public Uri Uri {
			get { return UriFu.PathToFileUri (FullName); }
		}

		public string [] Body {
			get { return body; }
		}

		public FileModel Parent {
			get { return parent; }
		}

		public ICollection Children {
			get { return children.Values; }
		}

		public int Size {
			get {
				if (IsFile)
					return 1;
				int sum = 0;
				if (IsDirectory)
					sum = 1; // count ourselves
				foreach (FileModel child in children.Values)
					sum += child.Size;
				return sum;
			}
		}

		// IsAbove is easier to think about than IsAncestorOf
		public bool IsAbove (FileModel other)
		{
			return this == other.parent 
				|| (other.parent != null && IsAbove (other.parent));
		}
		
		// IsBelow is easier to thik about than IsDescendantOf
		public bool IsBelow (FileModel other)
		{
			return other.IsAbove (this);
		}

		//////////////////////////////////////////////////////////////

		private void RecursiveListAdd (ArrayList list, 
					       bool add_self,
					       bool add_dirs, 
					       bool add_files)
		{
			if (add_self && ((add_dirs && IsDirectory) || (add_files && IsFile)))
				list.Add (this);

			if (children != null)
				foreach (FileModel file in children.Values)
					file.RecursiveListAdd (list, true, add_dirs, add_files);
		}

		public ArrayList GetDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, false, true, true);
			return list;
		}

		public ArrayList GetFileDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, false, false, true);
			return list;
		}

		// Includes ourself
		public ArrayList GetDirectories ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, true, true, false);
			return list;
		}

		// Never includes ourself
		public ArrayList GetDirectoryDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, false, true, false);
			return list;
		}

		//////////////////////////////////////////////////////////////

		public FileModel PickDescendant ()
		{
			ArrayList all;
			all = GetDescendants ();
			if (all.Count == 0)
				return null;
			return all [random.Next (all.Count)] as FileModel;
		}

		public FileModel PickFileDescendant ()
		{
			ArrayList all;
			all = GetFileDescendants ();
			if (all.Count == 0)
				return null;
			return all [random.Next (all.Count)] as FileModel;
		}

		public FileModel PickDirectoryDescendant ()
		{
			ArrayList all;
			all = GetDirectoryDescendants ();
			if (all.Count == 0)
				return null;
			return all [random.Next (all.Count)] as FileModel;
		}

		// This can return the root
		public FileModel PickDirectory ()
		{
			ArrayList all;
			all = GetDirectories ();
			return all [random.Next (all.Count)] as FileModel;
		}

		//////////////////////////////////////////////////////////////

		public bool BodyContains (string token)
		{
			if (body == null)
				return false;

			// FIXME: Do a binary search (or something smarter)
			// instead
			for (int i = 0; i < body.Length; ++i)
				if (body [i] == token)
					return true;
			return false;
		}

		public bool Contains (string token)
		{
			return name == token || BodyContains (token);
		}

		//////////////////////////////////////////////////////////////

		private FileModel () { }

		public static FileModel NewRoot ()
		{
			FileModel root = new FileModel ();
			root.name = PathFinder.HomeDir;
			root.children = new Hashtable ();
			return root;
		}

		// Creates a randomly-named new directory.
		// Avoid name collisions with existing files.
		public FileModel NewDirectory ()
		{
			if (! IsDirectory)
				throw new ArgumentException ("parent must be a directory");

			// no more names left
			if (children.Count == Token.Count)
				return null;

			FileModel child;
			child = new FileModel ();
			child.name = PickName (this);
			child.children = new Hashtable ();

			child.parent = this;
			children [child.name] = child;

			// Actually create the directory
			Directory.CreateDirectory (child.FullName);

			return child;
		}

		public FileModel NewFile ()
		{
			if (! IsDirectory)
				throw new ArgumentException ("parent must be a directory");

			// no more names left
			if (children.Count == Token.Count)
				return null;

			FileModel child;
			child = new FileModel ();
			child.name = PickName (this);
			child.body = NewBody (10);

			child.parent = this;
			children [child.name] = child;

			// Create the file
			child.Write ();

			return child;
		}

		//////////////////////////////////////////////////////////////

		// Mutate the tree

		public void Grow (int depth)
		{
			const int num_dirs = 2;
			const int num_files = 5;

			if (depth > 0) {
				for (int i = 0; i < num_dirs; ++i) {
					FileModel file;
					file = NewDirectory ();
					if (file != null)
						file.Grow (depth - 1);
				}
			}
			
			for (int i = 0; i < num_files; ++i)
				NewFile ();
		}

		//////////////////////////////////////////////////////////////

		// Basic file system operations

		public void Touch ()
		{
			if (IsFile) {
				body = NewBody (10);
				Write ();
			}
		}

		public void Delete ()
		{
			if (IsRoot)
				throw new Exception ("Can't delete the root!");

			if (IsDirectory)
				Directory.Delete (FullName, true); // recursive
			else
				File.Delete (FullName);

			parent.children.Remove (name);
			parent = null;
		}

		// If the move would cause a filename collision or is
		// otherwise impossible, return false.  Return true if the
		// move actually happens.
		public bool MoveTo (FileModel new_parent, // or null, to just rename
				    string    new_name)   // or null, to just move
		{
			if (! new_parent.IsDirectory)
				throw new ArgumentException ("Parent must be a directory");

			if (this.IsRoot)
				throw new ArgumentException ("Can't move a root");

			// Impossible
			if (this == new_parent || this.IsAbove (new_parent))
				return false;

			string old_path;
			old_path = this.FullName;

			if (new_parent == null)
				new_parent = this.parent;
			if (new_name == null)
				new_name = this.name;

			// check for a filename collision
			if (new_parent.children.Contains (new_name))
				return false;

			// modify the data structure
			this.parent.children.Remove (this.name);
			this.parent = new_parent;
			this.name = new_name;
			this.parent.children [this.name] = this;
			
			string new_path;
			new_path = Path.Combine (new_parent.FullName, new_name);

			if (this.IsDirectory)
				Directory.Move (old_path, new_path);
			else
				File.Move (old_path, new_path);

			return true;
		}


		//////////////////////////////////////////////////////////////

		// Useful utility functions

		static private string PickName (FileModel p)
		{
			string pick;
			do {
				pick = Token.GetRandom ();
			} while (p.children.Contains (pick));
			return pick;
		}

		static private string [] NewBody (int size)
		{
			string [] body;
			body = new string [size];
			for (int i = 0; i < size; ++i)
				body [i] = Token.GetRandom ();
			Array.Sort (body);
			return body;
		}

		private void Write ()
		{
			TextWriter writer;
			writer = new StreamWriter (FullName);
			for (int i = 0; i < body.Length; ++i)
				writer.WriteLine (body [i]);
			writer.Close ();
		}

		//////////////////////////////////////////////////////////////

		//
		// Code to determine a a file will match a particular query
		//

		private bool MatchesQueryPart (QueryPart abstract_part)
		{
			bool is_match;
			is_match = false;

			if (abstract_part is QueryPart_Text) {
				QueryPart_Text part;
				part = abstract_part as QueryPart_Text;

				if ((part.SearchTextProperties && Name == part.Text)
				    || (part.SearchFullText && BodyContains (part.Text)))
					is_match = true;

			} else if (abstract_part is QueryPart_Or) {
				QueryPart_Or part;
				part = abstract_part as QueryPart_Or;

				foreach (QueryPart sub_part in part.SubParts) {
					if (MatchesQueryPart (sub_part)) {
						is_match = true;
						break;
					}
				}
			} else if (abstract_part is QueryPart_Property) {
				QueryPart_Property part;
				part = abstract_part as QueryPart_Property;

				if (part.Key == "beagle:MimeType") {
					if (part.Value == "inode/directory")
						is_match = IsDirectory;
					else if (part.Value == "text/plain")
						is_match = IsFile;
					else
						is_match = false;
				} else if (part.Key == "beagle:ExactFilename") {
					is_match = (Name == part.Value);
				} else {
					throw new Exception ("Unsupported property " + part.Key);
				}
			} else {
				throw new Exception ("Unsupported part");
			}

			if (abstract_part.Logic == QueryPartLogic.Prohibited)
				is_match = ! is_match;

			return is_match;
		}

		public bool MatchesQuery (Query query)
		{
			// We assume the root node never matches any query.
			if (IsRoot)
				return false;

			foreach (QueryPart part in query.Parts) {
				if (! MatchesQueryPart (part))
					return false;
			}
			
			return true;
		}

		private void RecursiveQueryCheck (ArrayList match_list, Query query)
		{
			if (MatchesQuery (query))
				match_list.Add (this);

			if (children != null)
				foreach (FileModel file in children.Values)
					file.RecursiveQueryCheck (match_list, query);
		}

		public ArrayList GetMatchingDescendants (Query query)
		{
			ArrayList match_list;
			match_list = new ArrayList ();
			RecursiveQueryCheck (match_list, query);
			return match_list;
		}
	}	
}
	
