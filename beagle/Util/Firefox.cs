//
// FileSystem.cs
//
// Copyright (C) 2007 Kevin Kubasik kevin@kubasik.net
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
using System.Xml;
using System.Xml.XPath;
using System.Collections.Generic;

namespace Beagle.Util
{
	public class DownloadedFile
	{
		private string local_uri;
		private string remote_uri;
		public DownloadedFile()
		{
			
		}
		public DownloadedFile(string loc, string remote)
		{
			local_uri=loc;
			remote_uri=remote;
		}
		
		public string Local
		{
			get { return local_uri; }
			set { local_uri = value;}
		}
		
		public string Remote
		{
			get { return remote_uri; }
			set { remote_uri = value;}
		}
	}
	
	public class Firefox 
	{
		const string RDF = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
		const string NC="http://home.netscape.com/NC-rdf#";
		List<DownloadedFile> templist; 
		string profile_dir;
		
		public Firefox(string profiledir)
		{
			templist = new System.Collections.Generic.List<DownloadedFile> ();
			profile_dir = profiledir;
		}
		
		public List<DownloadedFile> GetDownloads()
		{
			templist.Clear();
			XmlReader read = new XmlTextReader (File.OpenText (Path.Combine( profile_dir , "downloads.rdf" )));
		
			XmlDocument xpdoc = new XmlDocument();
			try{
				xpdoc.Load(read);
				XmlNamespaceManager  nsMgr = new XmlNamespaceManager(xpdoc.NameTable);
				nsMgr.AddNamespace ("RDF",RDF);
				nsMgr.AddNamespace ("NC" ,NC);

				XPathNavigator xnav = xpdoc.CreateNavigator ();
				
				XPathNodeIterator xnodeitr = xnav.Select ("//RDF:Description",nsMgr);

				xnodeitr.MoveNext();
				while(xnodeitr.MoveNext()){
					DownloadedFile temp = new DownloadedFile ();
					temp.Local =xnodeitr.Current.GetAttribute ("about",RDF);
					xnodeitr.Current.MoveToChild ("URL",NC);
					temp.Remote = xnodeitr.Current.GetAttribute ("resource",RDF);
					templist.Add (temp);
				}			
			}finally{
				read.Close();
			}
			return templist;
		}
//		public static void Main(string[] args){
//			Firefox f = new Firefox();
//			foreach(Beagle.Util.DownloadedFile df in f.GetDownloads()){
//				Console.WriteLine(df.Local + " -> " + df.Remote);
//			}
		//}
		
	}
}
