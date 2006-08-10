//
// Rss.cs: Adds RSS feed indexing support to the Thunderbird backend
//
// Copyright (C) 2006 Pierre Östlund
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
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;

using Beagle.Util;
using Beagle.Daemon;
using TB = Beagle.Util.Thunderbird;

namespace Beagle.Daemon.ThunderbirdQueryable {	

	[ThunderbirdIndexableGenerator (TB.AccountType.Rss, "RSS Support", true)]
	public class RssIndexableGenerator : ThunderbirdIndexableGenerator {
		private string feed_url;
		
		public RssIndexableGenerator (ThunderbirdIndexer indexer, TB.Account account, string mork_file)
			: base (indexer, account, mork_file)
		{
		}
			
		public override bool HasNextIndexable ()
		{
			do {
				if (DbEnumerator == null || !DbEnumerator.MoveNext ()) {
					Done = true;
					indexer.NotificationEvent -= OnNotification;
					indexer.ChildComplete ();
					return false;
				}
			} while (IsUpToDate ((DbEnumerator.Current as TB.RssFeed).Uri));
			
			return true;
		}
		
		public override Indexable GetNextIndexable ()
		{
			TB.RssFeed feed = DbEnumerator.Current as TB.RssFeed;
			
			// If status is different, than something happend when loading this mail and we dont'
			// want to change it's status.
			if (feed.GetObject ("FullIndex") == null)
				feed.SetObject ("FullIndex", (object) FullIndex);
			
			return RssFeedToIndexable (feed);
		}
		
		public override void LoadDatabase ()
		{
			string folder_name = null;
			
			try {
				db = new TB.Database (account, DbFile);
				db.Load ();
				
				Hashtable tbl = db.Db.Compile ("1", "ns:msg:db:row:scope:dbfolderinfo:all");
				feed_url = tbl ["feedUrl"] as string;
				folder_name = tbl ["folderName"] as string;
			} catch (Exception e) {
				Logger.Log.Warn (e, "Failed to load {0}:", DbFile);
				return;
			}
			
			if (db.Count <= 0)
				return;
			
			Logger.Log.Info ("Indexing \"{0}\" RSS feed containing {1} entries ({2})", folder_name, db.Count, RelativePath);
		}
		
		private Indexable RssFeedToIndexable (TB.RssFeed feed)
		{
			Indexable indexable;
			StringReader content = feed.Content;
			
			indexable = NewIndexable (feed.Uri, DateTime.Parse (feed.GetString ("date")).ToUniversalTime (), "FeedItem");
			indexable.MimeType = "text/html";
			
			indexable.AddProperty (Property.NewKeyword ("dc:identifier", feed.GetString ("message-id")));
			indexable.AddProperty (Property.NewKeyword ("dc:source", feed_url));
			indexable.AddProperty (Property.New ("dc:publisher", feed.GetString ("sender")));
			if (content == null)
				indexable.AddProperty (Property.New ("dc:title", feed.GetString ("subject")));
			
			indexable.SetTextReader (content);
			
			return indexable;
		}
	}
}
