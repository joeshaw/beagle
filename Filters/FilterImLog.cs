//
// FilterGaimLog.cs
//
// Copyright (C) 2005 Novell, Inc.
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

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters {

	public class FilterImLog : Beagle.Daemon.Filter {

		private ImLog log;

		public FilterImLog ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType (GaimLog.MimeType));
			AddSupportedFlavor (FilterFlavor.NewFromMimeType (KopeteLog.MimeType));
			SnippetMode = true;
			OriginalIsText = true;
		}

		override protected void DoOpen (FileInfo file)
		{
			if (MimeType == GaimLog.MimeType)
				log = new GaimLog (FileInfo, new StreamReader (Stream));
			else if (MimeType == KopeteLog.MimeType)
				log = new KopeteLog (FileInfo, new StreamReader (Stream));
			else
				Error ();
		}

		protected override void DoPullProperties ()
		{
			AddProperty (Beagle.Property.NewDate ("fixme:starttime", log.StartTime));
			AddProperty (Beagle.Property.NewDate ("fixme:endtime", log.EndTime));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:client", log.Client));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:protocol", log.Protocol));

			// FIXME: Should these use Property.NewKeyword and be searched?
			AddProperty (Beagle.Property.NewKeyword ("fixme:speakingto", log.SpeakingTo));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:identity", log.Identity));
			
	
		}

		override protected void DoPull ()
		{
			foreach (ImLog.Utterance utt in log.Utterances) {
				AppendText (utt.Text);
				AppendText (" ");
			}

			Finished ();
		}

	}
}
