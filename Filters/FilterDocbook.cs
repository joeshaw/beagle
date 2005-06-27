//
// FilterDocbook.cs
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
using System.Collections;

using Beagle.Util;
using Beagle.Daemon;

namespace Beagle.Filters 
{
	public class FilterDocbook : Filter 
	{
		protected XmlReader reader;

		protected string base_path;

		protected Stack indexables_stack = new Stack ();
		protected Stack contents_stack = new Stack ();
		protected Stack depth_stack = new Stack ();

		public FilterDocbook ()
		{
			SnippetMode = false;

			AddSupportedFlavor (FilterFlavor.NewFromMimeType ("application/docbook+xml"));
			AddSupportedFlavor (FilterFlavor.NewFromExtension (".docbook"));

			// FIXME: Uri/Extension mapping?
			AddSupportedFlavor (new FilterFlavor ("file:///usr/share/doc/*", ".xml", null, 0));
		}

		///////////////////////////////////////////////////

		override protected void DoOpen (FileInfo info)
		{
			base_path = info.FullName;
			reader = new XmlTextReader (Stream);
		}

		override protected void DoPullProperties ()
		{
			Stopwatch watch = new Stopwatch ();
			
			watch.Start ();

			while (reader.Read ()) {
				switch (reader.NodeType) {
				case XmlNodeType.Element:
					if (NodeLooksImportant (reader.Name)) {
						string id = reader.GetAttribute ("id");
						
						if (id != null && id != "")
							CreateIndexable (id, reader.Depth);
					}
					break;
					
				case XmlNodeType.Text:
					// Append text to the child indexable
					if (contents_stack.Count > 0)
						((StringBuilder) contents_stack.Peek ()).Append (reader.Value);
					// Append text to the main indexable
					AppendText (reader.Value);
					break;
					
				case XmlNodeType.EndElement:
					if (depth_stack.Count > 0 && ((int) depth_stack.Peek ()) == reader.Depth)
						ProcessIndexable ();
					break;
				}
			}

			watch.Stop ();
			
			// If we've successfully crawled the file but haven't 
			// found any indexables, we shouldn't consider it
			// successfull at all.
			if (ChildIndexables.Count == 0) {
				Error ();
				return;
			}

			Logger.Log.Debug ("Parsed docbook file in {0}", watch);

			Finished ();
		}

		///////////////////////////////////////////////////

		protected void CreateIndexable (string id, int depth)
		{
			Indexable indexable = new Indexable (UriFu.PathToFileUri (String.Format ("{0}#{1}", base_path, id)));
			indexable.Type = "DocBookEntry";
			indexable.MimeType = "text/plain";
			indexable.AddProperty (Property.NewKeyword ("fixme:id", id));

			indexables_stack.Push (indexable);
			contents_stack.Push (new StringBuilder ());
			depth_stack.Push (depth);
		}

		protected void ProcessIndexable () 
		{
			Indexable indexable = (Indexable) indexables_stack.Pop ();
			StringBuilder content = (StringBuilder) contents_stack.Pop ();
		
			depth_stack.Pop ();
			
			StringReader content_reader = new StringReader (content.ToString ());
			indexable.SetTextReader (content_reader);
			
			AddChildIndexable (indexable);
		}

		///////////////////////////////////////////////////

		protected bool NodeLooksImportant (string node_name) {
			return node_name.StartsWith ("sect") || 
				node_name.StartsWith ("chapter");
		}
	}
}
