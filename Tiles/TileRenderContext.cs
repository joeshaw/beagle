//
// TileRenderContext.cs
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

namespace Beagle.Tile {

	public abstract class TileRenderContext {

		abstract public void Write (string markup);

		abstract public void Link  (string label, TileActionHandler handler);

		abstract public void Image (string name, int width, int height,
					    TileActionHandler handler);

		abstract public void Tile  (Tile tile);

		// Convenience functions

		public void Write (string format, params object[] args)
		{
			Write (String.Format (format, args));
		}

		public void WriteLine (string format, params object[] args)
		{
			Write (format, args);
			Write ("<br>");
		}
		
		public void Image (string name)
		{
			Image (name, -1, -1, null);
		}

		public void Image (string name, TileActionHandler handler)
		{
			Image (name, -1, -1, handler);
		}

		// Don't call these unless you really know what you are doing!
		abstract public void Checkpoint ();
		abstract public void Undo ();

	}
}
