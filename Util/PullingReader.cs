//
// PullingReader.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Beagle.Util {
	
	public class PullingReader : TextReader {

		public delegate string Pull ();

		Pull pull;
		StringBuilder pullBuffer = new StringBuilder ();
		bool done = false;

		public PullingReader (Pull _pull) : base ()
		{
			pull = _pull;
		}

		private void DoPull (int neededSize)
		{
			while (! done && pullBuffer.Length < neededSize) {
				string str = pull ();
				if (str != null) {
					pullBuffer.Append (str);
				} else
					done = true;
			}
		}

		public override void Close ()
		{

		}

		public override int Peek ()
		{
			DoPull (1);
			return done ? -1 : (int) pullBuffer [0];
		}

		public override int Read ()
		{
			DoPull (1);
			if (done)
				return -1;
			int x = (int) pullBuffer [0];
			pullBuffer.Remove (0, 1);
			return x;
		}

		public override int Read (char[] buffer, int index, int count)
		{
			DoPull (count);
			if (done && pullBuffer.Length < count)
				count = pullBuffer.Length;

			for (int i = 0; i < count; ++i)
				buffer [index + i] = pullBuffer [i];
			pullBuffer.Remove (0, count);

			return count;
		}

		public override int ReadBlock (char[] buffer, int index, int count)
		{
			return Read (buffer, index, count);
		}

		public override string ReadLine ()
		{
			int i = 0;

			DoPull (1);

			if (done)
				return null;

			while (true) {
				
				while (i < pullBuffer.Length) {
					if (pullBuffer [i] == '\n') {
						string foo = pullBuffer.ToString (0, i);
						pullBuffer.Remove (0, i+1);
						return foo;
					}
					++i;
				}

				// If there is nothing else to pull, just return everything
				// in our buffer.
				if (done) {
					string str = pullBuffer.ToString ();
					pullBuffer = new StringBuilder ("");
					return str;
				}

				DoPull (2 * pullBuffer.Length);
			}
		}

		public override string ReadToEnd ()
		{
			while (! done)
				DoPull (2 * pullBuffer.Length);
			
			string str = pullBuffer.ToString ();
			pullBuffer = new StringBuilder ("");
			return str;
		}

		

	}
}

// last line
