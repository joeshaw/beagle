
using System;
using System.Collections;
using System.IO;

using Beagle.Util;
using Beagle;

namespace Bludgeon {
	
	public class FileModel {

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

		//////////////////////////////////////////////////////////////

		private void RecursiveListAdd (ArrayList list, bool add_dirs, bool add_files)
		{
			if ((add_dirs && IsDirectory) || (add_files && IsFile))
				list.Add (this);
			if (children != null)
				foreach (FileModel file in children.Values)
					file.RecursiveListAdd (list, add_files, add_files);
		}

		public ArrayList GetDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, true, true);
			list.RemoveAt (0); // remove ourselves
			return list;
		}

		public ArrayList GetFileDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, false, true);
			list.RemoveAt (0); // remove ourselves
			return list;
		}

		public ArrayList GetDirectoryDescendants ()
		{
			ArrayList list = new ArrayList ();
			RecursiveListAdd (list, true, false);
			list.RemoveAt (0); // remove ourselves
			return list;
		}

		//////////////////////////////////////////////////////////////

		static Random random = new Random ();

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


		//////////////////////////////////////////////////////////////

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
	
