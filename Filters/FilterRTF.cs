//
// Beagle
//
// FilterRTF.cs : Trivial implementation of a RTF-document filter.
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
//
// Currently, the filtering is based on only few *control words*. If anyone
// has any samples that can break this "assumption", kindly post a copy of it, 
// if you can, to <vvaradhan@novell.com>
//
// FIXME:  Require more complex samples to test the parsing, mostly generated
//         using Microsoft Word or wordpad. :)

using System;
using System.Collections;
using System.IO;
using System.Text;
using Beagle.Util;
internal class RTFControlWordType {
	
	public enum Type {
		None,
		Skip,
		MetaDataBlock,
		MetaDataTag,
		Paragraph,
		EscSeq,
		CharProp
	}

	public Type Types;
	public string ctrlWord;
	
	RTFControlWordType (Type types, string ctrlword)
	{
		this.Types = types;
		this.ctrlWord = ctrlword;
	}

	// FIXME: Need to add "unicode", "styles", 
	// "header", "footer" etc.
	static RTFControlWordType[] types = 
	{
		new RTFControlWordType (Type.None, ""),
		new RTFControlWordType (Type.MetaDataBlock, "info"),
		new RTFControlWordType (Type.MetaDataTag, "title"),
		new RTFControlWordType (Type.MetaDataTag, "author"),
		new RTFControlWordType (Type.MetaDataTag, "comment"),
		new RTFControlWordType (Type.MetaDataTag, "operator"),
		new RTFControlWordType (Type.MetaDataTag, "nofpages"),
		new RTFControlWordType (Type.MetaDataTag, "nofwords"),
		new RTFControlWordType (Type.MetaDataTag, "generator"),
		new RTFControlWordType (Type.MetaDataTag, "company"),
		new RTFControlWordType (Type.Paragraph, "par"),
		new RTFControlWordType (Type.Paragraph, "pard"),
		new RTFControlWordType (Type.CharProp, "b"),
		new RTFControlWordType (Type.CharProp, "i"),
		new RTFControlWordType (Type.CharProp, "ul"),
		new RTFControlWordType (Type.CharProp, "up"),
		new RTFControlWordType (Type.CharProp, "dn"),
		new RTFControlWordType (Type.Skip, "'"),
		new RTFControlWordType (Type.Skip, "*"),
		new RTFControlWordType (Type.EscSeq, "{"),
		new RTFControlWordType (Type.EscSeq, "}"),
		new RTFControlWordType (Type.EscSeq, "\\"),
	};

	public static RTFControlWordType Find (string strCtrlWord)
	{
		for (int i = 0; i < types.Length; i++) {
			if (String.Compare (types[i].ctrlWord, strCtrlWord) == 0)
				return types[i];
		}
		return types[0];
	}
}
namespace Beagle.Filters {

	public class FilterRTF : Beagle.Daemon.Filter {
		
		public enum Position {
			None,
			InMetaData,
			InMetaDataTagGenerator,
			InBody
		}

		public enum ErrorCodes {
			ERROR_RTF_OK,
			ERROR_RTF_EOF,
			ERROR_RTF_UNHANDLED_SYMBOL
		};
		
		Position pos;
		Stack MetaDataStack;
		Stack TextDataStack;
		int groupCount;
		long offset;
		FileStream FsRTF;
		StreamReader SReaderRTF;

		public FilterRTF ()
		{
			// Make this a general rtf filter.
			AddSupportedMimeType ("application/rtf");
			pos = Position.None;
			MetaDataStack = new Stack();
			TextDataStack = new Stack();
			groupCount = 0;
			offset = 0;
			FsRTF = null;
			SReaderRTF = null;
		}

		override protected void DoOpen (FileInfo info) 
		{
			FsRTF = new FileStream (info.FullName, FileMode.Open, FileAccess.Read);
			if (FsRTF != null)
				SReaderRTF = new StreamReader (FsRTF);
			
		}

		// Identifies the type of RTF control word and handles accordingly
		private ErrorCodes HandleControlWord (string strCtrlWord, int paramVal, bool bMeta)
		{
			RTFControlWordType ctrlWrdType = RTFControlWordType.Find (strCtrlWord);
			
			switch (ctrlWrdType.Types) {
			case RTFControlWordType.Type.MetaDataBlock: /* process meta-data */
				pos = Position.InMetaData;
				break;
			case RTFControlWordType.Type.MetaDataTag:
				if (pos == Position.InMetaData) {
					if (String.Compare (strCtrlWord, "title") == 0)
						MetaDataStack.Push ("dc:title");
					else if (String.Compare (strCtrlWord, "author") == 0)
						MetaDataStack.Push ("dc:author");
					else if (String.Compare (strCtrlWord, "comment") == 0)
						MetaDataStack.Push ("fixme:comment");
					else if (String.Compare (strCtrlWord, "operator") == 0)
						MetaDataStack.Push ("fixme:operator");
					else if (String.Compare (strCtrlWord, "nofpages") == 0) {
						MetaDataStack.Push (Convert.ToString (paramVal));
						MetaDataStack.Push ("fixme:page-count");
					}
					else if (String.Compare (strCtrlWord, "nofwords") == 0) {
						MetaDataStack.Push (Convert.ToString (paramVal));
						MetaDataStack.Push ("fixme:word-count");
					}
					else if (String.Compare (strCtrlWord, "company") == 0)
						MetaDataStack.Push ("fixme:company");
				} else if (String.Compare (strCtrlWord, "generator") == 0) {
					pos = Position.InMetaDataTagGenerator;
					MetaDataStack.Push ("fixme:generator");
				}
				break;

			case RTFControlWordType.Type.Paragraph:
				if (!bMeta)
					pos = Position.InBody;
				break;

				// FIXME: "Hot" styles are not *properly reset to normal*
				// on some *wierd* conditions.
			case RTFControlWordType.Type.CharProp:
				if (pos == Position.InBody) {
					if (paramVal < 0)
						HotUp ();
				}
				break;

			case RTFControlWordType.Type.EscSeq:
				if (pos == Position.InBody) {
					TextDataStack.Push (strCtrlWord);
					TextDataStack.Push ("EscSeq");
				}
				break;
			}
			return ErrorCodes.ERROR_RTF_OK;
		}

		// FIXME: Probably need a little cleanup ;-)

		private ErrorCodes ProcessControlWords (bool bMeta)
		{
			int aByte = -1;
			char ch;
			int paramVal = -1, i;
			StringBuilder strCtrlWord = new StringBuilder ();
			StringBuilder strParameter = new StringBuilder ();
			
			aByte = SReaderRTF.Read ();
			if (aByte == -1)
				return ErrorCodes.ERROR_RTF_EOF;
			
			ch = (char) aByte;
			RTFControlWordType ctrlWrdType = RTFControlWordType.Find (new String (ch, 1));

			if (!Char.IsLetter (ch) && 
			    ctrlWrdType.Types != RTFControlWordType.Type.Skip &&
			    ctrlWrdType.Types != RTFControlWordType.Type.EscSeq) {
				Console.WriteLine ("Unhandled symbol: {0}, {1}", ch, ctrlWrdType.Types);
				return ErrorCodes.ERROR_RTF_UNHANDLED_SYMBOL;
			}
			while (aByte != -1) {
				strCtrlWord.Append (ch);
				aByte = SReaderRTF.Peek ();
				ch = (char) aByte; 
				if (Char.IsLetter (ch)) {
					aByte = SReaderRTF.Read ();
					ch = (char) aByte;
				}
				else
					break;
			}
			aByte = SReaderRTF.Peek ();
			ch = (char) aByte;
			if (Char.IsDigit (ch)) {
				aByte = SReaderRTF.Read ();
				ch = (char) aByte;
				while (aByte != -1) {
					strParameter.Append (ch);
					aByte = SReaderRTF.Peek ();
					ch = (char) aByte;
					if (Char.IsDigit (ch)) {
						aByte = SReaderRTF.Read ();
						ch = (char) aByte;
					}
					else
						break;
				}
				if (strParameter.Length > 0)
					paramVal = Convert.ToInt32 (strParameter.ToString());
			}
			//Console.WriteLine ("{0}\t{1}", strCtrlWord, strParameter);
			return (HandleControlWord (strCtrlWord.ToString(), paramVal, bMeta));
		}

		private ErrorCodes RTFParse (bool bMeta)
		{
			int aByte = -1;
			char ch;
			StringBuilder str = new StringBuilder ();
			string strTemp = null;
			ErrorCodes ec;

			// If we are not extracting meta-data, set the 
			// file pointer to the saved position
			if (!bMeta)
				SReaderRTF.BaseStream.Seek (offset, SeekOrigin.Begin);
		       
			while ((aByte = SReaderRTF.Read ()) != -1) {
				ch = (char) aByte;
				switch (ch) {
				case '\\': /* process keywords */
					ec = ProcessControlWords (bMeta); 
					if (ec != ErrorCodes.ERROR_RTF_OK)
						return ec;
					if (pos == Position.InBody) {
						AddTextForIndexing (str);
						//AppendText (str.ToString());
						//AppendWhiteSpace ();
					}
					str.Remove (0, str.Length);
					break;
				case '{': /* process groups */
					if (pos == Position.InBody)
					    AddTextForIndexing (str);
					str.Remove (0, str.Length);
					groupCount++;
					break;
				case '}': /* process groups */
					groupCount--;
					if (pos == Position.InMetaData ||
					    pos == Position.InMetaDataTagGenerator) {
						// groupCount will atleast be 1 for 
						// the outermost "{" block
						if (pos == Position.InMetaData && groupCount == 1) {
							if (bMeta) {
								offset = SReaderRTF.BaseStream.Position;
								return ErrorCodes.ERROR_RTF_OK;
							}

						} else {
							if (MetaDataStack.Count > 0) {
								strTemp = (string) MetaDataStack.Pop ();
								if ((String.Compare (strTemp, "fixme:word-count") == 0) ||
								    (String.Compare (strTemp, "fixme:page-count") == 0)) {
									str.Append ((string) MetaDataStack.Pop ());
									AddProperty (Beagle.Property.NewKeyword (strTemp,
														 str.ToString()));
								}
								else
									AddProperty (Beagle.Property.New (strTemp, 
													  str.ToString()));
							}
						}
						
					} else if (pos == Position.InBody) {
						if (str.Length > 0)
							str.Append (' ');
						AddTextForIndexing (str);
						if (IsHot)
							HotDown ();
					}

					break;
				case '\r': /* ignore \r */
				case '\n': /* ignore \n */
					break;
				default:
					str.Append (ch);
					break;
				}
			}
			return ErrorCodes.ERROR_RTF_OK;
		}

		private void AddTextForIndexing (StringBuilder str)
		{
			string strTemp;
			string strStyle;
			int elemCount;

			while (TextDataStack.Count > 0) {
				strTemp = (string) TextDataStack.Pop ();
				switch (strTemp) {
				case "EscSeq":
					strTemp = (string) TextDataStack.Pop ();
					str.Append (strTemp);
					break;
				}
			}
			if (str.Length > 0) {
				AppendText (str.ToString());
				str.Remove (0, str.Length);
			}
		}
			
		override protected void DoPull ()
		{
			ErrorCodes ec;
			ec = ErrorCodes.ERROR_RTF_OK;
			pos = Position.None;
			ec = RTFParse (false);
			if (ec != ErrorCodes.ERROR_RTF_OK)
				Logger.Log.Error ("{0}", ec);
			Finished ();
		}
		
		override protected void DoPullProperties ()
		{
			ErrorCodes ec;
			ec = ErrorCodes.ERROR_RTF_OK;
			ec = RTFParse (true);
			if (ec != ErrorCodes.ERROR_RTF_OK)
				Logger.Log.Error ("{0}", ec);
		}

	}
}
