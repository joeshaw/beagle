//
// ExerciseFileSystem.cs
//
// Copyright (C) 2005 Novell, Inc.
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

using CommandLineFu;
using Beagle.Util;

class ExerciseFileSystemTool {

	[Option (LongName="root")]
	static string root = null;

	[Option (LongName="file-source")]
	static string file_source = null;

	[Option (LongName="max-files")]
	static int max_files = 20;

	[Option (LongName="max-directories")]
	static int max_directories = 5;

	[Option (LongName="depth")]
	static int max_depth = 4;

	[Option (LongName="action-count")]
	static int action_count = 0;

	[Option (LongName="max-interval")]
	static int max_interval = 1000;

	static Random random = new Random ();

	static ArrayList all_directories = new ArrayList ();
	static ArrayList all_files = new ArrayList ();

	static ArrayList available_source_files = null;
	static string GetRandomSourceFile ()
	{
		if (file_source == null)
			return null;
		if (available_source_files == null) {
			available_source_files = new ArrayList ();
			DirectoryInfo dir = new DirectoryInfo (file_source);
			foreach (FileInfo file in dir.GetFiles ()) {
				if (file.Name.StartsWith (".")
				    || file.Name.EndsWith ("~")
				    || (file.Name.StartsWith ("#") && file.Name.EndsWith ("#")))
					continue;
				available_source_files.Add (file.FullName);
			}
		}
		if (available_source_files.Count == 0)
			return null;

		return (string) available_source_files [random.Next (available_source_files.Count)];
	}

	static int name_counter = 0;
	static string GetRandomName ()
	{
		++name_counter;
		return name_counter.ToString ();
	}

	static int file_count = 0;
	static int directory_count = 0;

	static string CreateFile (string directory)
	{
		string source_file = GetRandomSourceFile ();
		string new_file = Path.Combine (directory, GetRandomName ());

		if (source_file != null) {
			new_file += Path.GetExtension (source_file);
			File.Copy (source_file, new_file);
		} else {
			File.Create (new_file).Close ();
		}

		all_files.Add (new_file);
		return new_file;
	}

	static void CreateDirectories (string parent, int depth)
	{
		string path = Path.Combine (parent, GetRandomName ());
		Directory.CreateDirectory (path);

		all_directories.Add (path);

		int n_files = random.Next (max_files+1);
		for (int i = 0; i < n_files; ++i)
			CreateFile (path);

		if (depth < max_depth) {
			int n_dirs = random.Next (max_directories+1);
			if (depth == 0 && n_dirs == 0)
				n_dirs = 1;
			for (int i = 0; i < n_dirs; ++i)
				CreateDirectories (path, depth+1);
		}
	}

	static void DoSomething ()
	{
		if (all_directories.Count == 0) {
			CreateDirectories (root, 0);
			return;
		}

		int random_dir_i = random.Next (all_directories.Count);
		int random_file_i = random.Next (all_files.Count);

		string random_dir = (string) all_directories [random_dir_i];
		string random_file = (string) all_files [random_file_i];

		int action = 0;
		action = random.Next (4);
		switch (action) {

		case 0: // Create new file
			string new_file = CreateFile (random_dir);
			Console.WriteLine ("Created {0}", new_file);
			break;

		case 1: // Delete a file
			File.Delete (random_file);
			all_files.RemoveAt (random_file_i);
			Console.WriteLine ("Deleted {0}", random_file);
			break;

		case 2: // Create new subdirectory
			string path = Path.Combine (random_dir, GetRandomName ());
			Directory.CreateDirectory (path);
			all_directories.Add (path);
			Console.WriteLine ("Created subdirectory {0}", path);
			break;

		case 3: // Recursively delete a subdirectory
			break;
		}
	}
	

	static void Main (string [] args)
	{
		CommandLine.ProgramName = "beagle-exercise-file-system";
		CommandLine.ProgramCopyright = "Copyright (C) 2005 Novell, Inc.";
		args = CommandLine.Process (typeof (ExerciseFileSystemTool), args);
		if (args == null)
			return;

		if (root == null)
			root = Environment.GetEnvironmentVariable ("PWD");
		
		
		if (Directory.Exists (Path.Combine (root, "1")))
			Directory.Delete (Path.Combine (root, "1"), true);
		CreateDirectories (root, 0);

		Console.WriteLine ("Created {0} files across {1} directories",
				   all_files.Count, all_directories.Count);

		for (int i = 0; i < action_count; ++i) {
			DoSomething ();
			int interval = random.Next (max_interval);
			if (interval > 0)
				Thread.Sleep (interval);
		}
	}

}
