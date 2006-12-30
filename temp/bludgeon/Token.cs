
using System;
using System.Text;

namespace Bludgeon {

	public class Token {

		// There are no vowels here to avoid stop words, stemming, etc.
		// We also exclude l, since it looks too much like 1
		private const string token_chars = "bcdfghjkmnpqrstvwxz0123456789";

		// A mixture of ASCII characters and latin characters outside
		// the normal ISO-8859-1 range to test UTF-8 support.  These
		// *should* still stem properly.
		private const string unicode_token_chars = "bĉđfğħjķłmńpqrŝſŧvwxžƀ0123456789";

		public const int Count = 512;

		static public string IdToString (int id)
		{
			if (id < 0 || id >= Count)
				throw new ArgumentException ();
			return token_table [id];
		}

		static public int StringToId (string str)
		{
			int i;
			i = Array.BinarySearch (token_table, str);
			if (i < 0 || i >= Count)
				return -1;
			return i;
		}

		private static Random random = new Random ();

		static public string GetRandom ()
		{
			return token_table [random.Next (Count)];
		}

		static public string GetRandomWithUnicode ()
		{
			return unicode_token_table [random.Next (Count)];
		}

		///////////////////////////////////////////////////////////////////////

		static private string [] token_table;
		static private string [] unicode_token_table;

		static Token ()
		{
			token_table = new string [Count];
			unicode_token_table = new string [Count];

			char [] buffer = new char [2];
			
			for (int i = 0; i < Count; ++i) {
				int a, b;
				a = i / token_chars.Length;
				b = i % token_chars.Length;
				
				buffer [0] = token_chars [a];
				buffer [1] = token_chars [b];

				token_table [i] = new string (buffer);
			}

			for (int i = 0; i < Count; ++i) {
				int a, b;
				a = i / token_chars.Length;
				b = i % unicode_token_chars.Length;

				buffer [0] = token_chars [a];
				buffer [1] = unicode_token_chars [b];

				unicode_token_table [i] = new string (buffer);
			}
		}
	}

}
