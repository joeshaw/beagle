//
// RemovableIndexTool.cs
// Single tool to create, load and unload removable indexes
//
// Copyright (C) 2008 D Bera <dbera.web@gmail.com>
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
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Beagle;
using Beagle.Util;

// Assembly information
[assembly: AssemblyTitle ("beagle-removable-index")]
[assembly: AssemblyDescription ("Command-line interface for managing Beagle indexes for removable media")]

public class RemovableIndexTool {
	private static void PrintUsage ()
	{
		VersionFu.PrintHeader ();

		string usage =
			"Usage: beagle-removable-index [OPTIONS]\n\n" +
			"Options:\n" +
			"  --mount PATH\tTell beagled to load a removable index from the given directory.\n" +
			"  --umount PATH\tTell beagled to unload a removable index from the given directory.\n" +
			"               \tGive full path to the directory in two options above.\n" +
			"  --help\t\tPrint this usage message.\n" +
			"  --version\t\tPrint version information.\n";

		Console.WriteLine (usage);
	}

	[DllImport("libgobject-2.0.so.0")]
	static extern void g_type_init ();

	public static void Main (string[] args) 
	{
		// Initialize GObject type system
		g_type_init ();

		int i = 0;
		while (i < args.Length) {
			
			string arg = args [i];
			++i;
			string next_arg = i < args.Length ? args [i] : null;

			switch (arg) {
			case "-h":
			case "--help":
				PrintUsage ();
				Environment.Exit (0);
				break;

			case "--mount":
				if (next_arg != null)
					MountRemovableIndex (next_arg);
				else
					PrintUsage ();
				++ i;
				break;

			case "--unmount":
				if (next_arg != null)
					UnmountRemovableIndex (next_arg);
				else
					PrintUsage ();
				++ i;
				break;

			case "--version":
				VersionFu.PrintVersion ();
				Environment.Exit (0);
				break;

			default:
				PrintUsage ();
				Environment.Exit (1);
				break;

			}
		}
	}

	private static void MountRemovableIndex (string path)
	{
		Console.WriteLine ("Loading removable index from '{0}'", path);

		RemovableIndexRequest req = new RemovableIndexRequest ();
		req.Mount = true;
		req.Path = Path.IsPathRooted (path) ? path : Path.GetFullPath (path);

		ResponseMessage resp;
		
		try {
			resp = req.Send ();
		} catch (ResponseMessageException ex) {
			Log.Error (ex, "Error in loading index.");
			return;
		}

		RemovableIndexResponse res = (RemovableIndexResponse) resp;
		Console.WriteLine ("Successfully added source '{0}' from {1}", res.Source, path);
	}

	private static void UnmountRemovableIndex (string path)
	{
		Console.WriteLine ("Unloading removable index from '{0}'", path);

		RemovableIndexRequest req = new RemovableIndexRequest ();
		req.Mount = false;
		req.Path = Path.IsPathRooted (path) ? path : Path.GetFullPath (path);

		ResponseMessage resp;
		try {
			resp = req.Send ();
		} catch (ResponseMessageException ex) {
			Log.Error (ex, "Error in unloading index.");
			return;
		}

		RemovableIndexResponse res = (RemovableIndexResponse) resp;
		Console.WriteLine ("Successfully unloaded source '{0}' from {1}", res.Source, path);
	}
}
