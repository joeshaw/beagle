//
// ISearch.cs
//
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Beagle.Search {
	
	[Interface ("org.gnome.Beagle.Search")]
	public interface ISearch {
		void Show ();
		void Hide ();
	}
}
