//
// MoveMail: Adds Unix Mailspool (MoveMail) indexing support to the Thunderbird backend
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
	
	[ThunderbirdIndexableGenerator (TB.AccountType.MoveMail, "Movemail Support", false)]
	public class MoveMailIndexableGenerator : ThunderbirdIndexableGenerator {
	
		public MoveMailIndexableGenerator (ThunderbirdIndexer indexer, TB.Account account, string file)
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
			} while (IsUpToDate ((DbEnumerator.Current as TB.MoveMail).Uri));
			
			return true;
		}
		
		public override Indexable GetNextIndexable ()
		{
			TB.MoveMail message = DbEnumerator.Current as TB.MoveMail;
			
			// If status is different, than something happend when loading this mail and we dont'
			// want to change it's status.
			if (message.GetObject ("FullIndex") == null)
				message.SetObject ("FullIndex", (object) FullIndex);
			
			return MoveMailToIndexable (message);
		}
		
		public override void LoadDatabase ()
		{
			try {
				db = new TB.Database (account, DbFile);
				db.Load ();
			} catch (Exception e) {
				Logger.Log.Warn (e, "Failed to load {0}", DbFile);
				return;
			}
			
			if (db.Count <= 0)
				return;
			
			FullIndex = (Thunderbird.IsFullyIndexable (DbFile) ? true : false);
			Logger.Log.Info ("Indexing {0} Movemails ({1})", db.Count, RelativePath);
		}
		
		// FIXME: This need some more info
		private Indexable MoveMailToIndexable (TB.MoveMail mail)
		{
			Indexable indexable;

			indexable = NewIndexable (mail.Uri, DateTime.UtcNow, "MailMessage");
			indexable.MimeType = "message/rfc822";
			
			string subject = GMime.Utils.HeaderDecodePhrase (mail.GetString ("subject"));
			indexable.AddProperty (Property.New ("dc:title", subject));
			
			return indexable;
		}
	}
}
