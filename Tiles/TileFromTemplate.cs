//
// TileFromTemplate.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;

namespace Beagle {

	abstract public class TileFromTemplate : Tile {

		private ArrayList templateLines = new ArrayList ();

		public TileFromTemplate (string templateResource) : base ()
		{
			// We look for the resource in the assembly that contains
			// the type's definition.
			Assembly assembly = Assembly.GetAssembly (this.GetType ());
			Stream stream = assembly.GetManifestResourceStream (templateResource);
			// FIXME: fail if we there is no such resource (i.e. if stream == null)
			StreamReader sr = new StreamReader (stream);
			string line;
			while ((line = sr.ReadLine ()) != null)
				templateLines.Add (line);
		}

		virtual protected string ExpandKey (string key)
		{
			return null;
		}

		virtual protected bool RenderKey (string key, TileRenderContext ctx)
		{
			return false;
		}

		private void RenderLine (string line, TileRenderContext ctx)
		{
			ctx.Checkpoint ();

			int i = 0;
			while (i < line.Length) {
				int j = line.IndexOf ('@', i);
				if (j == -1)
					break;
				int k = line.IndexOf ('@', j+1);
				if (k == -1)
					break;

				if (j > i)
					ctx.Write (line.Substring (i, j-i));

				string key = line.Substring (j+1, k-j-1);
				string expansion;

				if (key == "") {
					ctx.Write ("@");
				} else if ((expansion = ExpandKey (key)) != null) {
					ctx.Write (expansion);
				} else if (! RenderKey (key, ctx)) {
					ctx.Undo ();
					return;
				}
				i = k+1;
			}

			if (i < line.Length)
				ctx.Write (line.Substring (i));
			ctx.Write ("\n");
		}

		override public void Render (TileRenderContext ctx)
		{
			foreach (string line in templateLines)
				RenderLine (line, ctx);
		}

	}

}
