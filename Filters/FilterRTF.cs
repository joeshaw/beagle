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
		ParaEnd,
		SplSection,
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
		new RTFControlWordType (Type.ParaEnd, "par"),
		new RTFControlWordType (Type.Paragraph, "pard"),
		new RTFControlWordType (Type.SplSection, "headerl"),
		new RTFControlWordType (Type.SplSection, "footerl"),
		new RTFControlWordType (Type.SplSection, "footnote"),
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
			InBody,
			InPara
		}

		public enum ErrorCodes {
			ERROR_RTF_OK,
			ERROR_RTF_EOF,
			ERROR_RTF_UNHANDLED_SYMBOL
		};
		
		Position pos;
		int groupCount;
		int skipCount;
		int hotStyleCount;
		bool bPartHotStyle;
		long offset;
		FileStream FsRTF;
		StreamReader SReaderRTF;
	        string partText;

		Stack MetaDataStack;
		Stack TextDataStack;

		public FilterRTF ()
		{
			// Make this a general rtf filter.
			AddSupportedMimeType ("application/rtf");
			pos = Position.None;
			groupCount = 0;
			skipCount = 0;
			hotStyleCount = 0;
			bPartHotStyle = false;
			offset = 0;
			FsRTF = null;
			SReaderRTF = null;
			partText = "";

			MetaDataStack = new Stack ();
			TextDataStack = new Stack ();

			SnippetMode = true;
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
					pos = Position.InPara;
				break;

			case RTFControlWordType.Type.ParaEnd:
				if (!bMeta)
					pos = Position.InBody;
				break;

				// FIXME: "Hot" styles are not *properly reset to normal*
				// on some *wierd* conditions.
				// To avoid such stuff, we need to maintain a stack of 
				// groupCounts for set/reset Hot styles.
			case RTFControlWordType.Type.SplSection:
				hotStyleCount = groupCount - 1;
				break;

			case RTFControlWordType.Type.CharProp:
				if (pos == Position.InPara) {
					if (paramVal < 0) {
						//Console.WriteLine ("HotUp: \\{0}{1}", strCtrlWord, paramVal);
						hotStyleCount = groupCount - 1;
						//HotUp ();
					}
				}
				break;

			case RTFControlWordType.Type.EscSeq:
				if (pos == Position.InPara) {
					TextDataStack.Push (strCtrlWord);
					TextDataStack.Push ("EscSeq");
				}
				break;
			case RTFControlWordType.Type.Skip:
				skipCount = groupCount - 1;
				//SkipDataStack.Push (groupCount-1);
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
			bool negParamVal = false;
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
			if (aByte != -1 && ch == '-') {
				negParamVal = true;
				aByte = SReaderRTF.Read (); // move the fp
				aByte = SReaderRTF.Peek ();
				ch = (char) aByte;
			}
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
			if (negParamVal && paramVal > -1)
				paramVal *= -1;
			return (HandleControlWord (strCtrlWord.ToString(), paramVal, bMeta));
		}

		private ErrorCodes RTFParse (bool bMeta)
		{
			int aByte = -1;
			char ch;
			StringBuilder str = new StringBuilder ();
			string strTemp = null;
			ErrorCodes ec;
			int OriginalGrpCount = 0x7FFFFFFF;

			// If we are not extracting meta-data, set the 
			// file pointer to the saved position
			if (!bMeta)
				SReaderRTF.BaseStream.Seek (offset, SeekOrigin.Begin);
		       
			while ((aByte = SReaderRTF.Read ()) != -1) {
				ch = (char) aByte;
				switch (ch) {
				case '\\': /* process keywords */
					if (skipCount > 0) {
						if (groupCount > skipCount)
							continue;
						else
							skipCount = 0;
					}
					ec = ProcessControlWords (bMeta); 
					if (ec != ErrorCodes.ERROR_RTF_OK)
						return ec;
					if (pos == Position.InPara)
						AddTextForIndexing (str);
					str.Remove (0, str.Length);
					break;
				case '{': /* process groups */
					if (pos == Position.InPara)
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
						
					} else if (pos == Position.InPara) {
						AddTextForIndexing (str);

					} else if (pos == Position.InBody) {
						//Console.WriteLine ("\\par : {0}", str);
						if (str.Length > 0)
							str.Append (' ');
						AddTextForIndexing (str);
					}
					if (hotStyleCount > 0
					    && groupCount <= hotStyleCount) {
						//Console.WriteLine ("Group count: {0}, stack: {1}", 
						//groupCount, hotStyleCount);
						HotDown ();
						hotStyleCount = 0;
					}
					
					break;
				case '\r': /* ignore \r */
				case '\n': /* ignore \n */
					break;
				default:
					if (skipCount == 0 || groupCount <= skipCount)
						str.Append (ch);
					break;
				}
			}
			if (partText.Length > 0) {
				if (bPartHotStyle && !IsHot) 
					HotUp ();
				AppendText (partText);
				if (IsHot)
					HotDown ();
			}
			return ErrorCodes.ERROR_RTF_OK;
		}

		private void AddTextForIndexing (StringBuilder str)
		{
			string strTemp;
			string paramStr = null;

			int elemCount;
			bool wasHot = false;

			while (TextDataStack.Count > 0) {
				strTemp = (string) TextDataStack.Pop ();
				switch (strTemp) {
				case "EscSeq":
					strTemp = (string) TextDataStack.Pop ();
					str.Append (strTemp);
					break;
				}
			}
			
			strTemp = "";
			if (str.Length > 0) {
				//Console.WriteLine ("Text: [{0}]", str);

				paramStr = str.ToString ();
				str.Remove (0, str.Length);

				int index = paramStr.LastIndexOf (' ');
				int sindex = 0;

				if (index > -1) {
					if (partText.Length > 0) {
						sindex = paramStr.IndexOf (' ');
						strTemp = partText + paramStr.Substring (0, sindex);
						//Console.WriteLine ("PartHotStyle: {0}, HotStyleCount: {1}, partText: {2}",
						//   bPartHotStyle,
						//	   hotStyleCount, strTemp);
						if (!IsHot) {
							if (bPartHotStyle)
								HotUp ();
						}
						else
							wasHot = true;

						AppendText (strTemp);
						if (!wasHot && bPartHotStyle)
							HotDown ();
						bPartHotStyle = false;
					}
					paramStr = paramStr.Substring (sindex);
					index = paramStr.LastIndexOf (' ');
					sindex = 0;
				}
				if (index > -1) {
					partText = paramStr.Substring (index);
					paramStr = paramStr.Substring (sindex, index);
				} else {
					strTemp = partText + paramStr;
					partText = strTemp;
					paramStr = "";
					strTemp = "";
				}
					
				// Enable *HOT* just before appending the text
				// because, there can be some *Partial Texts* without
				// *HOT* styles that needs to be appended.
				if (hotStyleCount > 0) {
					if (!IsHot)
						HotUp ();
					bPartHotStyle = true;
				} else 
					bPartHotStyle |= false;

				if (paramStr.Length > 0)
					AppendText (paramStr);

				if (partText.Length < 1)
					bPartHotStyle = false;
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
