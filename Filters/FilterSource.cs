//
// FilterText.cs
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
using System.IO;
namespace Beagle.Filters {

	public class FilterSource : Beagle.Daemon.Filter {

		public FilterSource ()
		{
			AddSupportedMimeType ("text/x-java");
			AddSupportedMimeType ("text/x-csrc");
			AddSupportedMimeType ("text/x-csharp");
		}

		StreamReader reader;

		override protected void DoOpen (FileInfo info)
		{
			reader = info.OpenText ();
		}
	
		protected void parseSourceFile (StreamReader reader)
		{
			string str;
			int index;
			string mimeType;
			string [] KeyWords ;

			string  [] cKeyWords  = {"auto", "break", "case", "char", "const", 
						"continue", "default", "do", "double", "else",
						"enum", "extern", "float", "for", "goto",
						"if", "int", "long", "register", "return", "short",
						"signed", "sizeof", "static", "strcut", "switch", "typedef",
						"union", "unsigned", "void", "volatile", "while" };

		        string [] javaKeyWords = {"abstract", "boolean", "break", "byte", "case", "catch",
						  "char", "class", "const", "continue", "default", "do",
						  "double", "else", "extends", "final", "finally", "float", 
						  "for", "goto", "if", "implements", "import", "instanceof", 
						  "int", "interface", "long", "native", "new", "package", 
						  "private", "protected", "public", "return", "short", "static", 
						  "strictfp", "super", "switch", "synchronized", "this", "throw",
						  "throws", "transient", "try", "void", "volatile", "while" };

			string [] csharpKeyWords = {"abstract",  "as",  "base", "bool",  "break", 
	                                            "byte",  "case",  "catch",  "char", "checked", 
						    "class", "const", "continue", "decimal", "default", 
						    "delegate",  "do",  "double",  "else",  "enum", 
						    "event", "explicit",  "extern", "false", "finally", 
					            "fixed", "float", "for", "foreach", "goto", 
						    "if", "implicit", "in",  "int", "interface", 
						    "internal",  "is",  "lock",  "long",  "namespace", 
						    "new",  "null",  "object", "operator", "out", 
						    "override", "params", "private", "protected", "public", 
						    "readonly", "ref", "return", "sbyte", "sealed", 
						    "short", "sizeof", "stackalloc", "static", "string", 
						    "struct", "switch", "this", "throw", "true", 
						    "try", "typeof", "uint", "ulong", "unchecked", 
						    "unsafe", "ushort", "using", "virtual", "void", 
						    "volatile", "while"}; 
		       KeyWords = csharpKeyWords;	
		       mimeType = Flavor.MimeType;
		       if (mimeType.Equals("text/x-java"))
			  KeyWords = javaKeyWords;
		       else if (mimeType.Equals("text/x-csrc"))
			  KeyWords = cKeyWords;
	               else if (mimeType.Equals("text/x-csharp"))
			  KeyWords = csharpKeyWords;
			
		       str = reader.ReadToEnd ();
			// FIXME this fails when keyword is part of an another word 
			for ( index = 0 ; index < KeyWords.Length; index++)
			{
				str = str.Replace (KeyWords[index], "");
			}
			AppendText (str);
                        AppendWhiteSpace ();
	
		
		}
		override protected void DoPull ()
		{
		  parseSourceFile (reader);
		  Finished ();
		}
	}
}
