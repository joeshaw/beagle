using System;
using System.Collections;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle.Daemon {

	public class RemoteIndexerRequest : RequestMessage {

		public string RemoteIndexName;

		ArrayList indexables_to_add = new ArrayList ();
		ArrayList uris_to_remove = new ArrayList ();
		ArrayList uris_to_rename = new ArrayList (); // paired, in the usual stupid fashion

		public RemoteIndexerRequest () : base ("socket-helper")
		{
		}

		public void Add (Indexable indexable)
		{
			indexables_to_add.Add (indexable);
		}

		public void Remove (Uri uri)
		{
			uris_to_remove.Add (uri);
		}

		public void Rename (Uri old_uri, Uri new_uri)
		{
			uris_to_rename.Add (old_uri);
			uris_to_rename.Add (new_uri);
		}

		[XmlArrayItem (ElementName="Indexable", Type=typeof(Indexable))]
		[XmlArray (ElementName="ToAdd")]
		public ArrayList ToAdd {
			get { return indexables_to_add; }
		}

		[XmlAttribute ("ToRemove")]
		public string ToRemoveString {
			get { return UriFu.UrisToString (uris_to_remove); }
			set { 
				uris_to_remove = new ArrayList ();
				uris_to_remove.AddRange (UriFu.StringToUris (value));
			}
		}

		[XmlAttribute ("ToRename")]
		public string ToRenameString {
			get { return UriFu.UrisToString (uris_to_rename); }
			set { 
				uris_to_rename = new ArrayList ();
				uris_to_rename.AddRange (UriFu.StringToUris (value));
			}
		}

		////////////////////////////////////////////////////////////////////////////

		public void Process (IIndexer indexer)
		{
			foreach (Indexable indexable in indexables_to_add)
				indexer.Add (indexable);

			foreach (Uri uri in uris_to_remove)
				indexer.Remove (uri);

			int i = 0;
			while (i < uris_to_rename.Count - 1) {
				Uri old_uri = uris_to_rename [i] as Uri;
				Uri new_uri = uris_to_rename [i+1] as Uri;
				indexer.Rename (old_uri, new_uri);
				i += 2;
			}
			
			indexer.Flush ();
		}

		public void FireEvent (IIndexer source, IIndexerChangedHandler handler)
		{
			if (handler == null)
				return;
			
			ArrayList uris_to_add = new ArrayList ();
			foreach (Indexable indexable in indexables_to_add)
				uris_to_add.Add (indexable.Uri);
			
			handler (source,
				 uris_to_add,
				 uris_to_remove,
				 uris_to_rename);
		}

	}
}
