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

			private static string GetFieldDelimiter (string field)
			{
				if (field.Substring(0, 9) == "property:")
					return "=";

				return ":";
			}

			// This should be usable for both <query> and <category>
			private static string ParseXesamSourcesAndContents (XPathNavigator nav)
			{
				string ret = "", attr;
				bool has_source = false;

				attr = nav.GetAttribute ("source", String.Empty);
				if (!String.IsNullOrEmpty (attr) && (attr != "xesam:Source")) {
					string[] sources = attr.Split (',');

					has_source = true;
					ret += "( " + Ontologies.XesamToBeagleSource (sources[0]);

					for (int i = 1; i < sources.Length; i++)
						ret += " OR " + Ontologies.XesamToBeagleSource(sources[i].Trim ());

					ret += " )";
				}

				attr = nav.GetAttribute ("content", String.Empty);
				if (!String.IsNullOrEmpty (attr) && (attr != "xesam:Content")) {
					string[] contents = attr.Split (',');

					if (has_source)
						ret = "( " + ret + " AND ";
					ret += "( " + Ontologies.XesamToBeagleContent (contents[0]);

					for (int i = 1; i < contents.Length; i++)
						ret += " OR " + Ontologies.XesamToBeagleContent(contents[i].Trim ());

					ret += " )";
					if (has_source)
						ret += " )";
				}

				return ret;
			}

			private static string ParseXesamField (XPathNavigator nav)
			{
				string field = nav.GetAttribute ("name", String.Empty);
				// FIXME: Using just the first field is NOT correct
				field = Ontologies.XesamToBeagleField (field)[0];

				if (field.Contains (":")) {
					field = "property:" + field;
				}

				return field;
			}

			private static string ParseXesamData (XPathNavigator nav, ComparisonType dateComp)
			{
				string q = String.Empty;

				switch (nav.Name) {
				case "string":
					q += nav.Value;
					if (nav.GetAttribute ("phrase", String.Empty) != "false")
						q = '"' + q + '"';
					break;
				case "date":
					string date = nav.Value.Replace ("-", String.Empty);
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

			private static string ParseXesamCollectible (XPathNavigator nav, CollectibleType col)
			{
				// Got XPathNavigator:
				// 	iterate over selections  (equals, fullText, etc.)
				// 	recurse over collections (and, or)
				//
				// FIXME: Assuming only 1 field and 1 data element
				
				string q = String.Empty, field;
				
				while (true) {
					if (nav.GetAttribute ("negate", String.Empty) == "true")
						q += "-";

					switch (nav.Name) {
					case "and":
						nav.MoveToFirstChild ();
						q += "( ";
						q += ParseXesamCollectible (nav, CollectibleType.And);
						q += " )";
						nav.MoveToParent ();
						break;
					case "or":
						nav.MoveToFirstChild ();
						q += "( ";
						q += ParseXesamCollectible (nav, CollectibleType.Or);
						q += " )";
						nav.MoveToParent ();
						break;
					case "fullText":
						nav.MoveToFirstChild ();
						q += "(";
						q += ParseXesamData (nav, ComparisonType.None);
						q += ")";
						nav.MoveToParent ();
						break;
					case "inSet":
						nav.MoveToFirstChild ();
						field = ParseXesamField (nav);
						bool first = false;

						q += "( ";
						while (nav.MoveToNext ()) {
							if (!first)
								first = true;
							else
								q += " or ";

							q += field + GetFieldDelimiter(field) + ParseXesamData (nav, ComparisonType.Equals);
						}
						q += " )";

						nav.MoveToParent ();
						break;
					case "contains":
						goto case "equals";
					case "startsWith":
						goto case "equals";
					case "equals":
						nav.MoveToFirstChild ();
						field = ParseXesamField (nav);
						q += field;
						q += GetFieldDelimiter (field);
						nav.MoveToNext ();
						q += ParseXesamData (nav, ComparisonType.Equals);
						nav.MoveToParent ();
						break;
					case "greaterThanEquals":
						goto case "greaterThan";
					case "greaterThan":
						nav.MoveToFirstChild ();
						field = ParseXesamField (nav);
						q += field;
						q += GetFieldDelimiter (field);
						nav.MoveToNext ();
						q += ParseXesamData (nav, ComparisonType.Greater);
						nav.MoveToParent ();
						break;
					case "lessThanEquals":
						goto case "greaterThan";
					case "lessThan":
						nav.MoveToFirstChild ();
						field = ParseXesamField (nav);
						q += field;
						q += GetFieldDelimiter (field);
						nav.MoveToNext ();
						q += ParseXesamData (nav, ComparisonType.Greater);
						nav.MoveToParent ();
						break;
					default:
						Console.Error.WriteLine ("TBD: {0}", nav.Name);
						break;
					}

					if (nav.MoveToNext () && col != CollectibleType.None) {
						q += (col == CollectibleType.And ? " AND " : " OR ");
					} else {
						break;
					}
				}

				return q;
			}

			public static string ParseXesamQuery (string xmlQuery)
			{
				string ret = "";
				XmlTextReader tReader = new XmlTextReader (new System.IO.StringReader (xmlQuery));

				XmlReaderSettings settings = new XmlReaderSettings ();
				settings.IgnoreComments = true;
				settings.IgnoreWhitespace = true;
				//settings.Schemas.Add ("urn:xesam-query-schema", "xesam-query.xsd");
				//settings.ValidationType = ValidationType.Schema;

				XmlReader reader = XmlReader.Create (tReader, settings);

				// FIXME: This _does_ validation, right?
				XPathDocument xpDocument = new XPathDocument (reader);
				XPathNavigator nav = xpDocument.CreateNavigator ();

				nav.MoveToRoot ();
				nav.MoveToFirstChild ();
				nav.MoveToNext ();	// Move to <request>
				nav.MoveToFirstChild ();
				nav.MoveToNext ();	// Move to <query>

				while (nav.Name != "query" && nav.MoveToNext ()) { };

				if (nav.Name == "userQuery") {
					Console.Error.WriteLine ("*** User queries are not really supported! ***");
					return nav.Value;
				}
				
				if (nav.Name != "query") {
					Console.Error.WriteLine ("Didn't find a <query> (found {0})", nav.Name);
					return String.Empty;
				}

				string temp = ParseXesamSourcesAndContents (nav);
				if (!String.IsNullOrEmpty (temp)) {
					ret += temp + " AND ";
				}

				if (!nav.MoveToFirstChild () && !nav.MoveToNext ()) {
					Console.Error.WriteLine ("<query> element has no children");
					return String.Empty;
				}

				ret += ParseXesamCollectible (nav, CollectibleType.None);
				Console.Error.WriteLine ("Parsed Query: {0}", ret);

				return ret;
			}

		}
	}
}
