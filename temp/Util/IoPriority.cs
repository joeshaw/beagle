//
// IoPriority.cs
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
using System.Runtime.InteropServices;

namespace Beagle.Util {
	
	public class IoPriority {

		private IoPriority () {} // Static class

		[DllImport ("libbeagleglue")]
		static extern int set_io_priority_idle ();

		[DllImport ("libbeagleglue")]
		static extern int set_io_priority_best_effort (int ioprio);

		static public void ReduceIoPriority ()
		{
			// First try setting our IO class to idle, so that we
			// only ever get run if there are no other IO tasks.
			// If that fails, then try setting our IO priority
			// within the best effort class to the lowest we can,
			// which is priority 7.

			if (set_io_priority_idle () >= 0)
				Log.Debug ("Set IO priority class to idle.");
			else if (set_io_priority_best_effort (7) >= 0)
				Log.Debug ("Set best effort IO priority to lowest level (7)");
			else
				Log.Warn ("Unable to set IO priority class to idle or IO priority within best effort class to 7");
		}
	}
}
