using System;
using System.Diagnostics;
using System.Collections;
using Mono.Unix;
using Beagle.Util;

using Beagle;

namespace Search.Tiles {

	public class IMLogActivator : TileActivator {

		public IMLogActivator () : base ()
		{
			AddSupportedFlavor (new HitFlavor (null, "IMLog", null));
		}

		public override Tile BuildTile (Beagle.Hit hit, Beagle.Query query)
		{
			return new IMLog (hit, query);
		}
	}

	public class IMLog : TileFlat {

		private static Hashtable all_icons = new Hashtable ();
		private static Hashtable email_cache = new Hashtable ();

		public IMLog (Beagle.Hit hit, Beagle.Query query) : base (hit, query)
		{
			Group = TileGroup.Conversations;

			subject.LabelProp = Catalog.GetString ("IM Conversation");
			from.LabelProp = "<b>" + hit.GetFirstProperty ("fixme:speakingto") + "</b>";

			try {
				Timestamp = Utils.ParseTimestamp (hit.GetFirstProperty ("fixme:starttime"));
				date.LabelProp = Utils.NiceShortDate (Timestamp);
			} catch {}

			MaybeAddEmailAction ();
		}

		private Hashtable IconsForSize (int size)
		{
			Hashtable icons = new Hashtable ();

			icons ["aim"] = WidgetFu.LoadThemeIcon ("im-aim", size);
			icons ["icq"] = WidgetFu.LoadThemeIcon ("im-icq", size);
			icons ["jabber"] = WidgetFu.LoadThemeIcon ("im-jabber", size);
			icons ["msn"] = WidgetFu.LoadThemeIcon ("im-msn", size);
			icons ["novell"] = WidgetFu.LoadThemeIcon ("im-nov", size);
			icons ["yahoo"] = WidgetFu.LoadThemeIcon ("im-yahoo", size);

			return icons;
		}

		protected override void LoadIcon (Gtk.Image image, int size)
		{
			if (size > 32) {
				// FIXME: We do not respect the icon size request
				Gdk.Pixbuf icon = LoadBuddyIcon ();
				
				if (icon != null) {
					image.Pixbuf = icon;
					return;
				}
			}

			Hashtable icons = (Hashtable)all_icons[size];
			if (icons == null)
				all_icons[size] = icons = IconsForSize (size);

			string protocol = Hit.GetFirstProperty ("fixme:protocol");
			if (icons [protocol] != null)
				image.Pixbuf = (Gdk.Pixbuf)icons [protocol];
			else
				image.Pixbuf = WidgetFu.LoadThemeIcon ("im", size);
		}

		private void MaybeAddEmailAction ()
		{
			string alias = Hit.GetFirstProperty ("fixme:speakingto_alias");

			if (alias == null)
				return;

			Console.WriteLine ("Non null alias!  {0}", alias);

			string email;

			if (email_cache.Contains (alias)) {
				email = (string) email_cache [alias];

				if (email == null)
					return;

				AddAction (new TileAction (Catalog.GetString ("Email"), Email));
			} else
				RequestEmail (alias);
		}

		private Query email_query = null;
		private string alias = null;

		private void RequestEmail (string alias)
		{
			this.alias = alias;

			email_query = new Query ();
			email_query.AddDomain (QueryDomain.Local);
			
			QueryPart_Property part = new QueryPart_Property ();
			part.Logic = QueryPartLogic.Required;
			part.Type = PropertyType.Text;
			part.Key = "fixme:FullName";
			part.Value = alias;
			email_query.AddPart (part);

			part = new QueryPart_Property ();
			part.Logic = QueryPartLogic.Required;
			part.Type = PropertyType.Keyword;
			part.Key = "beagle:Source";
			part.Value = "EvolutionDataServer";
			email_query.AddPart (part);

			email_query.HitsAddedEvent += OnHitsAdded;
			email_query.FinishedEvent += OnFinished;

			Console.WriteLine ("Sending query!");

			email_query.SendAsync ();
		}

		private void OnHitsAdded (HitsAddedResponse response)
		{
			Console.WriteLine ("Got hits!");

			// FIXME: Handle multiple hits?
			if (response.Hits.Count == 0) {
				// Nothing matches
				email_cache [alias] = null;
				return;
			}

			Hit email_hit = (Hit) response.Hits [0];
			string email = email_hit.GetFirstProperty ("fixme:Email");

			email_cache [alias] = email;
			AddAction (new TileAction (Catalog.GetString ("Email"), Email));
		}
			
		private void OnFinished (FinishedResponse response)
		{
			Console.WriteLine ("Finished query!");

			email_query.HitsAddedEvent -= OnHitsAdded;
			email_query.FinishedEvent -= OnFinished;

			alias = null;
			email_query = null;
		}

		private Gdk.Pixbuf LoadBuddyIcon ()
		{
			Gdk.Pixbuf icon = null;

			if (Hit ["fixme:speakingto_icon"] != null && System.IO.File.Exists (Hit ["fixme:speakingto_icon"]))
				icon = new Gdk.Pixbuf (Hit ["fixme:speakingto_icon"]);

			return icon;				
		}

		protected override DetailsPane GetDetails ()
		{
			DetailsPane details = new DetailsPane ();

			details.AddLabelPair (Catalog.GetString ("Name:"), FromLabel.Text, 0, 1);
			details.AddLabelPair (Catalog.GetString ("Date Received:"), DateLabel.Text, 1, 1);

			GotSnippet += SetSubject;
			details.AddSnippet (2, 1);

			return details;
		}

		private void SetSubject (string snippet)
		{
			subject.Markup = snippet;
		}

		public override void Open ()
		{
			SafeProcess p = new SafeProcess ();
			p.Arguments = new string [] { "beagle-imlogviewer",
						      "--client", Hit ["fixme:client"],
						      "--highlight-search", Query.QuotedText,
						      Hit.Uri.LocalPath };

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Unable to run {0}: {1}", p.Arguments [0], e.Message);
			}
		}

		public void Email ()
		{
			string alias = Hit.GetFirstProperty ("fixme:speakingto_alias");

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "evolution";
			p.StartInfo.Arguments = String.Format ("\"mailto:{0}\"", email_cache [alias]);

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Error launching Evolution composer: " + e);
			}
		}
	}
}
