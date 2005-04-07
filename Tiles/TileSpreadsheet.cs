//
// TileSpreadsheet.cs
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
using System.IO;
using System.Text;

using Mono.Posix;

using BU = Beagle.Util;

namespace Beagle.Tile {
	
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/vnd.sun.xml.calc")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType = "application/vnd.sun.xml.calc.template")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/excel")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/vnd.ms-excel")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/x-excel")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/x-msexcel")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/x-gnumeric")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="text/spreadsheet")]
		
	// OO 2.0 (writer) formats
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType="application/vnd.oasis.opendocument.spreadsheet")]
	[HitFlavor (Name="Docs", Rank=400, Emblem="", Color="#f5f5fe",
		    Type="File", MimeType = "application/vnd.oasis.opendocument.spreadsheet.template")]
		
		public class TileSpreadsheet : TileFile {
			public TileSpreadsheet (Hit _hit) : base (_hit, "template-spreadsheet.html")
			{
			}
			
			protected override void PopulateTemplate ()
			{
				StringBuilder strPagesAndWords = new StringBuilder ();

				base.PopulateTemplate ();
				if (Hit ["dc:title"] != null && Hit ["dc:title"].Trim () != "")
					Template ["Title"] = Hit ["dc:title"];
			}
		}
}
