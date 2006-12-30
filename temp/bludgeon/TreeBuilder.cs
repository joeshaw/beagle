
using System;
using System.Collections;
using Mono.Unix.Native;

namespace Bludgeon {

	static public class TreeBuilder {

		static string [] possible_extensions = new string [] { ".gz", ".bz2", ".tar", ".zip" };

		static public FileObject NewFile (int    n_directories,
						  int    n_files,
						  string extension,
						  double p_archive,
						  double archive_decay,
						  Random random)
		{
			if (random == null)
				random = new Random ();

			n_directories = (int) Math.Floor (n_directories * archive_decay);
			n_files = (int) Math.Floor (n_files * archive_decay);

			if (n_files == 0 || extension == ".txt" || (extension == null && random.NextDouble () > p_archive))
				return new TextFileObject ();

			if (extension == null)
				extension = possible_extensions [random.Next (possible_extensions.Length)];

			switch (extension) {
			case ".gz":
				FileObject gzipped_file;
				gzipped_file = NewFile (n_directories, n_files, null, p_archive, archive_decay, random);
				return new GzipFileObject (gzipped_file);

			case ".bz2":
				FileObject bzip2ed_file;
				bzip2ed_file = NewFile (n_directories, n_files, null, p_archive, archive_decay, random);
				return new Bzip2FileObject (bzip2ed_file);

			case ".tar":
				DirectoryObject tar_root;
				tar_root = new DirectoryObject ();
				Build (tar_root, n_directories, n_files, p_archive, archive_decay, false, null);
				return new TarFileObject (tar_root);

			case ".zip":
				DirectoryObject zip_root;
				zip_root = new DirectoryObject ();
				Build (zip_root, n_directories, n_files, p_archive, archive_decay, false, null);
				return new ZipFileObject (zip_root);

			}

			throw new Exception ("Something terrible happened!");
		}

		static public void GetAllSubdirectories (DirectoryObject dir, ArrayList target)
		{
			target.Add (dir);
			foreach (FileSystemObject child in dir.Children)
				if (child is DirectoryObject)
					GetAllSubdirectories ((DirectoryObject) child, target);
		}

		static public void Build (DirectoryObject root,
					  int             n_directories,
					  int             n_files,
					  double          p_archive,
					  double          archive_decay,
					  bool            build_in_random_order,
					  EventTracker    tracker)
		{
			//Log.Info ("BUILD {0} {1} {2}", n_directories, n_files, p_archive);
			Random random;
			random = new Random ();

			// First, create the list of all of the directories we could
			// put things in.
			ArrayList all_dirs;
			all_dirs = new ArrayList ();
			GetAllSubdirectories (root, all_dirs);

			int nd = n_directories, nf = n_files;

			// Next, we construct the directories and files.
			while (nd > 0 || nf > 0) {
				
				// If we are not building in a random order,
				// we create all of the directories first.
				bool create_dir;
				if (build_in_random_order)
					create_dir = (random.Next (nd + nf) < nd);
				else
					create_dir = (nd > 0);

				if (create_dir) {
					
					DirectoryObject dir;
					dir = new DirectoryObject ();

					FileSystemObject parent;
					parent = (FileSystemObject) all_dirs [random.Next (all_dirs.Count)];
					parent.AddChild (dir, tracker);
					all_dirs.Add (dir);

					//Log.Spew ("dir {0}: {1}", n_directories - nd, dir.FullName);
					--nd;
					
				} else {

					
					FileObject file;
					file = NewFile (n_directories, n_files, null, p_archive, archive_decay, random);

					FileSystemObject parent;
					parent = (FileSystemObject) all_dirs [random.Next (all_dirs.Count)];
					parent.AddChild (file, tracker);

#if false
					// Commented out because it breaks queries
	
					// 20% of the time make the file unwritable, which prevents us from
					// being able to set extended attributes and makes us fall back to
					// our sqlite store.
					if (random.Next (5) == 0)
						Syscall.chmod (file.FullName, (FilePermissions) 292); // 0444
#endif

					//Log.Spew ("file {0}: {1}", n_files - nf, file.FullName);
					--nf;
				}
			}
		}
	}
}
