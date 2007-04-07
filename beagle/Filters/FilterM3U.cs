using System;
using System.IO;

using Beagle.Daemon;

namespace Beagle.Filters {
	public class FilterM3U : Beagle.Daemon.Filter {
		public FilterM3U ()
		{
			SnippetMode = false;
			OriginalIsText = true;
			SetFileType ("audio");
		}

		protected override void RegisterSupportedTypes ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("audio/x-mpegurl"));
		}

		protected override void DoOpen (FileInfo file)
		{
			string line = TextReader.ReadLine ();
			if (line != "#EXTM3U")
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
				//	#EXTINF:length,Name
				//	Path
				if (line [0] == '#') {
					int index = line.IndexOf (',');
					if (index != -1 && index < (line.Length - 1))
						pull = AppendLine (line.Substring (index + 1));
				} else {
					pull = AppendLine (line);
				}
			} while (pull);
		}
	}
}
