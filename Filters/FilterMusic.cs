//
// FilterMusic.cs : This is the parent class of every audio tag-reading class
//                  It will retreive the tag from the subclass, then fill the
//                  beagle property as needed.
//
// Author:
//		Raphaël Slinckx <raf.raf@wol.be>
//
// Copyright 2004 (C) Raphaël Slinckx
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
using System.IO;
using Beagle.Util.AudioUtil;

namespace Beagle.Filters {

	public abstract class FilterMusic : Beagle.Daemon.Filter {
	
		private static Beagle.Util.Logger log = Beagle.Util.Logger.Get ("FilterMusic");
		
		public FilterMusic ()
		{
			RegisterSupportedTypes ();
		}
		
		protected abstract Tag GetTag (Stream s);
		
		protected abstract void RegisterSupportedTypes ();

		protected override void DoPullProperties ()
		{
			Tag tag = null;
			try {
				tag = GetTag (Stream);
			} catch (Exception e) {
				//FIXME: Is it better to throw an exception here ?
				Finished();
				return;
			}
			
			log.Debug ("{0}", tag);
			
			//FIXME: Do we need to check for non-null empty values ?
			//This should be done in Beagle.Property.New I think..
			
			if (tag.Artist.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:artist",  tag.Artist));

			if (tag.Album.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:album",   tag.Album));
			
			if (tag.Title.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:song",    tag.Title));

			if (tag.Comment.Length > 0)
				AddProperty (Beagle.Property.New ("fixme:comment", tag.Comment));

			if (tag.Track.Length > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:track", tag.Track));

			if (tag.Year.Length > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:year",  tag.Year));

			if (tag.Genre.Length > 0)
				AddProperty (Beagle.Property.NewKeyword ("fixme:genre", tag.Genre));
			
			/* TBA
			if (info.HasPicture)
-                               AddProperty (Beagle.Property.NewBool ("fixme:haspicture", true));
			*/
			
			Finished ();
		}
	}
}
