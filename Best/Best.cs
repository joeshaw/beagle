//
// Best.c
//
// Copyright (C) 2004 Novell, Inc.
//

/* Foo */

using System;

using Gtk;
using GtkSharp;
using Gnome;

using Dewey;

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
			
			IconTheme it = new IconTheme ();

			BestWindow.Create ();
			best.Run ();
		}
	}
}
