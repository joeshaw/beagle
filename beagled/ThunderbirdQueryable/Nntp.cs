//
// Nntp.cs: Adds NNTP indexing support to the Thunderbird backend
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

using GMime;
	
namespace Beagle.Daemon.ThunderbirdQueryable {	
	
	[ThunderbirdIndexableGenerator (TB.AccountType.Nntp, "NNTP Support", false)]
	public class NntpIndexableGenerator : ThunderbirdIndexableGenerator {
	
		public NntpIndexableGenerator (ThunderbirdIndexer indexer, TB.Account account, string file)
			: base (indexer, account, file)
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
			} while (IsUpToDate ((DbEnumerator.Current as TB.NntpMessage).Uri));
			
			return true;
		}
		
		public override Indexable GetNextIndexable ()
		{
			TB.NntpMessage message = DbEnumerator.Current as TB.NntpMessage;
			
			// If status is different, than something happend when loading this mail and we dont'
			// want to change it's status.
			if (message.GetObject ("FullIndex") == null)
				message.SetObject ("FullIndex", (object) FullIndex);
			
			return NntpMessageToIndexable (message);
		}
		
		public override void LoadDatabase ()
		{
			try {
				db = new TB.Database (account, DbFile);
				db.Load ();
			} catch (Exception e) {
				Logger.Log.Warn (e, "Failed to load {0}:", DbFile);
				return;
			}
			
			if (db.Count <= 0) {
				Logger.Log.Debug ("Empty file {0}; skipping", DbFile);
				return;
			}
			
			FullIndex = (Thunderbird.IsFullyIndexable (DbFile) ? true : false);
			Logger.Log.Info ("Indexing {0} NNTP messages", db.Count);
		}
		
		// FIXME: This need some more info
		private Indexable NntpMessageToIndexable (TB.NntpMessage message)
		{
			Indexable indexable;
			
			indexable = new Indexable (message.Uri);
			indexable.HitType = "MailMessage";
			indexable.MimeType = "message/rfc822";
			indexable.Timestamp = DateTime.Parse (message.GetString ("date")).ToUniversalTime ();
			
			indexable.AddProperty (Property.NewKeyword ("fixme:client", "thunderbird"));
			indexable.AddProperty (Property.NewUnsearched ("fixme:fullyIndexed", message.GetBool ("FullIndex")));
			indexable.AddProperty (Property.NewDate ("fixme:indexDateTime", DateTime.UtcNow));
			
			string subject = GMime.Utils.HeaderDecodePhrase (message.GetString ("subject"));
			indexable.AddProperty (Property.New ("dc:title", subject));
			
			return indexable;
		}
	}
}
