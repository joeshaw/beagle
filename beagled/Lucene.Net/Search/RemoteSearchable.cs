using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using Lucene.Net.Documents;
using Lucene.Net.Index;

namespace Lucene.Net.Search
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  IN NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	/// <summary>
	/// A remote searchable implementation.
	/// </summary>
	public class RemoteSearchable : MarshalByRefObject, Searchable 
	{
		private Searchable local;		

		public RemoteSearchable() 
		{
			this.local = local;
		}

		/// <summary>
		/// Constructs and exports a remote searcher.
		/// </summary>
		/// <param name="local"></param>
		public RemoteSearchable(Searchable local) 
		{
			this.local = local;
		}
  
		public void Search(Query query, Filter filter, HitCollector results)
		{
			local.Search(query, filter, results);
		}
  
		public void Close()  
		{
			local.Close();
		}

		public int DocFreq(Term term)  
		{
			return local.DocFreq(term);
		}

		public int MaxDoc()  
		{
			return local.MaxDoc();
		}

		public TopDocs Search(Query query, Filter filter, int n)  
		{
			return local.Search(query, filter, n);
		}

		public Document Doc(int i)  
		{
			return local.Doc(i);
		}

		public Query Rewrite(Query original)  
		{
			return local.Rewrite(original);
		}

		public Explanation Explain(Query query, int doc)  
		{
			return local.Explain(query, doc);
		}


		/// <summary>
		/// Exports a searcher for the index in args[0] named
		/// "//localhost:1099/Searchable". 
		/// </summary>
		/// <param name="args"></param>
		public static void Main(String[] args)  
		{
			ChannelServices.RegisterChannel(new TcpChannel(1099));
			RemoteSearchable remoteObj = new RemoteSearchable(new IndexSearcher(args[0]));
			RemotingServices.Marshal(remoteObj, "tcp://localhost:1099/Searchable");
		}
	}
}