//
// QueryProxy.cs
//
// Copyright (C) 2004 Novell, Inc.
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

namespace Beagle
{
	using System.Collections;
	using DBus;
	
	public enum QueryDomain {
		Local        = 1,
		Neighborhood = 2,
		Global       = 4
	}

	[Interface ("com.novell.Beagle.Query")]
	public abstract class QueryProxy {
		
		[Method]
		public abstract void AddText (string str);
		
		[Method]
		public abstract void AddTextRaw (string str);

		[Method]
		public abstract void AddMimeType (string type);

		[Method]
		public abstract void AddSource (string source);

		[Method]
		public abstract void AddDomain (QueryDomain d);
		
		[Method]
		public abstract void RemoveDomain (QueryDomain d);

		[Method]
		public abstract void Start ();

		[Method]
		public abstract void Cancel ();

		[Method]
		public abstract void CloseQuery ();

		public delegate void StartedHandler (QueryProxy sender);
		[Signal]
		public virtual event StartedHandler StartedEvent;

		public delegate void HitsAddedAsXmlHandler (QueryProxy sender, string hitsXml);
		[Signal]
		public virtual event HitsAddedAsXmlHandler HitsAddedAsXmlEvent;

		public delegate void HitsSubtractedAsStringHandler (QueryProxy sender, string uriList);
		[Signal]
		public virtual event HitsSubtractedAsStringHandler HitsSubtractedAsStringEvent;

		public delegate void CancelledHandler (QueryProxy sender);
		[Signal]
		public virtual event CancelledHandler CancelledEvent;

	}
}
