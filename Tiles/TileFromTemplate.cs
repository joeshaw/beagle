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

namespace Beagle.Tile {

	abstract public class TileFromTemplate : Tile {

		private Template template = null;
		private bool populated = false;
		private string template_resource;

		public TileFromTemplate (string resource) : base ()
		{
			template_resource = resource;
		}

		protected Template Template {
			get {
				if (template == null) {
					template = new Template (template_resource);
					Template["TileId"] = UniqueKey;
					Template["action:"] = "action:" + UniqueKey + "!";
				}

				return template;
			}
		}

		protected abstract void PopulateTemplate ();
		
		public override void Render (TileRenderContext ctx)
		{
			if (!populated) {
				PopulateTemplate ();
				populated = true;
			}
			
			ctx.Write (Template.ToString ());
		}

		protected override void Changed ()
		{
			populated = false;
			base.Changed ();
		}
	}

}
