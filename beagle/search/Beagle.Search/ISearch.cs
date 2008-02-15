//
// ISearch.cs
//
// Copyright (C) 2008 Lukas Lipka <lukaslipka@gmail.com>
//

using System;

using NDesk.DBus;
using org.freedesktop.DBus;

namespace Beagle.Search {
	
	[Interface ("org.gnome.Beagle")]
	public interface ISearch {
		void Query (string query);
	}
}
