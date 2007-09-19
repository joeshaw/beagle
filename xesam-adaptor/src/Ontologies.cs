//
// Ontologies.cs : Translate between Xesam and Beagle ontologies
//
// Copyright (C) 2007 Arun Raghavan <arunissatan@gmail.com>
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

namespace Beagle {
	namespace Xesam {
		class Ontologies {
			public static string ToBeagleField(string xesamField) {
				switch (xesamField) {
					case "dc:title":
						goto case "xesam:title";
					case "xesam:title":
						return "title";

					case "dc:author":
						goto case "xesam:author";
					case "xesam:author":
						return "author";

					case "dc:creator":
						goto case "xesam:creator";
					case "xesam:creator":
						return "creator";

					case "dc:date":
						return "date";

					case "mime":
						goto case "xesam:mimeType";
					case "xesam:mimeType":
						return "mimetype";

					case "url":
						goto case "xesam:url";
					case "uri":
						goto case "xesam:url";
					case "xesam:url":
						return "uri";

					default:
						Console.Error.WriteLine("Unsupported field: {0}", xesamField);
						return xesamField.Replace(':', '-');
				}
			}
		}
	}
}
