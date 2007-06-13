//
// VersionFu.cs: A utility class for version information
//
// Copyright (C) 2007 Lukas Lipka
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
using System.Reflection;
using System.Collections;

namespace Beagle.Util {
	
	public static class VersionFu {
		
		public static void PrintHeader (Assembly a)
		{
			// Retrieves name and description which should always
			// be stored in AssemblyInfo.cs
			AssemblyTitleAttribute title = (AssemblyTitleAttribute) Attribute.GetCustomAttribute (a, typeof (AssemblyTitleAttribute));
			AssemblyDescriptionAttribute desc = (AssemblyDescriptionAttribute) Attribute.GetCustomAttribute (a, typeof (AssemblyDescriptionAttribute));

			string info = "";
			
			if (title != null && desc != null)
				info += String.Format ("{0}: {1}.\n", title.Title, desc.Description);

			info +=
				"Web page: http://beagle-project.org\n" +
				"Copyright (C) 2004-2007 Novell, Inc.\n";

			Console.WriteLine (info);
		}

		public static void PrintHeader ()
		{
			// Gets the calling assembly of which the information
			// we want to get
			Assembly assembly = Assembly.GetCallingAssembly ();

			PrintHeader (assembly);
		}

		public static void PrintVersion ()
		{
			// Gets the calling assembly of which the information
			// we want to get
			Assembly assembly = Assembly.GetCallingAssembly ();

			PrintHeader (assembly);
			
			Console.WriteLine ("Beagle: " + ExternalStringsHack.Version);
			Console.WriteLine ("Mono: " + SystemInformation.MonoRuntimeVersion);
			Console.WriteLine ("Sqlite: " + ExternalStringsHack.SqliteVersion);
		}
	}
}
