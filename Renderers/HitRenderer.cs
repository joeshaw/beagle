//
// HitRenderer.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;
using System.Collections;

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
	}
}
