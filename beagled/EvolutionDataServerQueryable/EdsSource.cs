//
// EdsSource.cs
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

using Beagle.Util;

using Evolution;

namespace Beagle.Daemon.EvolutionDataServerQueryable {

	public delegate void IndexSourceHandler (Evolution.Source src);

	public class EdsSource {

		SourceList source_list;

		public IndexSourceHandler IndexSourceAll;
		public IndexSourceHandler IndexSourceChanges;
		public IndexSourceHandler RemoveSource;

		public EdsSource (string gconf_key)
		{
			this.source_list = new SourceList (gconf_key);

			if (this.source_list == null) {
				// FIXME: We may want to watch for the creation
				// of the sources GConf key
				Logger.Log.Info ("No sources found at {0}", gconf_key);
				return;
			}

			this.source_list.GroupAdded += OnGroupAdded;
			this.source_list.GroupRemoved += OnGroupRemoved;
		}

		public void Index ()
		{
			if (this.source_list == null)
				return;

			foreach (SourceGroup group in this.source_list.Groups)
				IndexSourceGroup (group);
		}

		private void IndexSourceGroup (SourceGroup group)
		{
			group.SourceAdded += OnSourceAdded;
			group.SourceRemoved += OnSourceRemoved;

			foreach (Evolution.Source src in group.Sources)
				this.IndexSourceChanges (src);
		}

		private void RemoveSourceGroup (SourceGroup group)
		{
			foreach (Evolution.Source src in group.Sources)
				this.RemoveSource (src);
		}

		private void OnGroupAdded (object o, GroupAddedArgs args)
		{
			IndexSourceGroup (args.Group);
		}

		private void OnGroupRemoved (object o, GroupRemovedArgs args)
		{
			RemoveSourceGroup (args.Group);
		}

		private void OnSourceAdded (object o, SourceAddedArgs args)
		{
			this.IndexSourceAll (args.Source);
		}

		private void OnSourceRemoved (object o, SourceRemovedArgs args)
		{
			this.RemoveSource (args.Source);
		}
	}
}