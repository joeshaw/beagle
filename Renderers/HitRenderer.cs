//
// HitRenderer.cs
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
using System.Reflection;

namespace Beagle {
	
	public abstract class HitRenderer {

		// The hit type this renderer knows how to handle.  See
		// doc/matchtypes.txt.
		//
		// Specify "Default" to get all matches for which
		// there is no type-specific renderer.
		protected String type = "Unknown";

		public String Type {
			get { return type; }
		}

		public abstract Gtk.Widget Widget { get; }

		protected abstract void DoRenderHits (ArrayList hits);

		public void RenderHits (ICollection hits)
		{
			ArrayList array = new ArrayList ();
			foreach (Hit hit in hits)
				if (hit.Type == Type)
					array.Add (hit);
			DoRenderHits (array);
		}

		//
		// FIXME: This assumes that all renderers live inside
		// of this assembly.  This isn't a terrible assumption
		// to make right now, but it probably won't be true in
		// the long run.
		//
		
		static Hashtable registry = null;
		
		static private void AutoRegisterRenderers ()
		{
			registry = new Hashtable ();

			Assembly a = Assembly.GetExecutingAssembly ();
			foreach (Type t in a.GetTypes ()) {
				if (t.IsSubclassOf (typeof (HitRenderer))) {
					HitRenderer r = null;
					try {
						r = (HitRenderer) Activator.CreateInstance (t);
					} catch {
						// If it fails, just skip it.
					}
					if (r != null)
						registry [r.Type] = t;
				}
			}
		}

		static public HitRenderer FindRendererByType (String hitType)
		{
			if (registry == null)
				AutoRegisterRenderers ();

			if (! registry.Contains (hitType))
				hitType = "Default";
			
			Type t = (Type) registry [hitType];

			return (HitRenderer) Activator.CreateInstance (t);
		}

		static public HitRenderer FindRendererForHit (Hit hit)
		{
			return FindRendererByType (hit.Type);
		}
	}
}
