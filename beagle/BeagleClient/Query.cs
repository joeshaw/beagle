//
// Query.cs
//
// Copyright (C) 2004-2005 Novell, Inc.
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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

using Beagle.Util;

namespace Beagle {

	[Flags]
	public enum QueryDomain {
		Local        = 1,
		System       = 2,
		Neighborhood = 4,
		Global       = 8,
		All          = 15 /* 1 | 2 | 4 | 8 */
	}

	public class Query : RequestMessage {

		// FIXME: This is a good default when on an airplane.
		private Beagle.QueryDomain domain_flags = QueryDomain.Local | QueryDomain.System; 

		private ArrayList parts = new ArrayList ();
		private ArrayList mimeTypes = new ArrayList ();
		private ArrayList hitTypes = new ArrayList ();
		private ArrayList searchSources = new ArrayList ();

		private ArrayList exact_text = null;
		private ArrayList stemmed_text = null;

		private QueryPart_Or mime_type_part = null;
		private QueryPart_Or hit_type_part = null;
		private QueryPart_Or source_part = null;

#if ENABLE_AVAHI
		private AvahiBrowser avahi_browser = null;
#endif

		private bool is_index_listener = false;
		
		private int nodes_finished = 0;

		// Events to make things nicer to clients
		public delegate void HitsAdded (HitsAddedResponse response);
		public event HitsAdded HitsAddedEvent;

		public delegate void HitsSubtracted (HitsSubtractedResponse response);
		public event HitsSubtracted HitsSubtractedEvent;

		public delegate void Finished (FinishedResponse response);
		public event Finished FinishedEvent;

#if ENABLE_AVAHI
		public delegate void HostFound (object o, AvahiEventArgs args);
		public event HostFound UnknownHostFoundEvent;
#endif

		public Query () : base (true)
		{
			this.RegisterAsyncResponseHandler (typeof (HitsAddedResponse), OnHitsAdded);
			this.RegisterAsyncResponseHandler (typeof (HitsSubtractedResponse), OnHitsSubtracted);
			this.RegisterAsyncResponseHandler (typeof (FinishedResponse), OnFinished);
			this.RegisterAsyncResponseHandler (typeof (ErrorResponse), OnError);
			this.RegisterAsyncResponseHandler (typeof (SearchTermResponse), OnSearchTerms);

#if ENABLE_AVAHI
                        avahi_browser = new AvahiBrowser ();
                        avahi_browser.HostFound += new AvahiEventHandler (OnHostFound);
                        avahi_browser.HostRemoved += new AvahiEventHandler (OnHostRemoved);
                        avahi_browser.Start ();
#endif
		}

		public Query (string str) : this ()
		{
			AddText (str);
		}

		~Query ()
		{
#if ENABLE_AVAHI
			avahi_browser.Dispose ();
#endif
		}

		///////////////////////////////////////////////////////////////

		private void OnHitsAdded (ResponseMessage r)
		{
			HitsAddedResponse response = (HitsAddedResponse) r;

			if (this.HitsAddedEvent != null)
				this.HitsAddedEvent (response);
		}

		private void OnHitsSubtracted (ResponseMessage r)
		{
			HitsSubtractedResponse response = (HitsSubtractedResponse) r;

			if (this.HitsSubtractedEvent != null)
				this.HitsSubtractedEvent (response);
		}

		private void OnFinished (ResponseMessage r)
		{
			FinishedResponse response = (FinishedResponse) r;
	
			if (this.FinishedEvent != null)
				this.FinishedEvent (response);
				
			// FIXME: This needs to be fixed when we break API
			// so that the FinishedEvent is sent when all nodes finish.
			if (++nodes_finished == clients.Count && this.FinishedEvent != null) {
				Logger.Log.Debug ("Query: All nodes finished!"); 
				//this.FinishedEvent ();
			}
		}

		private void OnError (ResponseMessage r)
		{
			ErrorResponse response = (ErrorResponse) r;
			
			Logger.Log.Warn ("Query: Error returned by a client: {0}", response.ErrorMessage );
			
			// FIXME: This needs to be fixed when we break API
			// so that the FinishedEvent is sent when all nodes finish.
			if (++nodes_finished == clients.Count && this.FinishedEvent != null) {
				Logger.Log.Debug ("Query: All nodes done on error!"); 
				//this.FinishedEvent ();
			}

			//FIXME: Ignore ErrorResponses from network nodes or let the user decide
			throw new ResponseMessageException (response);
		}

		private void OnSearchTerms (ResponseMessage r)
		{
			SearchTermResponse response = (SearchTermResponse) r;
			ProcessSearchTermResponse (response);
		}

		///////////////////////////////////////////////////////////////

#if ENABLE_AVAHI
                private void OnHostFound (object sender, AvahiEventArgs args)
                {
                        // Here we should add this host to the current query if
                        // we recognize it as a pre-registered host.
                        foreach (NetworkService service in Conf.Networking.NetworkServices) {
				if (service.Cookie == args.Service.Cookie && service.Name == args.Service.Name) {
					// FIXME: Here is where we add the host to the current query!!!
                                        Logger.Log.Debug ("NetworkService: Found service '{0}', adding to query...", service.Name);
                                        return;
                                }
                        }
                        
                        Logger.Log.Debug ("NetworkService: Unknown service '{0}' has been detected...", args.Service.Name);

                        if (UnknownHostFoundEvent != null)
                                UnknownHostFoundEvent (this, args);
                }

                private void OnHostRemoved (object sender, AvahiEventArgs args)
                {
                        // FIXME: This host should now be removed from the current query
                        Logger.Log.Debug ("NetworkService: Service '{0}' has been removed...", args.Service.Name);
                }
#endif

		///////////////////////////////////////////////////////////////

		// This is exposed for the benefit of QueryDriver.DoQueryLocal
		public void ProcessSearchTermResponse (SearchTermResponse response)
		{
			exact_text = response.ExactText;
			stemmed_text = response.StemmedText;
		}

		///////////////////////////////////////////////////////////////

		// Warning: For the moment, the daemon is allowed to IGNORE
		// index listener queries at its discretion... so don't assume
		// that they will work for you!  Listener queries should only be
		// used for debugging and testing.

		public bool IsIndexListener {
			set { is_index_listener = value; }
			get { return is_index_listener; }
		}

		///////////////////////////////////////////////////////////////

		public void ClearParts ()
		{
			if (parts != null)
				parts.Clear ();
		}

		public void AddPart (QueryPart part)
		{
			if (part != null)
				parts.Add (part);
		}

		// This is a human-entered query string that will be parsed in
		// the daemon.
		public void AddText (string str)
		{
			QueryPart_Human part;
			part = new QueryPart_Human ();
			part.QueryString = str;
			AddPart (part);
		}

		[XmlArrayItem (ElementName="Part", Type=typeof (QueryPart))]
		[XmlArray (ElementName="Parts")]
		public ArrayList Parts {
			get { return parts; }
		}

		[XmlIgnore]
		public ICollection Text {
			get { return exact_text; }
		}

		[XmlIgnore]
		public string QuotedText {
			get {
				StringBuilder builder = new StringBuilder ();
				foreach (string text in Text) {
					string text_cooked = text;
					if (builder.Length > 0)
						builder.Append (' ');
					bool contains_space = (text.IndexOf (' ') != -1);
					if (contains_space) {
						text_cooked = text.Replace ("\"", "\\\"");
						builder.Append ('"');
					}
					builder.Append (text_cooked);
					if (contains_space)
						builder.Append ('"');
				}
				return builder.ToString ();
			}
		}

		[XmlIgnore]
		public ICollection StemmedText {
			get { return stemmed_text; }
		}
						
		///////////////////////////////////////////////////////////////

		// This API is DEPRECATED.
		// The mime type is now stored in the beagle:MimeType property.
		// To restrict on mime type, just do a normal property query.

		public void AddMimeType (string str)
		{
			mimeTypes.Add (str);

			if (mime_type_part == null) {
				mime_type_part = new QueryPart_Or ();
				AddPart (mime_type_part);
			}

			// Create a part for this mime type.
			QueryPart_Property part;
			part = new QueryPart_Property ();
			part.Type = PropertyType.Keyword;
			part.Key = "beagle:MimeType";
			part.Value = str;
			mime_type_part.Add (part);
		}

		public bool AllowsMimeType (string str)
		{
			if (mimeTypes.Count == 0)
				return true;
			foreach (string mt in mimeTypes)
				if (str == mt)
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="MimeType", Type=typeof (string))]
		[XmlArray (ElementName="MimeTypes")]
		public ArrayList MimeTypes {
			get { return mimeTypes; }
		}

		public bool HasMimeTypes {
			get { return mimeTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		// This API is DEPRECATED.
		// The hit type is now stored in the beagle:HitType property.
		// To restrict on type, just do a normal property query.

		public void AddHitType (string str)
		{
			hitTypes.Add (str);

			if (hit_type_part == null) {
				hit_type_part = new QueryPart_Or ();
				AddPart (hit_type_part);
			}

			// Add a part for this hit type.
			QueryPart_Property part;
			part = new QueryPart_Property ();
			part.Type = PropertyType.Keyword;
			part.Key = "beagle:HitType";
			part.Value = str;
			hit_type_part.Add (part);
		}

		public bool AllowsHitType (string str)
		{
			if (hitTypes.Count == 0)
				return true;
			foreach (string ht in hitTypes)
				if (str == ht)
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="HitType",
			       Type=typeof(string))]
		[XmlArray (ElementName="HitTypes")]
		public ArrayList HitTypes {
			get { return hitTypes; }
		}

		[XmlIgnore]
		public bool HasHitTypes {
			get { return hitTypes.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		// This API is DEPRECATED.
		// The source is now stored in the beagle:Source property.
		// To restrict on source, just do a normal property query.

		public void AddSource (string str)
		{
			searchSources.Add (str);

			if (source_part == null) {
				source_part = new QueryPart_Or ();
				AddPart (source_part);
			}

			// Add a part for this source type.
			QueryPart_Property part;
			part = new QueryPart_Property ();
			part.Type = PropertyType.Keyword;
			part.Key = "beagle:Source";
			part.Value = str;
			source_part.Add (part);
		}
		

		public bool AllowsSource (string str)
		{
			if (searchSources.Count == 0)
				return true;
			foreach (string ss in searchSources)
				if (str == ss)
					return true;
			return false;
		}

		[XmlArrayItem (ElementName="Source", Type=typeof (string))]
		[XmlArray (ElementName="Sources")]
		public ArrayList Sources {
			get { return searchSources; }
		}

		[XmlIgnore]
		public bool HasSources {
			get { return searchSources.Count > 0; }
		}

		///////////////////////////////////////////////////////////////

		public QueryDomain QueryDomain {
			get { return domain_flags; }
			set { domain_flags = value; }
		}

		private Dictionary<string, bool> hosts_added = new Dictionary<string, bool> ();

		public void AddDomain (Beagle.QueryDomain domain)
		{
			domain_flags |= domain;

			ArrayList network_services = null;

			if (domain == QueryDomain.Neighborhood) {
				network_services = Conf.Networking.NetworkServices;
			}

			if (network_services == null && domain <= Beagle.QueryDomain.System) {
				SetLocal (true);
				return;
			}

			foreach (NetworkService service in network_services) {
				if (hosts_added.ContainsKey (service.UriString))
					continue;

				Logger.Log.Debug ("Query: Adding '{0}' to query!", service.UriString);

				hosts_added [service.UriString] = true;
				SetRemote (service.UriString);
			}
		}

		public void AddRemoteDomain (Beagle.QueryDomain domain, string url)
		{
			if (domain <= Beagle.QueryDomain.System)
				throw new Exception ("I meant _remote_ domain!");

			domain_flags |= domain;

			if (! hosts_added.ContainsKey (url)) {
				hosts_added [url] = true;
				SetRemote (url);
			}
		}

		public void RemoveDomain (Beagle.QueryDomain domain)
		{
			domain_flags &= ~domain;

			if (domain <= Beagle.QueryDomain.System)
				SetLocal (false);
			else
				throw new NotImplementedException ("Removing already added remote domains not supported");
		}

		public bool AllowsDomain (Beagle.QueryDomain domain)
		{
			return (domain_flags & domain) != 0;
		}

		///////////////////////////////////////////////////////////////

		private int max_hits = 100;
		public int MaxHits {
			get { return max_hits; }
			set { max_hits = value; }
		}

		///////////////////////////////////////////////////////////////

		[XmlIgnore]
		public bool IsEmpty {
			get { return parts.Count == 0 && mimeTypes.Count == 0 && searchSources.Count == 0; }
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder ();

			foreach (QueryPart p in parts)
				sb.Append (p.ToString () + "\n");

			return sb.ToString ();
		}
	}
}
