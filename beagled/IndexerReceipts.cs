//
// IndexerReceipts.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Collections;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	[XmlInclude (typeof (IndexerAddedReceipt)),
	 XmlInclude (typeof (IndexerRemovedReceipt)),
	 XmlInclude (typeof (IndexerChildIndexablesReceipt))]
	public abstract class IndexerReceipt {
		
		public IndexerReceipt () { }
	}

	public class IndexerAddedReceipt : IndexerReceipt {
		
		public IndexerAddedReceipt () { }

		public IndexerAddedReceipt (Uri uri)
		{
			this.Uri = uri;
		}

		public IndexerAddedReceipt (Uri uri, string filter_name, int filter_version)
		{
			this.Uri = uri;
			this.FilterName = filter_name;
			this.FilterVersion = filter_version;
		}
		
		[XmlIgnore]
		public Uri Uri;

		public bool PropertyChangesOnly = false;
		
		public string FilterName = null;
		
		public int FilterVersion = -1;
		
		[XmlAttribute ("Uri")]
		public string UriString {
			get { return UriFu.UriToEscapedString (Uri); }
			set { Uri = UriFu.EscapedStringToUri (value); }
		}
	}
	
	public class IndexerRemovedReceipt : IndexerReceipt {
		
		public IndexerRemovedReceipt () { }

		public IndexerRemovedReceipt (Uri uri)
		{
			this.Uri = uri;
		}
		
		[XmlIgnore]
		public Uri Uri;
		
		[XmlAttribute ("Uri")]
		public string UriString {
			get { return UriFu.UriToEscapedString (Uri); }
			set { Uri = UriFu.EscapedStringToUri (value); }
		}
	}
			     
	public class IndexerChildIndexablesReceipt : IndexerReceipt {

		public IndexerChildIndexablesReceipt () { }

		public IndexerChildIndexablesReceipt (Indexable parent, ArrayList children)
		{
			foreach (Indexable child in children)
				child.SetChildOf (parent);

			this.Children = children;
		}

		[XmlArray (ElementName="Children")]
		[XmlArrayItem (ElementName="Child", Type=typeof (Indexable))]
		public ArrayList Children;
	}

}
