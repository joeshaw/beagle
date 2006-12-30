
using System;
using System.Collections;

using Beagle.Util;

namespace Bludgeon {

	public class EventTracker {

		private enum EventType {
			Added,
			Subtracted
		}

		private class Event {
			public EventType Type;
			public string Uri;
		}

		object big_lock = new object ();
		ArrayList expected_events = new ArrayList ();

		public EventTracker ()
		{
			
		}

		////////////////////////////////////////////////////////

		public void ExpectingAdded (string uri)
		{
			lock (big_lock) {
				
				// Adding anything always generates a paired
				// subtracted event.
				ExpectingSubtracted (uri);

				Event ev = new Event ();
				ev.Type = EventType.Added;
				ev.Uri = uri;
				expected_events.Add (ev);

				//Log.Spew ("Expecting Added {0}", uri);
			}
		}

		public void ExpectingSubtracted (string uri)
		{
			lock (big_lock) {
				Event ev = new Event ();
				ev.Type = EventType.Subtracted;
				ev.Uri = uri;
				expected_events.Add (ev);

				//Log.Spew ("Expecting Subtracted {0}", uri);
			}
		}

		////////////////////////////////////////////////////////

		private void GotEvent (EventType type, string uri)
		{
			lock (big_lock) {
				for (int i = 0; i < expected_events.Count; ++i) {
					Event ev = (Event) expected_events [i];
					if (ev.Type == type && ev.Uri == uri) {
						expected_events.RemoveAt (i);
						return;
					}
				}
				throw new Exception ("Spurious event " + type + " " + uri);
			}
		}

		public void GotAddedEvent (string uri)
		{
			GotEvent (EventType.Added, uri);
		}

		public void GotSubtractedEvent (string uri)
		{
			GotEvent (EventType.Subtracted, uri);
		}
		
	}

}
