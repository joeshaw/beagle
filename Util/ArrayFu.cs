//
// ArrayFu.cs
//
// Copyright (C) 2005 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.Text;

namespace Beagle.Util {

	public class ArrayFu {

		public static void IntersectListChanges (IList new_list, 
							 IList old_list,
							 out IList added_list,
							 out IList removed_list)
		{
			added_list = new ArrayList ();
			removed_list = new ArrayList (old_list);

			foreach (object obj in new_list) {
				if (old_list.Contains (obj)) {
					removed_list.Remove (obj);
				} else {
					added_list.Add (obj);
				}
			}
		}

		public static int IndexOfByte (byte [] array, byte target, int start)
		{
			for (int i = start; i < array.Length; ++i)
				if (array [i] == target)
					return i;
			return -1;
		}

		public static int IndexOfByte (byte [] array, byte target)
		{
			return IndexOfByte (array, target, 0);
		}
	}
}
