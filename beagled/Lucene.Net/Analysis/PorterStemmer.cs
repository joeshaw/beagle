using System;
using System.IO;

namespace Lucene.Net.Analysis
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
	 *
	 * Redistribution and use _in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions _in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer _in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear _in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear _in their name, without
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

	/*
	   Porter stemmer in Java. The original paper is in

		   Porter, 1980, An algorithm for suffix stripping, Program, Vol. 14,
		   no. 3, pp 130-137,

	   See also http://www.muscat.com/~martin/stem.html

	   Bug 1 (reported by Gonzalo Parra 16/10/99) fixed as marked below.
	   Tthe words 'aed', 'eed', 'oed' leave k at 'a' for step 3, and b[k-1]
	   is then out outside the bounds of b.

	   Similarly,

	   Bug 2 (reported by Steve Dyrdahl 22/2/00) fixed as marked below.
	   'ion' by itself leaves j = -1 in the test for 'ion' in step 5, and
	   b[j] is then outside the bounds of b.

	   Release 3.

	   [ This version is derived from Release 3, modified by Brian Goetz to 
		 optimize for fewer object creations.  ]

	*/

	/// <summary>
	/// Stemmer, implementing the Porter Stemming Algorithm
	///
	/// The Stemmer class transforms a word into its root form.  The input
	/// word can be provided a character at time (by calling Add()), or at once
	/// by calling one of the various Stem(something) methods.  
	/// </summary>
	class PorterStemmer
	{   
		private char[] b;
		private int i,    /* offset into b */
			j, k, k0;
		private bool dirty = false;
		private const int INC = 50; /* unit of size whereby b is increased */
		private const int EXTRA = 1;

		public PorterStemmer() 
		{  
			b = new char[INC];
			i = 0;
		}

		/// <summary>
		/// Resets the stemmer so it can stem another word.  If you invoke
		/// the stemmer by calling Add(char) and then Stem(), you must call Reset()
		/// before starting another word.
		/// </summary>
		public void Reset() { i = 0; dirty = false; }

		/// <summary>
		/// Add a character to the word being stemmed.  When you are finished 
		/// adding characters, you can call Stem(void) to process the word. 
		/// </summary>
		/// <param name="ch"></param>
		public void Add(char ch) 
		{
			if (b.Length <= i + EXTRA) 
			{
				char[] new_b = new char[b.Length+INC];
				for (int c = 0; c < b.Length; c++) 
					new_b[c] = b[c];
				b = new_b;
			}
			b[i++] = ch;
		}

		/// <summary>
		/// After a word has been stemmed, it can be retrieved by ToString(), 
		/// or a reference to the internal buffer can be retrieved by GetResultBuffer
		/// and GetResultLength (which is generally more efficient.)
		/// </summary>
		/// <returns></returns>
		public override String ToString() { return new String(b,0,i); }

		/// <summary>
		/// Returns the length of the word resulting from the stemming process.
		/// </summary>
		/// <returns></returns>
		virtual public int GetResultLength() { return i; }

		/// <summary>
		/// Returns a reference to a character buffer containing the results of
		/// the stemming process.  You also need to consult GetResultLength()
		/// to determine the length of the result.
		/// </summary>
		/// <returns></returns>
		public char[] GetResultBuffer() { return b; }

		/// <summary>
		/// Ñons(i) is true &lt;=&gt; b[i] is a consonant.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		private bool Cons(int i) 
		{
			switch (b[i]) 
			{
				case 'a': 
					return false;
				case 'e':
					return false;
				case 'i': 
					return false;
				case 'o': 
				    return false;
				case 'u': 
					return false;
				case 'y': 
					return (i==k0) ? true : !Cons(i-1);
				default: 
					return true;
			}
		}

		/// <summary>
		/// M() measures the number of consonant sequences between k0 and j. if c is
		/// a consonant sequence and v a vowel sequence, and &lt;..&gt; indicates arbitrary
		/// presence,
		///		&lt;c&gt;&lt;v&gt;       gives 0
		///		&lt;c&gt;vc&lt;v&gt;     gives 1
		///		&lt;c&gt;vcvc&lt;v&gt;   gives 2
		///		&lt;c&gt;vcvcvc&lt;v&gt; gives 3
		///		....
		/// </summary>
		/// <returns></returns>
		private int M() 
		{
			int n = 0;
			int i = k0;
			while(true) 
			{
				if (i > j) 
					return n;
				if (! Cons(i)) 
					break; 
				i++;
			}
			i++;
			while(true) 
			{
				while(true) 
				{
					if (i > j) 
						return n;
					if (Cons(i)) 
						break;
					i++;
				}
				i++;
				n++;
				while(true) 
				{
					if (i > j) 
						return n;
					if (! Cons(i)) 
						break;
					i++;
				}
				i++;
			}
		}

		/// <summary>
		/// Vowelinstem() is true &lt;=&gt; k0,...j contains a vowel
		/// </summary>
		/// <returns></returns>
		private bool Vowelinstem() 
		{
			int i; 
			for (i = k0; i <= j; i++) 
				if (! Cons(i)) 
					return true;
			return false;
		}

		/// <summary>
		/// Doublec(j) is true &lt;=&gt; j,(j-1) contain a double consonant.
		/// </summary>
		/// <param name="j"></param>
		/// <returns></returns>
		private bool Doublec(int j) 
		{
			if (j < k0+1) 
				return false;
			if (b[j] != b[j-1]) 
				return false;
			return Cons(j);
		}

		/// <summary>
		/// Cvc(i) is true &lt;=&gt; i-2,i-1,i has the form consonant - vowel - consonant
		/// and also if the second c is not w,x or y. this is used when trying to
		/// restore an e at the end of a short word. e.g.
		///
		///				Cav(e), Lov(e), Hop(e), Crim(e), but
		///				snow, box, tray.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		private bool Cvc(int i) 
		{
			if (i < k0+2 || !Cons(i) || Cons(i-1) || !Cons(i-2)) 
				return false;
			else 
			{
				int ch = b[i];
				if (ch == 'w' || ch == 'x' || ch == 'y') return false;
			}
			return true;
		}

		private bool Ends(String s) 
		{
			int l = s.Length;
			int o = k-l+1;
			if (o < k0) 
				return false;
			for (int i = 0; i < l; i++) 
				if (b[o+i] != s[i]) 
					return false;
			j = k-l;
			return true;
		}

		/// <summary>
		/// Setto(s) sets (j+1),...k to the characters in the string s, readjusting k. 
		/// </summary>
		/// <param name="s"></param>
		internal virtual void Setto(String s) 
		{
			int l = s.Length;
			int o = j+1;
			for (int i = 0; i < l; i++) 
				b[o+i] = s[i];
			k = j+l;
			dirty = true;
		}

		/// <summary>
		/// R(s) is used further down.
		/// </summary>
		/// <param name="s"></param>
		internal virtual void R(String s) { if (M() > 0) Setto(s); }
  
		/// <summary>
		/// Step1() gets rid of plurals and -ed or -ing. e.g.
		///		 caresses  ->  caress
		///		 ponies    ->  poni
		///		 ties      ->  ti
		///		 caress    ->  caress
		///		 cats      ->  cat
		///
		///		 feed      ->  feed
		///		 agreed    ->  agree
		///		 disabled  ->  disable
		///
		///		 matting   ->  mat
		///		 mating    ->  mate
		///		 meeting   ->  meet
		///		 milling   ->  mill
		///		 messing   ->  mess
		///
		///		 meetings  ->  meet
		/// </summary>
		private void Step1() 
		{
			if (b[k] == 's') 
			{
				if (Ends("sses")) k -= 2; 
				else if (Ends("ies")) Setto("i"); 
				else if (b[k-1] != 's') k--;
			}
			if (Ends("eed")) 
			{ 
				if (M() > 0) 
					k--; 
			} 
			else if ((Ends("ed") || Ends("ing")) && Vowelinstem()) 
			{  
				k = j;
				if (Ends("at")) Setto("ate"); 
				else if (Ends("bl")) Setto("ble"); 
				else if (Ends("iz")) Setto("ize"); 
				else if (Doublec(k)) 
				{
					int ch = b[k--];
					if (ch == 'l' || ch == 's' || ch == 'z') 
						k++;
				}
				else if (M() == 1 && Cvc(k)) 
					Setto("e");
			}
		}

		/// <summary>
		/// Step2() turns terminal y to i when there is another vowel in the stem.
		/// </summary>
		private void Step2() 
		{ 
			if (Ends("y") && Vowelinstem()) 
			{
				b[k] = 'i'; 
				dirty = true;
			}
		}


		/// <summary>
		/// Step3() maps double suffices to single ones. so -ization ( = -ize plus
		/// -ation) maps to -ize etc. note that the string before the suffix must give
		/// M() > 0.
		/// </summary>
		private void Step3() 
		{ 
			if (k == k0) return; /* For Bug 1 */ 
			switch (b[k-1]) 
			{
				case 'a': 
					if (Ends("ational")) { R("ate"); break; }
					if (Ends("tional")) { R("tion"); break; }
					break;
				case 'c': 
					if (Ends("enci")) { R("ence"); break; }
					if (Ends("anci")) { R("ance"); break; }
					break;
				case 'e': 
					if (Ends("izer")) { R("ize"); break; }
					break;
				case 'l': 
					if (Ends("bli")) { R("ble"); break; }
					if (Ends("alli")) { R("al"); break; }
					if (Ends("entli")) { R("ent"); break; }
					if (Ends("eli")) { R("e"); break; }
					if (Ends("ousli")) { R("ous"); break; }
					break;
				case 'o': 
					if (Ends("ization")) { R("ize"); break; }
					if (Ends("ation")) { R("ate"); break; }
					if (Ends("ator")) { R("ate"); break; }
					break;
				case 's': 
					if (Ends("alism")) { R("al"); break; }
					if (Ends("iveness")) { R("ive"); break; }
					if (Ends("fulness")) { R("ful"); break; }
					if (Ends("ousness")) { R("ous"); break; }
					break;
				case 't': 
					if (Ends("aliti")) { R("al"); break; }
					if (Ends("iviti")) { R("ive"); break; }
					if (Ends("biliti")) { R("ble"); break; }
					break;
				case 'g': 
					if (Ends("logi")) { R("log"); break; }
				break;
			} 
		}

		/// <summary>
		/// Step4() deals with -ic-, -full, -ness etc. similar strategy to Step3.
		/// </summary>
		private void Step4() 
		{ 
			switch (b[k]) 
			{
				case 'e': 
					if (Ends("icate")) { R("ic"); break; }
					if (Ends("ative")) { R(""); break; }
					if (Ends("alize")) { R("al"); break; }
					break;
				case 'i': 
					if (Ends("iciti")) { R("ic"); break; }
					break;
				case 'l': 
					if (Ends("ical")) { R("ic"); break; }
					if (Ends("ful")) { R(""); break; }
					break;
				case 's': 
					if (Ends("ness")) { R(""); break; }
					break;
			}
		}
  
		/// <summary>
		/// Step5() takes off -ant, -ence etc., in context &lt;c&gt;vcvc&lt;v&gt;.
		/// </summary>
		private void Step5() 
		{
			if (k == k0) return; /* for Bug 1 */ 
			switch (b[k-1]) 
			{
				case 'a': 
					if (Ends("al")) break; 
					return;
				case 'c': 
					if (Ends("ance")) break;
					if (Ends("ence")) break; 
					return;
				case 'e': 
					if (Ends("er")) break; return;
				case 'i': 
					if (Ends("ic")) break; return;
				case 'l': 
					if (Ends("able")) break;
					if (Ends("ible")) break; return;
				case 'n': 
					if (Ends("ant")) break;
					if (Ends("ement")) break;
					if (Ends("ment")) break;
					/* element etc. not stripped before the m */
					if (Ends("ent")) break; 
					return;
				case 'o': 
					if (Ends("ion") && j >= 0 && (b[j] == 's' || b[j] == 't')) break;
					/* j >= 0 fixes Bug 2 */
					if (Ends("ou")) break; 
					return;
					/* takes care of -ous */
				case 's': 
					if (Ends("ism")) break; 
					return;
				case 't': 
					if (Ends("ate")) break;
					if (Ends("iti")) break; 
					return;
				case 'u': 
					if (Ends("ous")) break; 
					return;
				case 'v': 
					if (Ends("ive")) break; 
					return;
				case 'z': 
					if (Ends("ize")) break; 
					return;
				default: 
					return;
			}

#if false
			// FIXED trow@ximian.com 15 Nov 2004  Removed unreachable code.
			if (M() > 1) 
				k = j;
#endif
		}

		/// <summary>
		/// Step6() removes a final -e if M() > 1.
		/// </summary>
		private void Step6() 
		{
			j = k;
			if (b[k] == 'e') 
			{
				int a = M();
				if (a > 1 || a == 1 && !Cvc(k-1)) 
					k--;
			}
			if (b[k] == 'l' && Doublec(k) && M() > 1) 
				k--;
		}

		/// <summary>
		/// Stem a word provided as a String.  Returns the result as a String.
		/// </summary>
		/// <param name="s"></param>
		/// <returns></returns>
		public String Stem(String s) 
		{
			if (Stem(s.ToCharArray(), s.Length))
				return ToString();
			else 
				return s;
		}

		/// <summary>
		/// Stem a word contained in a char[].  Returns true if the stemming process
		/// resulted in a word different from the input.  You can retrieve the 
		/// result with GetResultLength()/GetResultBuffer() or ToString(). 
		/// </summary>
		/// <param name="word"></param>
		/// <returns></returns>
		public virtual bool Stem(char[] word) 
		{
			return Stem(word, word.Length);
		}

		/// <summary>
		/// Stem a word contained in a portion of a char[] array.  Returns
		/// true if the stemming process resulted in a word different from
		/// the input.  You can retrieve the result with
		/// GetResultLength()/GetResultBuffer() or ToString().  
		/// </summary>
		/// <param name="wordBuffer"></param>
		/// <param name="offset"></param>
		/// <param name="wordLen"></param>
		/// <returns></returns>
		public virtual bool Stem(char[] wordBuffer, int offset, int wordLen) 
		{
			Reset();
			if (b.Length < wordLen) 
			{
				char[] new_b = new char[wordLen + EXTRA];
				b = new_b;
			}
			for (int j=0; j<wordLen; j++) 
				b[j] = wordBuffer[offset+j];
			i = wordLen;
			return Stem(0);
		}

		/// <summary>
		/// Stem a word contained in a leading portion of a char[] array.
		/// Returns true if the stemming process resulted in a word different
		/// from the input. You can retrieve the result with
		/// GetResultLength()/GetResultBuffer() or ToString().  
		/// </summary>
		/// <param name="word"></param>
		/// <param name="wordLen"></param>
		/// <returns></returns>
		public virtual bool Stem(char[] word, int wordLen) 
		{
			return Stem(word, 0, wordLen);
		}

		/// Stem the word placed into the Stemmer buffer through calls to Add().
		/// Returns true if the stemming process resulted in a word different
		/// from the input.  You can retrieve the result with
		/// GetResultLength()/GetResultBuffer() or ToString().  
		public virtual bool Stem() 
		{
			return Stem(0);
		}

		public virtual bool Stem(int i0) 
		{  
			k = i - 1; 
			k0 = i0;
			if (k > k0+1) 
			{ 
				Step1(); Step2(); Step3(); Step4(); Step5(); Step6(); 
			}
			// Also, a word is considered dirty if we lopped off letters
			// Thanks to Ifigenia Vairelles for pointing this out.
			if (i != k+1)
				dirty = true;
			i = k+1;
			return dirty;
		}

		/// <summary>
		/// Test program for demonstrating the Stemmer.  It reads a file and
		/// stems each word, writing the result to standard out.  
		/// Usage: Stemmer file-name 
		/// </summary>
		/// <param name="args"></param>
		public static void Main(String[] args) 
		{
			PorterStemmer s = new PorterStemmer();

			for (int i = 0; i < args.Length; i++) 
			{
				try 
				{
					// fix: 1.3.2.2 - FileAccess.Read
					FileStream _in = new FileStream(args[i], FileMode.Open, FileAccess.Read);
					byte[] buffer = new byte[1024];
					int bufferLen, offset, ch;

					bufferLen = _in.Read(buffer, 0, buffer.Length);
					offset = 0;
					s.Reset();

					while(true) 
					{  
						if (offset < bufferLen) 
							ch = buffer[offset++];
						else 
						{
							bufferLen = _in.Read(buffer, 0, buffer.Length);
							offset = 0;
							if (bufferLen < 0) 
								ch = -1;
							else 
								ch = buffer[offset++];
						}

						if (Char.IsLetter((char) ch)) 
						{
							s.Add(Char.ToLower((char) ch));
						}
						else 
						{  
							s.Stem();
							Console.Write(s.ToString());
							s.Reset();
							if (ch < 0) 
								break;
							else 
							{
								Console.Write((char) ch);
							}
						}
					}

					_in.Close();
				}
				catch (IOException) 
				{  
					Console.WriteLine("error reading " + args[i]);
				}
			}
		}
	}
}
