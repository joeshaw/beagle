//
//  Zeroconf.cs
//
//  Copyright (c) 2006 Kyle Ambroff <kambroff@csus.edu>
//  Copyright (c) 2007 Lukas Lipka <lukaslipka@gmail.com>
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
using System.Collections;
using System.Xml.Serialization;

using Avahi;

using Beagle.Util;

namespace Beagle.Daemon.Network {        

        public class Zeroconf : IDisposable {

		public static string DOMAIN = "local";
                public static string PROTOCOL = "_beagle._tcp";
		
                private string name = null;
                private ushort port = 4000;
                private bool enabled = false;

                private EntryGroup entry_group = null;
                private Client client = null;
                private int collisions = 0;
                
                public Zeroconf ()
		{
			Logger.Log.Debug ("Zeroconf: Service started...");

			LoadConfiguration ();
                        Conf.Subscribe (typeof (Conf.NetworkingConfig), new Conf.ConfigUpdateHandler (OnConfigurationChanged));
		}
                

                public void Dispose ()
                {
			Logger.Log.Debug ("Zeroconf: Service stopped...");

			if (entry_group != null) {
				entry_group.Dispose ();
				entry_group = null;
			}

			if (client != null) {
				client.Dispose ();
				client = null;
			}
                }

		private void LoadConfiguration ()
		{
			if (String.IsNullOrEmpty (name))
				name = Conf.Networking.ServiceName;

			// Check to see if our enabled status hasn't changed
                        if (enabled != Conf.Networking.ServiceEnabled) {
                                enabled = Conf.Networking.ServiceEnabled;
                                
                                if (enabled)
					Publish ();
				else
					Unpublish ();
                                
                                Logger.Log.Info ("Zeroconf: Index sharing is {0}", enabled ? "enabled" : "disabled");
                        }

                        // Handle index name changes
                        if (String.Compare (name, Conf.Networking.ServiceName) != 0)
				Update ();
                }

		private void OnConfigurationChanged (Conf.Section section)
                {
			LoadConfiguration ();
		}

                private void Publish ()
                {
                        if (client == null)
                                client = new Client ();
                        
                        try {
				string [] args = new string [] { "Password=" + (Conf.Networking.PasswordRequired ? "true" : "false") };
					
				if (collisions > 0)
					name += String.Format (" ({0})", collisions);
                                        
				entry_group = new EntryGroup (client);
				entry_group.AddService (name, PROTOCOL, DOMAIN, port, args);
				entry_group.StateChanged += OnEntryGroupStateChanged;
				entry_group.Commit ();                               
                        } catch (ClientException e) {
                                if (e.ErrorCode == ErrorCode.Collision) {
                                        HandleCollision ();
                                        return;
                                } else {
                                        throw;
                                }
                        } catch (Exception e) {
                                Logger.Log.Error (e, "Zeroconf: Failed to publish service - '{0}'", name);
                                // FIXME: Shutdown or unpublish the service
			}
                }

                private void Unpublish ()
                {
			if (entry_group == null)
				return;

                        //entry_group.Reset ();
                        entry_group.Dispose ();
			entry_group = null;
                }

                private void Update ()
                {
                        //entry_group.UpdateService (Conf.Networking.ServiceName, Zeroconf.PROTOCOL, Zeroconf.DOMAIN);

                        // FIXME: Do this to get around a bug in avahi-sharp
			Unpublish ();
                        Publish ();
                }

                private void HandleCollision ()
                {
                        Logger.Log.Info ("Zeroconf: Service name collision - '{0}'", name);

                        Unpublish ();
                        collisions++;
                        Publish ();       
                }
                
                private void OnEntryGroupStateChanged (object sender, EntryGroupStateArgs args)
                {
                        Logger.Log.Debug ("Zeroconf: Service state changed: {0}", args.State);

                        if (args.State == EntryGroupState.Collision)
                                HandleCollision ();
                }

		public string Name {
                        get { return name; }
                }

                public ushort Port {
                        get { return port; }
                }

                public bool Enabled {
                        get { return enabled; }
                }
        }
}
