//
// Query.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using LNS = Lucene.Net.Search;

namespace Dewey {

    public class Query {

	String query_str;

	public Query (String _query_str) {
	    query_str = _query_str;
	}
	
	public LNS.Query ToLuceneQuery (Analyzer analyzer) {

	    LNS.BooleanQuery ln_query;
	    ln_query = new LNS.BooleanQuery ();

	    LNS.Query ln_hot_query;
	    ln_hot_query = QueryParser.Parse (query_str, "HotBody", analyzer);
	    ln_hot_query.SetBoost (2.0f);
	    ln_query.Add (ln_hot_query, false, false);
	    
	    LNS.Query ln_md_query;
	    ln_md_query = QueryParser.Parse (query_str, "MetaData", analyzer);
	    ln_md_query.SetBoost (1.5f);
	    ln_query.Add (ln_md_query, false, false);
	    
	    LNS.Query ln_body_query;
	    ln_body_query = QueryParser.Parse (query_str, "Body", analyzer);
	    ln_query.Add (ln_body_query, false, false);
	    
	    return ln_query;
	}
	

    }

}
