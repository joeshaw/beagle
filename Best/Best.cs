//
// Best.c
//
// Copyright (C) 2004 Novell, Inc.
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


/* Foo */

using System;

using Gtk;
using GtkSharp;
using Gnome;

using Beagle;
using Beagle.Util;

namespace Best {
	
	class Best {

		static int refs = 0;
		
		static public void IncRef ()
		{
			++refs;
		}

		static public void DecRef ()
		{
			--refs;
			if (refs <= 0)
				Application.Quit ();
		}

		static void Main (String[] args)
		{
			
			Program best = new Program ("best", "0.0", Modules.UI, args);
			GeckoUtils.Init ();
			GeckoUtils.SetSystemFonts ();

			if (args.Length > 0)
				BestWindow.Create (args [0]);
			else
				BestWindow.Create ();
			best.Run ();
		}
	}
}
