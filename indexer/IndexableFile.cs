//
// IndexableFile.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;

using Beagle.Filters;

namespace Beagle {

	public class IndexableFile : Indexable {

		Filter filter;
		String path;

		public IndexableFile (String _path)
		{
			path = Path.GetFullPath (_path);
			if (! File.Exists (path))
				throw new Exception ("No such file: " + path);
	    
			filter = Filter.FilterFromPath (path);
			if (filter == null)
				throw new Exception ("Can't find filter for file " + path);

			Uri = "file://" + path;
			Type = "File";
			MimeType = filter.Flavor.MimeType;		

			Timestamp = File.GetLastWriteTime (path);
		}

		override protected void DoBuild ()
		{
			Stream stream = new FileStream (path, FileMode.Open, FileAccess.Read);
			filter.Open (stream);
			foreach (String key in filter.Keys)
				this [key] = filter [key];
			Content = filter.Content;
			HotContent = filter.HotContent;
			filter.Close ();
			stream.Close ();

			FileInfo info = new FileInfo (path);
			this ["Length"] = Convert.ToString (info.Length);
			this ["LastAccess"] = info.LastAccessTime.ToString ();

			// FIXME: there is more information that we could attach
			// * File ownership
			// * File permissions
			// * Filesystem-level metadata
			// * Nautilus emblems
		}
	}

}
