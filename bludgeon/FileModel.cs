
using System;
using System.IO;

using Beagle.Util;

namespace Bludgeon {
	
	public class FileModel {

		static private int global_seqno = 1;

		private int seqno;
		private int name_token = -1;
		private int [] body;

		private string name;
		private string dir;
		private string path;
		private Uri uri;

		//////////////////////////////////////////////////////////////

		public int SequenceNumber {
			get { return seqno; }
		}

		public string Name {
			get { return name; }
		}

		public string FullName {
			get { return path; }
		}

		public Uri Uri {
			get { return uri; }
		}

		public int [] Body {
			get { return body; }
		}

		public int NameToken {
			get { return name_token; }
		}

		public bool BodyContains (int id)
		{
			// FIXME: Do a binary search (or something smarter)
			// instead
			for (int i = 0; i < body.Length; ++i)
				if (body [i] == id)
					return true;
			return false;
		}

		public bool Contains (int id)
		{
			return (id == name_token) || BodyContains (id);
		}

		//////////////////////////////////////////////////////////////

		private FileModel ()
		{
			seqno = global_seqno;
			++global_seqno;

			name_token = Token.GetRandom ();

			body = new int [10];
			for (int i = 0; i < body.Length; ++i)
				body [i] = Token.GetRandom ();
			Array.Sort (body);

			// A reasonable default
			SetDirectory (PathFinder.HomeDir);
		}

		private void SetDirectory (string dir)
		{
			this.dir = dir;
			
			if (name_token != -1)
				this.name = String.Format ("{0}-{1}", seqno, Token.GetString (name_token));
			else
				this.name = seqno.ToString ();

			this.path = Path.Combine (this.dir, this.name);
			
			this.uri = UriFu.PathToFileUri (this.path);
		}

		private void Write ()
		{
			TextWriter writer;
			writer = new StreamWriter (FullName);

			for (int i = 0; i < body.Length; ++i)
				writer.WriteLine (Token.GetString (body [i]));

			writer.Close ();
		}

		//////////////////////////////////////////////////////////////
		
		static public FileModel Create ()
		{
			FileModel file;
			file = new FileModel ();
			file.Write ();
			return file;
		}
	}	
}
	
