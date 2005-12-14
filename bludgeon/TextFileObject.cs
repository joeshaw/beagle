
using System;
using System.Collections;
using System.IO;
using System.Text;

using Beagle.Util;
using Beagle;

namespace Bludgeon {

	public class TextFileObject : FileObject {

		const int body_size = 10;
		string [] body;

		public TextFileObject ()
		{
			body = new string [body_size];
			for (int i = 0; i < body_size; ++i)
				body [i] = Token.GetRandom ();
		}

		override public string MimeType {
			get { return "text/plain"; }
		}

		override public string Extension {
			get { return ".txt"; }
		}

		override public void AddToStream (Stream stream, EventTracker tracker)
		{
			if (tracker != null)
				tracker.ExpectingAdded (this.Uri);
			
			// We can't just use a StreamWriter here, since that
			// will close the underlying stream when it gets
			// disposed.
			UnclosableStream unclosable = new UnclosableStream (stream);
			StreamWriter writer = new StreamWriter (unclosable);

			foreach (string str in body) 
				writer.WriteLine (str);

			writer.Close ();
		}

		override protected bool MatchesQueryPart (QueryPart abstract_part)
		{
			if (abstract_part is QueryPart_Text) {
				QueryPart_Text part;
				part = (QueryPart_Text) abstract_part;

				if (part.SearchFullText)
					foreach (string str in body)
						if (part.Text == str)
							return true;
			}

			return false;
		}

	}

}
