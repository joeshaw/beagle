//
// BugzillaDriver.cs
//
// Copyright (C) 2004 Novell, Inc.
//
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
using System.IO;
using System.Xml;
using System.Runtime.InteropServices;
using System.Net;
using System.Text.RegularExpressions;
using Beagle.Util;

namespace Beagle.Daemon {

	[QueryableFlavor (Name="Bugzilla", Domain=QueryDomain.Global, RequireInotify=false)]
	public class BugzillaDriver : IQueryable {

		private string bugzilla_host = "http://bugzilla.ximian.com";

		int maxResults = 5;


		public BugzillaDriver ()
		{
		}

		public string Name {
			get { return "Bugzilla"; }
		}

		public void Start () 
		{
		}

		public bool AcceptQuery (QueryBody body)
		{
			if (! body.HasText)
				return false;
			return true;
		}


		public void DoQuery (QueryBody body,
				     IQueryResult result,
				     IQueryableChangeData changeData)
		{
			Logger.Log.Debug ("Kicking off a bugzilla query");
			// FIXME - hard coding the url here
                        XmlDocument xml = GetBugzillaXml (body.QuotedText);
                        if (xml != null) {
				Hit hit = XmlBugToHit (xml, body.QuotedText);
				if (hit != null)
					result.Add (hit);
			}
		}

		public string GetSnippet (QueryBody body, Hit hit)
		{
			return null;
		}
		
		private XmlDocument GetBugzillaXml (string bug) {
                                                                                                                                                             
                        // Confirm that the text we have been passed looks
                        // like a bug ID (i.e. is numeric)
                        Regex bugregex = new Regex ("[0-9]+", RegexOptions.Compiled);
                        if (!bugregex.Match (bug).Success)
                        {
                                return null;
                        }
                                                                                                                                                             
                        XmlDocument xml = new XmlDocument ();
                                                                                                                                                             
                        HttpWebRequest req = (HttpWebRequest)
                                WebRequest.Create (String.Format ("{0}/xml.cgi?id={1}", bugzilla_host, bug));
                                                                                                                                                             
                        req.UserAgent = "Beagle";
                                                                                                                                                             
                        StreamReader input;
                        try {
                                WebResponse resp = req.GetResponse ();
                                input = new StreamReader (resp.GetResponseStream ());
                        } catch {
                                return null;
                        }
                                                                                                                                                             
                        string bugxml;
                                                                                                                                                             
                        try {
                                bugxml = input.ReadToEnd ();
                        } catch {
                                return null;
                        }
			Logger.Log.Debug (bugxml);
                                                                                                                                                             
                        int startidx = bugxml.IndexOf ("<bugzilla");
                        if (startidx < 0)
                                return null;
                                                                                                                                                             
                        bugxml = bugxml.Substring (startidx);
                                                                                                                                                             
                        try {
                                xml.LoadXml (bugxml);
                        } catch {
                                Logger.Log.Warn ("Bugzilla XML is not well-formed: {0}", bugxml);
                                return null;
                        }
                                                                                                                                                             
                        return xml;
                }

		private Hit XmlBugToHit (XmlDocument xml, string c)
                {
                        string bug_num, product, summary, owner, status;
                                                                                                                                                             
                        // see if the bug was even found. If there wasn't a bug, there will be
                        // an error attribute on the /bug element that says NotFound if one didn't exist.
                        if (!IsValidBug (xml))
                                return null;
                                                                                                                                                             
                        try {
                                bug_num = this.GetXmlText (xml, "//bug_id");
                                product = this.GetXmlText (xml, "//product");
                                summary = this.GetXmlText (xml, "//short_desc");
                                summary = summary.Substring (0, Math.Min (summary.Length, 50));
                                owner   = this.GetXmlText (xml, "//assigned_to");
                                status  = this.GetXmlText (xml, "//bug_status");
                        } catch {
                                Logger.Log.Warn ("Could not get bug fields");
                                return null;
                        }
                                                                                                                                                             
                        string bug_url = String.Format ("{0}/show_bug.cgi?id={1}", bugzilla_host, bug_num);
                                                                                                                                                             
			Hit hit = new Hit ();
                                                                                                                                                             
			hit.Uri = new Uri (bug_url, true);
                        hit.Type     = "Bugzilla";
                        hit.MimeType = "text/html"; // FIXME
                        hit.Source   = "Bugzilla";
			hit.ScoreRaw = 1.0;

                        hit ["Number"]  = bug_num;
                        hit ["Product"] = product;
                        hit ["Owner"]   = owner;
                        hit ["Summary"] = summary;
                        hit ["Status"]  = status;
                                                                                                                                                             
                        return hit;
                }
                                                                                                                                                             
                private string GetXmlText (XmlDocument xml, string tag)
                {
                        XmlNode node;
                                                                                                                                                             
                        node = xml.SelectSingleNode (tag);
                        if (node == null)
                                return "???";
                                                                                                                                                             
                        return node.InnerText;
                }	
		
		// Determines if the bug is valid or if we searched on some number
                // that we thought might have been a bugzilla number.
                // Rather than return all '???' let's just ignore them
                private bool IsValidBug (XmlDocument xml)
                {
                        try {
                                XmlNode node;
                                node = xml.SelectSingleNode ("/bugzilla/bug");
                                                                                                                                                             
                                // if we can't find the "bug" element, then it's not valid
                                if (node == null)
                                        return false;
                                                                                                                                                             
                                XmlNode attrib;
                                attrib = node.Attributes.GetNamedItem ("error");
                                                                                                                                                             
                                // if there's no error attribute, it's legit
                                // Note: I don't know what possible values for "error" are.
                                // I know if the bug isn't there, you will get 'NotFound' so I'm assuming that any
                                // error attribute is bad
                                                                                                                                                             
                                if (attrib == null)
                                        return true;
                        } catch {
                                // on error, assume it's not a bug:
                                return false;
                        }
                        return false;
                }

		public int GetItemCount ()
		{
			return -1; // # of items is undefined
		}
	}

}
