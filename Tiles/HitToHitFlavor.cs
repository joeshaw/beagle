//
// HitToHitFlavor.cs
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
using System.Reflection;

namespace Beagle {

	public class HitToHitFlavor {

		private HitToHitFlavor () { } // This class is static

		static ArrayList flavorArray = null;
		
		static private void ScanAssembly (Assembly assembly)
		{
			foreach (Type type in assembly.GetTypes ()) {
				if (type.IsSubclassOf (typeof (Tile))) {
					foreach (object obj in Attribute.GetCustomAttributes (type)) {
						if (obj is HitFlavor) {
							HitFlavor flavor = (HitFlavor) obj;
							flavor.TileType = type;
							flavorArray.Add (flavor);
						}
					}
				}
			}
		}

		static public HitFlavor Get (Hit hit)
		{
			if (flavorArray == null) {
				flavorArray = new ArrayList ();
				ScanAssembly (Assembly.GetExecutingAssembly ());
			}

			HitFlavor flavorBest = null;
			int weightBest = -1;

			foreach (HitFlavor flavor in flavorArray) {
				if (flavor.IsMatch (hit)) {
					int weight = flavor.Weight;
					if (weight > weightBest) {
						flavorBest = flavor;
						weightBest = weight;
					} else if (weight == weightBest) {
						// This shouldn't happen.
						Console.WriteLine ("HitFlavor Weight tie! {0} and {1}",
								   flavorBest, flavor);
					}
				}
			}

			return flavorBest;
		}

		

	}

}
