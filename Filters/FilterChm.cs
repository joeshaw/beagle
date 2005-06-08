//
// FilterChm.cs : Trivial implementation of a CHM filter.
//
// Author :
//      Miguel Cabrera <mfcabrer@unalmed.edu.co>
//
// Copyright (C) 2005 Miguel Cabrera
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
using System.Text;
using HtmlAgilityPack;
using Beagle.Util;


namespace Beagle.Filters {

	public class FilterChm : FilterHtml {
		
		ChmFile chmFile;
		
		public FilterChm ()
		{
			RegisterSupportedTypes();
			SnippetMode= true;
			
		}
		

		new protected  void WalkHeadNodes (HtmlNode node)
		{
			foreach (HtmlNode subnode in node.ChildNodes) {
				if (subnode.NodeType == HtmlNodeType.Element
				    && subnode.Name == "title") {
					String title = WalkChildNodesForText (subnode);
					title = HtmlEntity.DeEntitize (title);
					//AddProperty (Beagle.Property.New ("dc:title", title));
					AppendText (title);
				}
				if (subnode.NodeType == HtmlNodeType.Element
				    && subnode.Name == "meta") {
	   				string name = subnode.GetAttributeValue ("name", "");
           				string content = subnode.GetAttributeValue ("content", "");
					if (name != "" && content != "")
						AddProperty (Beagle.Property.New (name, content));
				}
			}
		}
		

		public void WalkTocFile(HtmlNode node) 
		{
			
			
			
			foreach (HtmlNode subnode in node.ChildNodes) {
				if (subnode.NodeType == HtmlNodeType.Element) {
					switch (subnode.Name) {
					case "html":
				case "head":
					WalkTocFile (subnode);
					break;
				case "body":
					default:
						WalkToc (subnode);
						break;
					}
				}
			}
			
		}

		
		
		public void WalkToc(HtmlNode node)
		{
			
			switch (node.NodeType) {
				
			case HtmlNodeType.Document:
			case HtmlNodeType.Element:
				
				if(node.Name == "li")
					foreach(HtmlNode subnode in node.ChildNodes)
						HandleTocEntry(subnode);
				
				foreach(HtmlNode subnode in node.ChildNodes)
					WalkToc(subnode);
				break;
				
				
				
			}
			
		}	
		
		
		public void HandleTocEntry(HtmlNode node)
		{
			
			if(node.Name == "object") {
				
			string attr = node.GetAttributeValue ("type", "");
			
			if(String.Compare(attr,"text/sitemap",true) == 0) 
				foreach(HtmlNode subnode in node.ChildNodes) 
					if(String.Compare(subnode.Name,"param",true) == 0 &&
					   subnode.GetAttributeValue("name","") == "Name" ){
						HotUp();
						AppendText(subnode.GetAttributeValue("value",""));
						HotDown();
						
					}
			
			
			
			}
			
		}		
		

		void ReadHtml(TextReader reader) 
		{

			HtmlDocument doc = new HtmlDocument ();

			try {
				doc.Load (reader);
			} catch (ArgumentNullException e) {
				/*Weird should not happend*/
				//¿What should do here?
				Logger.Log.Warn (e.Message);
				return;
				
			}

			if (doc != null)
				WalkNodes (doc.DocumentNode);

						
			
		}
		
		override protected void DoOpen (FileInfo info) 
		{

			chmFile = new ChmFile();

			try {
				
				chmFile.Load(info.FullName);
				
			}
			catch (Exception e) {
				
				Logger.Log.Warn ("Could not parse {0}: {1}",info.Name,e.Message);
				Finished ();
				return;

			}
			
			
			

		}

		

		override protected void DoPullProperties() 
		{
						
			if(chmFile.Title != "") 
				AddProperty (Beagle.Property.New ("dc:title", chmFile.Title));
			
					
		
		}

		override protected void DoPull()
		{
			//Logger.Log.Debug("FilterCHM: Parsing:" + chmFile.Title);
			//chmFile.ParseContents(ReadHtml);
			

			/*
			  We only read the default file and the topic file
			**/
			ReadHtml(chmFile.GetDefaultFile());
			
			HtmlDocument doc = new HtmlDocument();

			doc.Load(chmFile.GetTopicsFile());
			
			WalkTocFile(doc.DocumentNode);
			
			Finished();
			
			
		}

		override protected void  DoClose() 
		{
			chmFile.Dispose();
		
		}
		
		override protected  void  RegisterSupportedTypes()
		{

			AddSupportedMimeType("application/x-chm");
		}

	}
}
