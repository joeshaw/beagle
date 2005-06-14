//
// TileNetwork.cs
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
using Beagle;

namespace Beagle.Tile {

	[HitFlavor (Name="Network", Rank=200, Emblem="emblem-google.png", 
		Color="#f5f5fe", Source="Network", Uri="netbeagle:*"),
	 HitFlavor (Name="Network", Rank=200, Emblem="emblem-google.png", 
		Color="#f5f5fe", Source="Network", Uri="net:*")]
	public class TileNetwork : TileFromHitTemplate {
	
		public TileNetwork (Hit _hit) : base (_hit, "template-network.html")
		{
		}

		public TileNetwork (Hit _hit, string template) : base (_hit, template)
		{
		}
		
		protected override void PopulateTemplate ()
		{
			base.PopulateTemplate ();

			Template["Icon"] = Images.GetHtmlSource ("netbeagle", "image/png");
	
			//netURI format ... netbeagle://154.132.45.23:8888/beagle?http://154.132.45.23:8888/beagle/public/testlt		
			string netUri = this.Hit.Uri.ToString();
			//Console.WriteLine("Hit Uri is " + netUri);
			
			string [] fragments = netUri.Split ('/');
			string hostNamePort = fragments[2];
			//Console.WriteLine("HostNamePort fragment is " + hostNamePort);
			
			int l = fragments.Length;
			string resourceName = fragments[l-1];
			if (resourceName != null && resourceName .Trim().Equals("")) 
				resourceName  = fragments[l-2];
			Template["FileName"] = resourceName;
			
			fragments = hostNamePort.Split(':');
						
			Template["HostName"] = fragments[0];
			
			string suffix = "";
			string resourceUrl = netUri;
		
			fragments = netUri.Split('?');
			if (fragments.Length > 1)
				resourceUrl = fragments[1];

			Template["ResourceUrl"] = resourceUrl;
			
			Template["NetworkedBeagleUrl"] = "http://" + hostNamePort + "/beagle/search.aspx";
			
			string snippet = null;
			if (Hit is NetworkHit) 
				snippet = ((NetworkHit)this.Hit).snippet;

			Template["Snippet"] = (snippet == null) ? "":snippet;								
		}
	}	
}
