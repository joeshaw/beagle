using System;

namespace Lucene.Net.QueryParsers
{
	public class QueryParserConstants
	{
		public const int EOF = 0;
		public const int _NUM_CHAR = 1;
		public const int _ESCAPED_CHAR = 2;
		public const int _TERM_START_CHAR = 3;
		public const int _TERM_CHAR = 4;
		public const int _WHITESPACE = 5;
		public const int AND = 7;
		public const int OR = 8;
		public const int NOT = 9;
		public const int PLUS = 10;
		public const int MINUS = 11;
		public const int LPAREN = 12;
		public const int RPAREN = 13;
		public const int COLON = 14;
		public const int CARAT = 15;
		public const int QUOTED = 16;
		public const int TERM = 17;
		public const int FUZZY = 18;
		public const int SLOP = 19;
		public const int PREFIXTERM = 20;
		public const int WILDTERM = 21;
		public const int RANGEIN_START = 22;
		public const int RANGEEX_START = 23;
		public const int NUMBER = 24;
		public const int RANGEIN_TO = 25;
		public const int RANGEIN_END = 26;
		public const int RANGEIN_QUOTED = 27;
		public const int RANGEIN_GOOP = 28;
		public const int RANGEEX_TO = 29;
		public const int RANGEEX_END = 30;
		public const int RANGEEX_QUOTED = 31;
		public const int RANGEEX_GOOP = 32;

		public const int Boost = 0;
		public const int RangeEx = 1;
		public const int RangeIn = 2;
		public const int DEFAULT = 3;

		public readonly String[] tokenImage = new String[] {
															   "<EOF>",
															   "<_NUM_CHAR>",
															   "<_ESCAPED_CHAR>",
															   "<_TERM_START_CHAR>",
															   "<_TERM_CHAR>",
															   "<_WHITESPACE>",
															   "<token of kind 6>",
															   "<AND>",
															   "<OR>",
															   "<NOT>",
															   "\"+\"",
															   "\"-\"",
															   "\"(\"",
															   "\")\"",
															   "\":\"",
															   "\"^\"",
															   "<QUOTED>",
															   "<TERM>",
															   "\"~\"",
															   "<SLOP>",
															   "<PREFIXTERM>",
															   "<WILDTERM>",
															   "\"[\"",
															   "\"{\"",
															   "<NUMBER>",
															   "\"TO\"",
															   "\"]\"",
															   "<RANGEIN_QUOTED>",
															   "<RANGEIN_GOOP>",
															   "\"TO\"",
															   "\"}\"",
															   "<RANGEEX_QUOTED>",
															   "<RANGEEX_GOOP>",
		};

	}
}