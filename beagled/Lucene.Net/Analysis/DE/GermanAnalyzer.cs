using System;
using System.IO;
using System.Collections;
using Lucene.Net.Analysis.Standard;

namespace Lucene.Net.Analysis.De
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
	/// Analyzer for German language. Supports an external list of stopwords (words that
	/// will not be indexed at all) and an external list of exclusions (word that will
	/// not be stemmed, but indexed).
	/// A default set of stopwords is used unless an alternative list is specified, the
	/// exclusion list is empty by default.
	/// </summary>
	/// <author>Gerhard Schwarz</author>
	/// <version>$Id$</version>
	public class GermanAnalyzer : Analyzer
	{
		/// <summary>
		/// List of typical german stopwords.
		/// </summary>
		private String[] GERMAN_STOP_WORDS = 
		{
			"einer", "eine", "eines", "einem", "einen",
			"der", "die", "das", "dass", "daß",
			"du", "er", "sie", "es",
			"was", "wer", "wie", "wir",
			"und", "oder", "ohne", "mit",
			"am", "im", "in", "aus", "auf",
			"ist", "sein", "war", "wird",
			"ihr", "ihre", "ihres",
			"als", "für", "von",
			"dich", "dir", "mich", "mir",
			"mein", "kein",
			"durch", "wegen"
		};

		/// <summary>
		/// Contains the stopwords used with the StopFilter. 
		/// </summary>
		private Hashtable stoptable = new Hashtable();

		/// <summary>
		/// Contains words that should be indexed but not stemmed. 
		/// </summary>
		private Hashtable excltable = new Hashtable();

		/// <summary>
		/// Builds an analyzer. 
		/// </summary>
		public GermanAnalyzer()
		{
			stoptable = StopFilter.MakeStopTable( GERMAN_STOP_WORDS );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( String[] stopwords )
		{
			stoptable = StopFilter.MakeStopTable( stopwords );
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( Hashtable stopwords )
		{
			stoptable = stopwords;
		}

		/// <summary>
		/// Builds an analyzer with the given stop words. 
		/// </summary>
		/// <param name="stopwords"></param>
		public GermanAnalyzer( FileInfo stopwords )
		{
			stoptable = WordlistLoader.GetWordtable( stopwords );
		}

		/// <summary>
		/// Builds an exclusionlist from an array of Strings. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable( String[] exclusionlist )
		{
			excltable = StopFilter.MakeStopTable( exclusionlist );
		}

		/// <summary>
		/// Builds an exclusionlist from a Hashtable. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable( Hashtable exclusionlist )
		{
			excltable = exclusionlist;
		}

		/// <summary>
		/// Builds an exclusionlist from the words contained in the given file. 
		/// </summary>
		/// <param name="exclusionlist"></param>
		public void SetStemExclusionTable(FileInfo exclusionlist)
		{
			excltable = WordlistLoader.GetWordtable(exclusionlist);
		}

		/// <summary>
		/// Creates a TokenStream which tokenizes all the text in the provided TextReader. 
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="reader"></param>
		/// <returns>A TokenStream build from a StandardTokenizer filtered with StandardFilter, StopFilter, GermanStemFilter</returns>
		public override TokenStream TokenStream(String fieldName, TextReader reader)
		{
			TokenStream result = new StandardTokenizer( reader );
			result = new StandardFilter( result );
			result = new StopFilter( result, stoptable );
			result = new GermanStemFilter( result, excltable );
			return result;
		}
	}
}