//
// ContactViewer.cs
//
// Copyright (C) 2006 Pierre Östlund
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
using Mono.Unix;
using Beagle.Util;

namespace ContactViewer {

	public enum ContactManager {
		Thunderbird
	}

	public class ContactViewer {
		
		private static Uri uri = null;
		private static string contact = null;
		
		public static void Main (string[] args)
		{
			Catalog.Init ("beagle", Beagle.Util.ExternalStringsHack.LocaleDir);
			
			SystemInformation.SetProcessName ("beagle-contactviewer");

			ParseArgs (args);

			ContactManager contact_manager;
			try {
				contact_manager = (ContactManager) Enum.Parse (typeof (ContactManager), contact, true);
			} catch {
				Console.WriteLine ("ERROR: '{0}' is not a valid contact manager.", contact);
				Environment.Exit (3);
				return;
			}

			new ContactWindow (contact_manager, uri);
		}
		
		private static void PrintUsageAndExit ()
		{
			Console.WriteLine ("USAGE: beagle-contactviewer --manager <MANAGER>  [OPTIONS] <uri>");
			
			Environment.Exit (0);
		}
		
		private static void ParseArgs (string[] args)
		{
			if (args.Length < 1)
				PrintUsageAndExit ();
			
			for (int i = 0; i < args.Length; i++) {
				switch (args [i]) {
				case "-h":
				case "--help":
					PrintUsageAndExit ();
					break;
				case "--manager":
					contact = args [i + 1];
					i++;
					break;
				default:
					if (args [i].StartsWith ("--")) {
						Console.WriteLine ("WARN: Invalid option {0}", args [i]);
					} else {
						try {
							uri = new Uri (args [i]);
						} catch {
							Console.WriteLine ("ERROR: Invalid URI!");
							Environment.Exit (1);
						}
					}
					break;
				}
			}
			
			if (contact == null) {
				Console.WriteLine ("ERROR: Please specify a valid contact manager.");
				Environment.Exit (2);
			}
		}
	}
}
