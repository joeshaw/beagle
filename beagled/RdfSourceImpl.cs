//
// RdfSinkImpl.cs
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

using DBus;
using System;
using System.Collections;

namespace Beagle.Daemon {

	public class RdfSourceImpl : Beagle.RdfSourceProxy {

		static private object counterLock = new object ();
		static private int counter = 1;

		private string path;
		private bool started = false;
		private bool finished = false;
		private ArrayList queue = new ArrayList ();

		public RdfSourceImpl ()
		{
			lock (counterLock) {
				path = "/com/novell/Beagle/RdfSource/Obj" + counter;
				++counter;
			}

			DBusisms.Service.RegisterObject (this, Path);
		}

		public string Path {
			get { return path; }
		}

		public override event Beagle.GotRdfXmlHandler   GotRdfXmlEvent;
		public override event Beagle.RdfFinishedHandler RdfFinishedEvent;

		public override void Start ()
		{
			if (! started) {
				started = true;
				foreach (string rdfXml in queue)
					AddRdfXml (rdfXml);
				queue = null;
			}
		}

		public void AddRdfXml (string rdfXml)
		{
			if (finished) {
				Console.WriteLine ("Adding to finished RdfSource");
				return;
			}

			if (started)
				GotRdfXmlEvent (rdfXml);
			else
				queue.Add (rdfXml);
		}

		public void Finished ()
		{
			if (! finished) {
				finished = true;
				RdfFinishedEvent ();
			}
		}

		
		

	}
}
