//
// FilterText.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.IO;

namespace Beagle.Filters {

	public class FilterText : Filter {
	
		public FilterText ()
		{
			AddSupportedMimeType ("text/plain");
		}

		override protected void Read (Stream stream) 
		{
			StreamReader reader = new StreamReader (stream);
			String text = reader.ReadToEnd ();
			AppendContent (text);
		}
	}
 }
