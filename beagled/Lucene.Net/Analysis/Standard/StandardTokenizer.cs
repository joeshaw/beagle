using System;
using System.Collections;
using System.IO;

namespace Lucene.Net.Analysis.Standard
{
	/// <summary>
	/// A grammar-based tokenizer constructed with JavaCC.
	/// <p> This should be a good tokenizer for most European-language documents.</p>
	/// <p>
	/// Many applications have specific tokenizer needs.  If this tokenizer does
	/// not suit your application, please consider copying this source code
	/// directory to your project and maintaining your own grammar-based tokenizer.
	/// </p>
	/// </summary>
	public class StandardTokenizer : Lucene.Net.Analysis.Tokenizer 
	{
		/// <summary>
		/// Constructs a tokenizer for this TextReader.
		/// </summary>
		/// <param name="TextReader"></param>
		public StandardTokenizer(TextReader TextReader) 
			: this(new FastCharStream(TextReader))
		{
			this.input = TextReader;
		}

		/// <summary>
		/// Returns the next token in the stream, or null at EOS.
		/// <p>The returned token's type is set to an element of STC.tokenImage.</p>
		/// </summary>
		/// <returns></returns>
		public override Lucene.Net.Analysis.Token Next() 
		{
			Token token = null;
			switch ((_jj_ntk==-1)?jj_ntk():_jj_ntk) 
			{
				case STC.ALPHANUM:
					token = jj_consume_token(STC.ALPHANUM);
					break;
				case STC.APOSTROPHE:
					token = jj_consume_token(STC.APOSTROPHE);
					break;
				case STC.ACRONYM:
					token = jj_consume_token(STC.ACRONYM);
					break;
				case STC.COMPANY:
					token = jj_consume_token(STC.COMPANY);
					break;
				case STC.EMAIL:
					token = jj_consume_token(STC.EMAIL);
					break;
				case STC.HOST:
					token = jj_consume_token(STC.HOST);
					break;
				case STC.NUM:
					token = jj_consume_token(STC.NUM);
					break;
				case STC.SIGRAM:
					token = jj_consume_token(STC.SIGRAM);
					break;					
				case 0:
					token = jj_consume_token(0);
					break;
				default:
					jj_la1[0] = jj_gen;
					jj_consume_token(-1);
					throw new ParseException();
			}
			if (token.kind == STC.EOF) 
			{
			{if (true) return null;}
			} 
			else 
			{
			{
				if (true) return
							  new Lucene.Net.Analysis.Token(token.image,
							  token.beginColumn,token.endColumn,
							  STC.tokenImage[token.kind]);}
			}
		}

		public StandardTokenizerTokenManager token_source;
		public Token token, jj_nt;
		private int _jj_ntk;
		private int jj_gen;
		readonly private int[] jj_la1 = new int[1];
		static private int[] _jj_la1_0;
		static StandardTokenizer() 
		{
			jj_la1_0();
		}

		private static void jj_la1_0() 
		{
			_jj_la1_0 = new int[] {0x4ff,};
		}

		public StandardTokenizer(CharStream stream) 
		{
			token_source = new StandardTokenizerTokenManager(stream);
			token = new Token();
			_jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 1; i++) jj_la1[i] = -1;
		}

		public void ReInit(CharStream stream) 
		{
			token_source.ReInit(stream);
			token = new Token();
			_jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 1; i++) jj_la1[i] = -1;
		}

		public StandardTokenizer(StandardTokenizerTokenManager tm) 
		{
			token_source = tm;
			token = new Token();
			_jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 1; i++) jj_la1[i] = -1;
		}

		public void ReInit(StandardTokenizerTokenManager tm) 
		{
			token_source = tm;
			token = new Token();
			_jj_ntk = -1;
			jj_gen = 0;
			for (int i = 0; i < 1; i++) jj_la1[i] = -1;
		}

		private Token jj_consume_token(int kind) 
		{
			Token oldToken = null;
			if ((oldToken = token).next != null) token = token.next;
			else token = token.next = token_source.GetNextToken();
			_jj_ntk = -1;
			if (token.kind == kind) 
			{
				jj_gen++;
				return token;
			}
			token = oldToken;
			jj_kind = kind;
			throw GenerateParseException();
		}

		public Token GetNextToken() 
		{
			if (token.next != null) token = token.next;
			else token = token.next = token_source.GetNextToken();
			_jj_ntk = -1;
			jj_gen++;
			return token;
		}

		public Token GetToken(int index) 
		{
			Token t = token;
			for (int i = 0; i < index; i++) 
			{
				if (t.next != null) t = t.next;
				else t = t.next = token_source.GetNextToken();
			}
			return t;
		}

		private int jj_ntk() 
		{
			if ((jj_nt=token.next) == null)
				return (_jj_ntk = (token.next=token_source.GetNextToken()).kind);
			else
				return (_jj_ntk = jj_nt.kind);
		}

		private ArrayList jj_expentries = new ArrayList();
		private int[] jj_expentry;
		private int jj_kind = -1;

		public ParseException GenerateParseException() 
		{
			jj_expentries.Clear();
			bool[] la1tokens = new bool[14];
			for (int i = 0; i < 14; i++) 
			{
				la1tokens[i] = false;
			}
			if (jj_kind >= 0) 
			{
				la1tokens[jj_kind] = true;
				jj_kind = -1;
			}
			for (int i = 0; i < 1; i++) 
			{
				if (jj_la1[i] == jj_gen) 
				{
					for (int j = 0; j < 32; j++) 
					{
						if ((_jj_la1_0[i] & (1<<j)) != 0) 
						{
							la1tokens[j] = true;
						}
					}
				}
			}
			for (int i = 0; i < 14; i++) 
			{
				if (la1tokens[i]) 
				{
					jj_expentry = new int[1];
					jj_expentry[0] = i;
					jj_expentries.Add(jj_expentry);
				}
			}
			int[][] exptokseq = new int[jj_expentries.Count][];
			for (int i = 0; i < jj_expentries.Count; i++) 
			{
				exptokseq[i] = (int[])jj_expentries[i];
			}
			return new ParseException(token, exptokseq, STC.tokenImage);
		}

		public void enable_tracing() 
		{
		}

		public void disable_tracing() 
		{
		}
	}
}