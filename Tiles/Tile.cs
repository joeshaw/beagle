//
// Tile.cs
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

namespace Beagle {

	public delegate void TileActionHandler ();
	public delegate void TileChangedHandler (Tile tile);

	public abstract class Tile {

		static private object uidSrcLock = new object ();
		static private long uidSrc = 0;
		private long uid;
		
		public Tile ()
		{
			lock (uidSrcLock) {
				++uidSrc;
				uid = uidSrc;
			}
		}

		public string UniqueKey {
			get { return "_tile_" + uid; }
		}

		////////////////////////

		bool renderInline = true; // FIXME: should be false by default
		public bool RenderInline {
			get { return renderInline; }
		}

		protected void EnableInlineRendering ()
		{
			renderInline = true;
		}
	       

		abstract public void Render (TileRenderContext ctx);

		////////////////////////

		virtual public void PopupMenu (TileMenuContext ctx) { }

		////////////////////////

		virtual public bool HandleUrlRequest (string url, Gtk.HTMLStream stream)
		{
			return false;
		}

		////////////////////////

		private TileChangedHandler changedHandler = null;

		public void SetChangedHandler (TileChangedHandler ch)
		{
			changedHandler = ch;
		}

		protected void Changed ()
		{
			if (changedHandler != null)
				changedHandler (this);
		}
	}
}
