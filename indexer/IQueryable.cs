//
// IQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

namespace Dewey {

	public interface IQueryable {

		String Name { get; }

		bool AcceptQuery (Query query);

		ICollection Query (Query query);
	}
}
