//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

namespace Dewey {

	public class Query {

		String queryStr;
		
		public Query (String _queryStr)
		{
			queryStr = _queryStr;
		}

		// As the name suggests, this is a transitional API.
		// (i.e. I don't quite yet know what the hell I'm doing.)
		public String AbusivePeekInsideQuery {
			get { return queryStr; }
		}
	}
}
