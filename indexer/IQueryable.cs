//
// IQueryable.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

namespace Beagle {

	public delegate void HitCollector (ICollection hits);

	public interface IQueryable {

		String Name { get; }

		bool AcceptQuery (Query query);

		void Query (Query query, HitCollector collector);
	}
}
