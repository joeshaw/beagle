//
// IndexableFile.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;
using System.IO;

using Dewey.Filters;

namespace Dewey {

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

			uri = "file://" + path;
			domain = "FileSystem";
			mimeType = filter.MimeType;		
			timestamp = File.GetLastWriteTime (path);
		}

		override public ICollection MetadataKeys {
			get { return filter.MetadataKeys; }
		}

		override public String this [String key] {
			get { return filter [key]; }
		}

		override public String Content {
			get { return filter.Content; }
		}

		override public String HotContent {
			get { return filter.HotContent; }
		}

		override public void Open ()
		{
			filter.Open (path);
		}

		override public void Close ()
		{
			filter.Close ();
		}

	}

}
