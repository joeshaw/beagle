/*
Copyright (C) 2003 Simon Mourier <simonm@microsoft.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions
are met:
1. Redistributions of source code must retain the above copyright
   notice, this list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright
   notice, this list of conditions and the following disclaimer in the
   documentation and/or other materials provided with the distribution.
3. The name of the author may not be used to endorse or promote products
   derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Collections;

namespace HtmlAgilityPack.Samples
{
	class GetDocLinks
	{
		[STAThread]
		static void Main(string[] args)
		{
			HtmlWeb hw = new HtmlWeb();
			string url = @"http://www.microsoft.com";
			HtmlDocument doc = hw.Load(url);
			doc.Save("mshome.htm");

			DocumentWithLinks nwl = new DocumentWithLinks(doc);
			Console.WriteLine("Linked urls:");
			for(int i=0;i<nwl.Links.Count;i++)
			{
				Console.WriteLine(nwl.Links[i]);
			}

			Console.WriteLine("Referenced urls:");
			for(int i=0;i<nwl.References.Count;i++)
			{
				Console.WriteLine(nwl.References[i]);
			}
		}
	}

	/// <summary>
	/// Represents a document that needs linked files to be rendered, such as images or css files, and points to other HTML documents.
	/// </summary>
	public class DocumentWithLinks
	{
		private ArrayList _links;
		private ArrayList _references;
		private HtmlDocument _doc;

		/// <summary>
		/// Creates an instance of a DocumentWithLinkedFiles.
		/// </summary>
		/// <param name="doc">The input HTML document. May not be null.</param>
		public DocumentWithLinks(HtmlDocument doc)
		{
			if (doc == null)
			{
				throw new ArgumentNullException("doc");
			}
			_doc = doc;
			GetLinks();
			GetReferences();
		}

		private void GetLinks()
		{
			_links = new ArrayList();
			HtmlNodeCollection atts = _doc.DocumentNode.SelectNodes("//*[@background or @lowsrc or @src or @href]");
			if (atts == null)
				return;

			foreach(HtmlNode n in atts)
			{
				ParseLink(n, "background");
				ParseLink(n, "href");
				ParseLink(n, "src");
				ParseLink(n, "lowsrc");
			}
		}

		private void GetReferences()
		{
			_references = new ArrayList();
			HtmlNodeCollection hrefs = _doc.DocumentNode.SelectNodes("//a[@href]");
			if (hrefs == null)
				return;

			foreach(HtmlNode href in hrefs)
			{
				_references.Add(href.Attributes["href"].Value);
			}
		}


		private void ParseLink(HtmlNode node, string name)
		{
			HtmlAttribute att = node.Attributes[name];
			if (att == null)
				return;

			// if name = href, we are only interested by <link> tags
			if ((name == "href") && (node.Name != "link"))
				return;

			_links.Add(att.Value);
		}

		/// <summary>
		/// Gets a list of links as they are declared in the HTML document.
		/// </summary>
		public ArrayList Links
		{
			get
			{
				return _links;
			}
		}

		/// <summary>
		/// Gets a list of reference links to other HTML documents, as they are declared in the HTML document.
		/// </summary>
		public ArrayList References
		{
			get
			{
				return _references;
			}
		}
	}
}
