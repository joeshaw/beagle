using System;

namespace Lucene.Net.Analysis.Standard
{
	public class STC 
	{
		public const int EOF = 0;
		public const int ALPHANUM = 1;
		public const int APOSTROPHE = 2;
		public const int ACRONYM = 3;
		public const int COMPANY = 4;
		public const int EMAIL = 5;
		public const int HOST = 6;
		public const int NUM = 7;
		public const int P = 8;
		public const int HAS_DIGIT = 9;
		public const int SIGRAM = 10;
		public const int ALPHA = 11;
		public const int LETTER = 12;
		public const int CJK = 13;
		public const int DIGIT = 14;
		public const int NOISE = 15;

		public const int DEFAULT = 0;

		public static readonly String[] tokenImage = new String[] {
										"<EOF>",
										"<ALPHANUM>",
										"<APOSTROPHE>",
										"<ACRONYM>",
										"<COMPANY>",
										"<EMAIL>",
										"<HOST>",
										"<NUM>",
										"<P>",
										"<HAS_DIGIT>",
										"<SIGRAM>",
										"<ALPHA>",
										"<LETTER>",
										"<CJK>",
										"<DIGIT>",
										"<NOISE>",
		};
	}
}