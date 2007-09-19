//
// Parser.cs : Parser for the Xesam Query Language
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
using System.Xml;
using System.Xml.XPath;
using System.Threading;
using System.Collections.Generic;
using Beagle;

namespace Beagle {
	namespace Xesam {
		class Parser {
			enum CollectibleType { None, And, Or };
			enum ComparisonType { None, Equals, Lesser, Greater };

			private static string ParseXesamField(XPathNavigator nav)
			{
				string field = nav.GetAttribute("name", "");
				switch (field) {
					case "dc:title":
						return "title";
					case "dc:author":
						return "author";
					case "dc:date":
						return "date";
					case "mime":
						return "mimetype";
					default:
						Console.Error.WriteLine("Unsupported field: {0}", field);
						return field.Replace(':', '-');
				}
			}

			private static string ParseXesamData(XPathNavigator nav, ComparisonType dateComp)
			{
				string q = "";

				switch (nav.Name) {
					case "string":
						q += nav.Value;
						if (nav.GetAttribute("phrase", "") != "false")
							q = '"' + q + '"';
						break;
					case "date":
						string date = nav.Value.Replace("-", "");
						if (dateComp == ComparisonType.Greater) {
							q += date + '-';
						} else if (dateComp == ComparisonType.Lesser) {
							q += '-' + date;
						} else {
							q += date;
						}
						break;
					default:
						q += nav.Value;
						break;
				}

				return q;
			}
			private static string ParseXesamCollectible(XPathNavigator nav, CollectibleType col)
			{
				// Got XPathNavigator:
				// 	iterate over selections  (equals, fullText, etc.)
				// 	recurse over collections (and, or)
				//
				// XXX: Assuming only 1 field and 1 data element
				
				string q = "";
				
				while (true) {
					if (nav.GetAttribute("negate", "") == "true")
						q += "-";

					switch (nav.Name) {
						case "and":
							nav.MoveToFirstChild();
							q += "( ";
							q += ParseXesamCollectible(nav, CollectibleType.And);
							q += " )";
							nav.MoveToParent();
							break;
						case "or":
							nav.MoveToFirstChild();
							q += "( ";
							q += ParseXesamCollectible(nav, CollectibleType.Or);
							q += " )";
							nav.MoveToParent();
							break;
						case "fullText":
							nav.MoveToFirstChild();
							q += "(";
							q += ParseXesamData(nav, ComparisonType.None);
							q += ")";
							nav.MoveToParent();
							break;
						case "inSet":
							Console.Error.WriteLine("TBD: {0}", nav.Name);
							break;
						case "contains":
							goto case "equals";
						case "startsWith":
							goto case "equals";
						case "equals":
							nav.MoveToFirstChild();
							q += ParseXesamField(nav);
							nav.MoveToNext();
							q += ':';
							q += ParseXesamData(nav, ComparisonType.Equals);
							nav.MoveToParent();
							break;
						case "greaterThanEquals":
							goto case "greaterThan";
						case "greaterThan":
							nav.MoveToFirstChild();
							q += ParseXesamField(nav);
							nav.MoveToNext();
							q += ':';
							q += ParseXesamData(nav, ComparisonType.Greater);
							nav.MoveToParent();
							break;
						case "lessThanEquals":
							goto case "greaterThan";
						case "lessThan":
							nav.MoveToFirstChild();
							q += ParseXesamField(nav);
							nav.MoveToNext();
							q += ':';
							q += ParseXesamData(nav, ComparisonType.Greater);
							nav.MoveToParent();
							break;
						default:
							Console.Error.WriteLine("TBD: {0}", nav.Name);
							break;
					}

					if (nav.MoveToNext() && col != CollectibleType.None) {
						q += (col == CollectibleType.And ? " AND " : " OR ");
					} else {
						break;
					}
				}

				return q;
			}

			public static string ParseXesamQuery(string xmlQuery)
			{
				XmlTextReader tReader = new XmlTextReader(new System.IO.StringReader(xmlQuery));

				XmlReaderSettings settings = new XmlReaderSettings();
				settings.IgnoreComments = true;
				settings.IgnoreWhitespace = true;
				//settings.Schemas.Add("urn:xesam-query-schema", "xesam-query.xsd");
				//settings.ValidationType = ValidationType.Schema;

				XmlReader reader = XmlReader.Create(tReader, settings);

				// XXX: This _does_ validation, right?
				XPathDocument xpDocument = new XPathDocument(reader);
				XPathNavigator nav = xpDocument.CreateNavigator();

				nav.MoveToRoot();
				nav.MoveToFirstChild();
				nav.MoveToNext();	// Move to <request>
				nav.MoveToFirstChild();
				nav.MoveToNext();	// Move to <query>

				while (nav.Name != "query" && nav.MoveToNext()) { };

				if (nav.Name == "userQuery") {
					Console.Error.WriteLine("*** User queries are not currently supported");
					return null;
				}
				
				if (nav.Name != "query") {
					Console.Error.WriteLine("Didn't find a <query> (found {0})", nav.Name);
					return null;
				}

				// XXX: Use query's type attribute

				if (!nav.MoveToFirstChild() && !nav.MoveToNext()) {
					Console.Error.WriteLine("<query> element has no children");
					return null;
				}

				string ret = ParseXesamCollectible(nav, CollectibleType.None);
				Console.Error.WriteLine("Parsed Query: {0}", ret);

				return ret;
			}

		}
	}
}
