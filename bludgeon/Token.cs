
using System;
using System.Text;

namespace Bludgeon {

	public class Token {

		public const int Count = 8192;

		static public string GetString (int id)
		{
			if (id < 0 || id >= Count)
				throw new ArgumentException ();
			return token_table [id];
		}

		static public int GetId (string str)
		{
			int i;
			i = Array.BinarySearch (token_table, str);
			if (i < 0 || i >= Count)
				return -1;
			return i;
		}

		private static Random random = new Random ();

		static public int GetRandom ()
		{
			return random.Next (Count);
		}

		///////////////////////////////////////////////////////////////////////

		static private string [] token_table;
		
		static Token ()
		{
			token_table = new string [Count];

			for (int i = 0; i < Count; ++i)
				token_table [i] = TokenFromSeed (i);
			Array.Sort (token_table);

			// Paranoia is healthy.

			for (int i = 1; i < Count; ++i)
				if (token_table [i-1] == token_table [i])
					throw new Exception ("Duplicate tokens!");

			for (int id = 0; id < Count; ++id) {
				string str;
				str = GetString (id);
				int id2;
				id2 = GetId (str);
				if (id != id2)
					throw new Exception ("Brain damage!");
			}
		}
		
		// This is a silly algorithm, but it is an easy way to
		// reproducibly generate 8192 strings that all stem to
		// distinct values.

		static private char [] buffer = new char [10];
		static private string TokenFromSeed (int seed)
		{
			const int first_char = 97; // lower case 'a'
			const uint p = 23;
			const uint q = 7;

			uint state = (uint) seed;

			for (int i = 0; i < buffer.Length; ++i) {
				
				// Put 'z' in the last three characters,
				// to avoid unhappy accidents of stemming.
				if (i >= buffer.Length - 3) {
					buffer [i] = 'z';
					continue;
				}

				buffer [i] = Convert.ToChar (first_char + (state % p));
				state *= state + q;
			}
			
			return new string (buffer);
		}

	}

}
