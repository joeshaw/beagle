//
// FileSystemQueryable.cs
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

using System;
using System.Reflection;
using Beagle.Daemon;

namespace Beagle.Daemon.FileSystemQueryable {

	[QueryableFlavor (Name="FileSystemQueryable", Domain=QueryDomain.Local)]
	public class FileSystemQueryable : IQueryable {

		IndexerQueue indexerQueue;
		Indexer indexer;

		public FileSystemQueryable ()
		{
			indexerQueue = new IndexerQueue ();
			ScanAssemblyForHandlers (Assembly.GetExecutingAssembly ());

			indexer = new Indexer (indexerQueue);
			
			DBusisms.Service.RegisterObject (indexer,
							 Beagle.DBusisms.IndexerPath);
		}

		private void ScanAssemblyForHandlers (Assembly assembly)
		{
			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (PreIndexHandler))) {

					PreIndexHandler handler = (PreIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PreIndexingEvent += handler.Run;
				}
				if (t.IsSubclassOf (typeof (PostIndexHandler))) {
					PostIndexHandler handler = (PostIndexHandler) Activator.CreateInstance (t);
					indexerQueue.PostIndexingEvent += handler.Run;
				}
			}
		}


		public string Name {
			get { return "FileSystemQueryable"; }
		}

		public bool AcceptQuery (QueryBody body)
		{
			return indexer.driver.AcceptQuery (body);
		}

		public void DoQuery (QueryBody body, IQueryResult queryResult)
		{
			indexer.driver.DoQuery (body, queryResult);
		}

	}

}
