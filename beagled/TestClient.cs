namespace BeagleDaemonTest {
	using DBus;
	using Beagle;

	public class TestClient {
		public static int Main (string[] args) {
			Gtk.Application.Init ();
			Connection connection = Bus.GetSessionBus ();

			Service service = Service.Get (connection,
						       "com.novell.Beagle");

			QueryManager manager = 
				(QueryManager) service.GetObject (typeof (QueryManager),
								  "/com/novell/Beagle/QueryManager");
			
			string queryPath = manager.NewQuery ();
			Query query = (Query)service.GetObject (typeof (Query),
								queryPath);

			query.GotHitsEvent += OnGotHits;

			query.AddText (args[0]);
			query.Run ();

			Gtk.Application.Run ();
			return 0;
		}

		static private void OnGotHits (string stringHits) {
			System.Collections.ArrayList hits = Hit.ReadHitXml (stringHits);
			System.Console.WriteLine ("got hits");
		} 
	}
}
