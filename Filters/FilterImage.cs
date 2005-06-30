//
// FilterImage.cs
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
using System.Collections;
using System.IO;
using System.Text;

using Beagle.Util;

namespace Beagle.Filters {

	public abstract class FilterImage : Beagle.Daemon.Filter {

		public FilterImage ()
		{
		}

		protected virtual void PullImageProperties () { }

		protected override void DoPullProperties ()
		{
			PullImageProperties ();

			try {
				FSpotTools.Photo photo = FSpotTools.GetPhoto (this.FileInfo.FullName);

				if (photo == null)
					return;

				if (photo.Description != null && photo.Description != "")
					AddProperty (Beagle.Property.New ("fspot:Description", photo.Description));
			
				foreach (FSpotTools.Tag tag in photo.Tags) {				
					if (tag.Name != null && tag.Name != "")
						AddProperty (Beagle.Property.New ("fspot:Tag", tag.Name));
				}
			} catch (Exception e) {
				//Console.WriteLine ("Failed extracting F-Spot information for '{0}'", this.FileInfo.Name);
			}
		}
	}
}
