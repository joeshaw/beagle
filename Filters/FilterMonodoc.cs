//
// FilterMonodoc.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Text;
using System.Threading;

using Beagle.Daemon;
using Beagle.Util;

using ICSharpCode.SharpZipLib.Zip;

namespace Beagle.Filters {

	public class FilterMonodoc : Filter {

		ZipFile archive = null;

		public FilterMonodoc ()
		{
			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/monodoc"));
			// FIXME: Autoconf to find the monodoc prefix
			AddSupportedFlavor (new FilterFlavor ("file:///usr/lib/monodoc/sources/*", ".zip", null, 0));
		}

		override protected void DoOpen (FileInfo file)
		{
			try {
				archive = new ZipFile (file.FullName);
			} catch (Exception e) {
				Logger.Log.Error ("Error while filtering Monodoc archive: {0}", e);
				Error ();
			}
		}

		override protected void DoPullProperties ()
		{
			if (archive == null) {
				Error ();
				return;
			}

			foreach (ZipEntry entry in archive) {
				if (entry.Name.IndexOf (".") != -1)
					continue;
				
				XmlDocument document = new XmlDocument ();
				document.Load (archive.GetInputStream (entry));
				
				XmlNode type = document.SelectSingleNode ("/Type");
				
				if (type == null)
					continue;
				
				Indexable type_indexable = TypeNodeToIndexable (type, FileInfo);
				AddChildIndexable (type_indexable);
				
				foreach(XmlNode member in type.SelectNodes ("Members/Member")) {
					Indexable member_indexable = MemberNodeToIndexable (member,
											    FileInfo,
											    type.Attributes ["FullName"].Value);
					AddChildIndexable (member_indexable);
				}
			}
			
			// If we've successfully crawled the file but haven't 
                        // found any indexables, we shouldn't consider it
                        // successfull at all.
                        if (ChildIndexables.Count == 0) {
                                Error ();
                                return;
                        }
			
			Finished ();
		}
		
		static private Indexable TypeNodeToIndexable (XmlNode node, FileInfo file)
		{
			Indexable indexable = new Indexable (UriFu.PathToFileUri (file + "#T:" + node.Attributes ["FullName"].Value));
			
			indexable.MimeType = "text/html";
			indexable.Type = "MonodocEntry";
			
			indexable.AddProperty (Property.NewKeyword ("fixme:type", "type"));
			indexable.AddProperty (Property.NewKeyword ("fixme:name", "T:" + node.Attributes["FullName"].Value));
			
			StringReader reader = new StringReader (node.SelectSingleNode ("Docs").InnerXml); 
                        indexable.SetTextReader (reader);
			
			return indexable;
		}
		
		static private Indexable MemberNodeToIndexable(XmlNode node, FileInfo file, string parentName)
		{
			char memberType = MemberTypeToChar (node.SelectSingleNode ("MemberType").InnerText);
			StringBuilder memberFullName = new StringBuilder ();
			
			memberFullName.Append (memberType + ":"+ parentName);
			
			if (memberType != 'C')
				memberFullName.Append ("." + node.Attributes["MemberName"].Value);
			
			if (memberType == 'C' || memberType == 'M' || memberType == 'E') {
				memberFullName.Append ("(");
				bool inside = false;
				
				foreach (XmlNode parameter in node.SelectNodes ("Parameters/Parameter")) {
					if (!inside) inside = true; else memberFullName.Append(",");
					memberFullName.Append (parameter.Attributes["Type"].Value);
				}
				
				memberFullName.Append (")");
			}

			Indexable indexable = new Indexable (UriFu.PathToFileUri (file + "#" + memberFullName));

			indexable.MimeType = "text/html";
			indexable.Type = "MonodocEntry";

			indexable.AddProperty (Property.NewKeyword ("fixme:type", node.SelectSingleNode ("MemberType").InnerText.ToLower ()));
			indexable.AddProperty (Property.New ("fixme:name",memberFullName));

			StringReader reader = new StringReader (node.SelectSingleNode ("Docs").InnerXml); 
                        indexable.SetTextReader (reader);

			return indexable;		
		}

		static private char MemberTypeToChar (string memberType)
		{
			switch (memberType) {
			case "Constructor":
				return 'C';
			case "Event":
				return 'E';
			case "Property":
				return 'P';
			case "Field":
				return 'F';
			case "Method":
				return 'M';
			default:
				return 'U';
			}
		}
	}
}
