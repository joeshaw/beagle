using System;
using System.Collections;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle {

        // The various async responses from a Query request

	public class HitsAddedResponse : ResponseMessage {
		[XmlArray (ElementName="Hits")]
		[XmlArrayItem (ElementName="Hit", Type=typeof (Hit))]
		public ArrayList Hits;

		public HitsAddedResponse () { }

		public HitsAddedResponse (ICollection hits)
		{
			this.Hits = new ArrayList (hits);
		}
	}

	public class HitsSubtractedResponse : ResponseMessage {
		private ICollection uris;

		public HitsSubtractedResponse () { }

		public HitsSubtractedResponse (ICollection uris)
		{
			this.uris = uris;
		}

		[XmlIgnore]
		public ICollection Uris {
			get { return this.uris; }
		}

		[XmlArray (ElementName="Uris")]
		[XmlArrayItem (ElementName="Uri")]
		public string[] UrisAsStrings {
			get {
				string[] uris = new string [this.uris.Count];
				
				int i = 0;
				foreach (Uri uri in this.uris) {
					uris [i] = UriFu.UriToSerializableString (uri);
					i++;
				}

				return uris;
			}

			set {
				int N = value.Length;
				Uri[] uris = new Uri [N];

				for (int i = 0; i < N; i++)
					uris [i] = UriFu.UriStringToUri (value [i]);

				this.uris = uris;
			}
		}
	}

	public class FinishedResponse : ResponseMessage { }
	public class CancelledResponse : ResponseMessage { }
}