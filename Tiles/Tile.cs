//
// Tile.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Diagnostics;
using Gtk;
using BU = Beagle.Util;

namespace Beagle.Tile {

	public delegate void TileActionHandler ();
	public delegate void TileChangedHandler (Tile tile);

	public abstract class Tile {

		static private object uidSrcLock = new object ();
		static private long uidSrc = 0;

		private long uid;
		private Uri uri;
		private Query query;
		
		public Tile ()
		{
			lock (uidSrcLock) {
				++uidSrc;
				uid = uidSrc;
			}
		}			

		public string UniqueKey {
			get { return "_tile_" + uid; }
		}

		public Uri Uri {
			get { return uri; }
			set { uri = value; }
		}

		public Query Query {
			get { return query; }
			set { query = value; }
		}

		////////////////////////

		abstract public void Render (TileRenderContext ctx);

		////////////////////////

		virtual public bool HandleUrlRequest (string url)
		{
			return false;
		}

		////////////////////////

		private TileChangedHandler changedHandler = null;

		public void SetChangedHandler (TileChangedHandler ch)
		{
			changedHandler = ch;
		}

		protected virtual void Changed ()
		{
			if (changedHandler != null)
				changedHandler (this);
		}

		private void OnErrorDialogResponse (object o, ResponseArgs args)
		{
			((MessageDialog)o).Destroy ();
		}


		protected void LaunchError (string format, params string[] args)
		{
			string message = String.Format (format, args);
			MessageDialog dlg = new MessageDialog (null,
							       0,
							       MessageType.Error,
							       ButtonsType.Ok,
							       message);
			
			dlg.Response += OnErrorDialogResponse;
			dlg.Show ();

		}

		[TileAction]
		public virtual void Open ()
		{
			System.Console.WriteLine ("Warning: Open method not implemented for this tile type");
		}

		protected void OpenFolder (string path)
		{
			if (path == null || path == "")
				return;
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "nautilus";
			if ((!path.StartsWith ("\"")) && (!path.EndsWith ("\"")))
				path = "\"" + path + "\"";
			p.StartInfo.Arguments = path;

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Cannot open folder in Nautilus: " + e);
			}
		}

		protected void OpenFromMime (Hit hit)
		{
			OpenFromMime (hit, null, false);
		}

		protected void OpenFromMime (Hit hit,
					     string command_fallback,
					     bool expects_uris_fallback)
		{
			string argument = null;
			string command = command_fallback;
			bool expects_uris = expects_uris_fallback;
			
			BU.GnomeVFSMimeApplication app;
			app = BU.GnomeIconLookup.GetDefaultAction (hit.MimeType);
			
			if (app.command != null) {
				command = app.command;
				expects_uris = (app.expects_uris != BU.GnomeVFSMimeApplicationArgumentType.Path);
			}

			if (command == null) {
				LaunchError ("Can't open MimeType '{0}'", hit.MimeType);
				return;
			}
			
			if (expects_uris) {
				argument = String.Format ("'{0}'", hit.Uri);
			} else {
				argument = hit.PathQuoted;
			}
			
			Console.WriteLine ("Cmd: {0}", command);
			Console.WriteLine ("Arg: {0}", argument);

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = command;
			p.StartInfo.Arguments = argument;

			try {
				p.Start ();
			} catch (Exception e) {
				Console.WriteLine ("Error in OpenFromMime: " + e);
			}
		}

		protected void SendFile (string attach)
		{
			if ((attach == null || attach == "")) {
				Console.WriteLine ("SendFile got empty attachment");
				return;
			}

			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName = "nautilus-sendto";
			p.StartInfo.Arguments = "--default-dir=/ " + attach;

			try {
				p.Start () ;
			} catch (Exception e) {
				// Fall back to just email
				SendMailToAddress (null, attach);
			}
		}

		protected void SendMailToAddress (string email, string attach)
		{
			if ((email == null || email == "") && (attach == null || attach == "")) {
				Console.WriteLine ("SendMail got empty email address and attachment");
				return;
			}
			
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName        = "evolution";
			p.StartInfo.Arguments       = "mailto:";

			if (email != null && email != "")
				p.StartInfo.Arguments += email;

			if (attach != null && attach != "")
				p.StartInfo.Arguments += "?attach=" + attach;
			
			try {
				p.Start () ;
			} catch (Exception e) {
				Console.WriteLine ("Error launching Evolution composer: " + e);
			}
		}

		protected void SendIm (string protocol,
				       string screenname)
		{
			if (screenname == null)
				return;

			Console.WriteLine ("SendImAim {0}", screenname);
			Process p = new Process ();
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.FileName        = "gaim-remote";
			p.StartInfo.Arguments       = "uri " + protocol + ":goim?screenname=" + screenname;

			try {
				p.Start () ;
			} catch (Exception e) {
				Console.WriteLine ("Error launching gaim-remote: " + e);
			}
		}

		protected void SendImAim (string screenname)
		{
			SendIm ("aim", screenname);
		}

		protected void SendImIcq (string screenname)
		{
			SendIm ("icq", screenname);
		}

		protected void SendImJabber (string screenname)
		{
			SendIm ("jabber", screenname);
		}

		protected void SendImMsn (string screenname)
		{
			SendIm ("msn", screenname);
		}

		protected void SendImYahoo (string screenname)
		{
			SendIm ("yahoo", screenname);
		}

		protected void SendImGroupwise (string screenname)
		{
			SendIm ("novell", screenname);
		}			
	}
}
	
