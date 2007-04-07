using System;
using System.IO;

using Beagle.Daemon;

namespace Beagle.Filters {
	public class FilterPls : Beagle.Daemon.Filter {
		public FilterPls ()
		{
			SnippetMode = false;
			OriginalIsText = true;
			SetFileType ("audio");
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-scpls"));
		}

		protected override void DoOpen (FileInfo file)
		{
			string line = TextReader.ReadLine ();
			if (line != "[Playlist]")
				Error ();
		}

		override protected void DoPull ()
		{
			bool pull = false;
			do {
				string line = TextReader.ReadLine ();
				if (line == null) {
					Finished ();
					return;
				}

				// Format
				//	FileX=<path>
				//	TitleX=<title>
				//	other lines
				if (line.StartsWith ("File") ||
				    line.StartsWith ("Title")) {
					int index = line.IndexOf ('=');
					if (index != -1 && index < (line.Length - 1))
						pull = AppendLine (line.Substring (index + 1));
				}
			} while (pull);
		}
	}
}
