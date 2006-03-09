//
// FilterExternal.cs
//
// Copyright (C) 2006 Novell, Inc.
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

// The purpose of this filter is to allow users to easily extract text
// content from structured files using an external program.  See
// external-filters.txt for more information.

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Xml.Serialization;

using Beagle.Daemon;
using Beagle.Util;

namespace Beagle.Filters {

	public class ExternalFilterInfo {

		[XmlElement ("mimetype")]
		public string [] MimeTypes;

		[XmlElement ("extension")]
		public string [] Extensions;

		[XmlElement ("command")]
		public string Command;

		[XmlElement ("arguments")]
		public string Arguments;
	}

	[XmlRoot ("external-filters")]
	public class ExternalFilterInfoContainer {
		[XmlElement ("filter")]
		public ExternalFilterInfo [] Items;
	}

	public class FilterExternal : Beagle.Daemon.Filter {

		private ExternalFilterInfo [] filters = null;

		public FilterExternal ()
		{
			string path = Path.Combine (ExternalStringsHack.SysConfDir, "external-filters.xml");

			try {
				FileStream fs = File.Open (path, FileMode.Open, FileAccess.Read, FileShare.Read);
				XmlSerializer serializer = new XmlSerializer (typeof (ExternalFilterInfoContainer));
				ExternalFilterInfoContainer container = (ExternalFilterInfoContainer) serializer.Deserialize (fs);
				fs.Close ();
				this.filters = container.Items;
			} catch (FileNotFoundException) {
				// Probably not an error if the file isn't there.

			} catch (InvalidOperationException ex) {
				// Something wrong with the XML
				Logger.Log.Error ("Unable to parse {0}: {1}", path, ex.Message);

			} catch (XmlException ex) {
				// Something else wrong with the XML
				Logger.Log.Error ("Unable to parse {0}: {1}", path, ex.Message);
			}

			if (this.filters == null)
				return;

			foreach (ExternalFilterInfo efi in this.filters) {
				if (efi.MimeTypes != null) {
					foreach (string s in efi.MimeTypes)
						AddSupportedFlavor (FilterFlavor.NewFromMimeType (s));
				}

				if (efi.Extensions != null) {
					foreach (string s in efi.Extensions)
						AddSupportedFlavor (FilterFlavor.NewFromExtension (s));
				}
			}
		}

		private ExternalFilterInfo GetFilterInfo (string extension, string mime_type)
		{
			if (this.filters == null || this.filters.Length == 0)
				return null;

			if (extension != null) {
				foreach (ExternalFilterInfo efi in this.filters) {
					if (efi.Extensions == null)
						continue;

					if (ArrayFu.IndexOfString (efi.Extensions, extension) != -1)
						return efi;
				}
			}

			if (mime_type != null) {
				foreach (ExternalFilterInfo efi in this.filters) {
					if (efi.MimeTypes == null)
						continue;

					if (ArrayFu.IndexOfString (efi.MimeTypes, mime_type) != -1)
						return efi;
				}
			}

			return null;
		}

		protected override void DoPull ()
		{
			ExternalFilterInfo efi = GetFilterInfo (this.Extension, this.MimeType);

			if (efi == null) {
				Logger.Log.Warn ("Unable to find a match for extension {0}, mime type {1} when one should have matched", this.Extension, this.MimeType);
				Error ();
			}

			string args = efi.Arguments.Replace ("%s", String.Format ("\"{0}\"", FileInfo.FullName));

			Process pc = new Process ();

			pc.StartInfo.FileName = efi.Command;
			pc.StartInfo.Arguments = args;
			pc.StartInfo.RedirectStandardInput = false;
			pc.StartInfo.RedirectStandardOutput = true;
			pc.StartInfo.UseShellExecute = false;

			try {
				pc.Start ();
			} catch (System.ComponentModel.Win32Exception ex) {
				Logger.Log.Warn ("Unable to execute {0}: {1}",
						 pc.StartInfo.FileName, ex);
				Error ();
				return;
			}

			// Nice the process so that we don't monopolize the CPU
			pc.PriorityClass = ProcessPriorityClass.BelowNormal;

			StreamReader pout = pc.StandardOutput;

			string str;
			while ((str = pout.ReadLine ()) != null) {
				AppendText (str);
				AppendStructuralBreak ();
			}

			pout.Close ();
			pc.WaitForExit ();
			pc.Close ();
			Finished ();
		}
	}
}
