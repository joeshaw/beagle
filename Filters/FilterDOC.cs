//
// FilterDOC.cs
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
using System.Collections;
using System.IO;
using System.Text;
using Gsf;

namespace Beagle.Filters {
    
	public class FilterDOC : FilterOle {

		int fib_ccpText;
		int fib_fcclx;
		Hashtable pieceItems;
		Hashtable pieceIndices;


		public FilterDOC () 
		{
			AddSupportedMimeType ("application/msword");
		}

		// Note:  This code to find the Real "FC" inside a "WordDocument"
		// stream is copied from wv2.  Word specification (!) says something about
		// the calculation, but not so clear.  Gotta crack it!!
	
		private void FindRealFC (ref int fc, ref bool unicode)
		{
			if ( (fc & 0x40000000) > 0 ) {
				fc = (int)( fc & 0xbfffffff ) >> 1;
				unicode = false;
			}
			else
				unicode = true;
		}
		public string getTableStreamName (Gsf.Input stream)
		{
			int fWhichTblStm;
			int length = 0;
			stream.SeekEmulate (0x0A);
			byte [] data = stream.Read (2);
			if (data == null) {
				Console.WriteLine ("Data is null");
				return "";
			}
		
			int fFlagSet = 0;

			for (int i = 1; i > -1; i--) {
				fFlagSet <<= 8;
				fFlagSet |= 0xff & data[i];
			}
		
			// find out the table stream correspoding to this document
			fWhichTblStm = fFlagSet & 0x0200;

			if (fWhichTblStm > 0)
				return ("1Table");
			else
				return ("0Table");
		}

		private void readPieceTable (Gsf.Input tblStream, 
					     int fib_fcclx)
		{
			int size = 0;
			tblStream.SeekEmulate (fib_fcclx);
			byte[] data = tblStream.Read (1);
			while (((clxtType)data[0]) == clxtType.clxtGrpprl) {
				data = tblStream.Read (2);
				size = GetInt16 (data, 0);
				tblStream.SeekEmulate (size);
			
				data = tblStream.Read (1);
			}

			if (((clxtType)data[0]) == clxtType.clxtPlcfpcd) {
				int cntPCDs = 0;
				data = tblStream.Read (4);
				size = GetInt32 (data, 0);
			
				pieceItems = new Hashtable ();
				pieceIndices = new Hashtable ();
			
				// 8 is the sizeof a PCD
				if (((size - 4) % (8 + 4)) > 0)
					cntPCDs = 0;
				else
					cntPCDs = (size - 4) / (8 + 4);
			
				// n+1 CP/FCs
				for (int i = 0; i < (cntPCDs+1); i++) {
					data = tblStream.Read (4);
					pieceIndices.Add (i, GetInt32 (data, 0));
				}

				// n PCDs
				for (int i = 0; i < cntPCDs; i++) {
					//Console.WriteLine ("Current tablestream pos : {0}", 
					//tblStream.Tell());
					//data = tblStream.Read (4);
					pieceItems.Add (i, new PCD (tblStream));
				}
			}
		
		}

		private StringBuilder StripOffWordSpecificChars (string strValue)
		{
			if (strValue == null)
				return null;

			StringBuilder strText = new StringBuilder ();

			for (int i = 0; i < strValue.Length; i++) {
				if (strValue[i] > 31 && strValue[i] < 126)
					strText.Append (strValue[i]);
				else {
					// FIXME:  We need to handle the characters appropriately,
					// such that we can extract the attributes of them as well.
					switch (Convert.ToInt32(strValue[i])) {
					case (int) PieceType.PageSectionBreaks:
					case (int) PieceType.ParagraphEnds:
					case (int) PieceType.HardLineBreaks: strText.Append (" ");
						break;
					
					case (int) PieceType.Tab: strText.Append (" ");
						break;
					}
				}
			}
			return strText;

		}

		public void ExtractText (Gsf.Input docStream, 
					   Gsf.Input tblStream)
		{
			int pcdTblIndex = 0;
			int chCntPCD = 0;
			StringBuilder strText = new StringBuilder ();
			string val = null;
			int fc;
			bool unicode;

			// get the ccpText defined in FIB
			docStream.SeekEmulate (0x004C);
			byte[] data = docStream.Read (4);
			fib_ccpText = GetInt32 (data, 0);

			// get the fcclx defined in FIB
			docStream.SeekEmulate (0x01A2);
			data = docStream.Read (4);
			fib_fcclx = GetInt32 (data, 0);

			// get the plcpcd list from the tableStream
			readPieceTable (tblStream, fib_fcclx);

			data = null;
			unicode = true;

			while (fib_ccpText > 0 && pcdTblIndex < pieceItems.Count) {
				chCntPCD = (int) pieceIndices[pcdTblIndex+1] - 
					(int) pieceIndices[pcdTblIndex];
				chCntPCD = chCntPCD > fib_ccpText ? fib_ccpText : chCntPCD;
			
				fc = ((PCD)pieceItems[pcdTblIndex]).GetFC();
			
				FindRealFC (ref fc, ref unicode);

				if (docStream.SeekEmulate (fc))
					Console.WriteLine ("Seek failed!!");

				//Console.WriteLine ("Number of chars present: {0}", chCntPCD);
				if (unicode) {
					data = docStream.Read (chCntPCD * 2); //unicode chars
					if (data != null)
						val = System.Text.Encoding.Unicode.GetString (data);
					else
						Console.WriteLine ("Unicode Data is Null");
				} else {
			
					data = docStream.Read (chCntPCD);
					if (data != null)
						val = System.Text.Encoding.ASCII.GetString (data);
					else
						Console.WriteLine ("ASCII data is NULL");
				}
				if (val != null) {
					strText = StripOffWordSpecificChars (val);
					AppendText (strText.ToString());
				}
				pcdTblIndex ++;
				data = null;
				val = null;
			}
			AppendWhiteSpace ();
		}

		// This enum is a "convenience enum" for reading the piece table
		private enum clxtType {
			clxtGrpprl = 1,
			clxtPlcfpcd = 2
		}
		override protected void DoPull ()
		{
			Input docStream = file.ChildByName ("WordDocument");
			Input tblStream = file.ChildByName (getTableStreamName (docStream));

			if (docStream != null)
				ExtractText (docStream, tblStream);
			Finished();
		}
		// FIXME: there are other specially treated "ASCII" characters
		// available to list here, but, we will content with these
		// and in general, we can replace ASCII 1 thru ASCII 31 with
		// simple spaces, however, word has this wierd logic where-in
		// if chp.fSpec = 1, some ASCII characters between ASCII 32 and 
		// ASCII 41 should be interpreted differently.

		public enum PieceType {
			Unknown = 0,
			Tab = 0x09,
			HardLineBreaks = 0x0B,
			PageSectionBreaks = 0x0C,
			ParagraphEnds = 0x0D,
			BreakingHyphens = 0x2D,
			NonRequiredHyphens = 0x1F,
			NonBreakingHyphens = 0x1E
		}

		public class PCD {
			/* this structure is of size 8 bytes and other fields do exist..
			   I am just having the one that I am interested in.. ;-)
			*/
			int _notInterested; // 16 bits
			int fc; // 32 bits
			int _actually_prm; // 16 bits

			public PCD (Gsf.Input stream)
			{
				read (stream);
			}
			private void read (Gsf.Input stream)
			{
				byte[] data = stream.Read (2);
				data = stream.Read (4);
				fc = GetInt32 (data, 0);
				//Console.WriteLine ("PCD FC : {0}", fc);
				data = stream.Read (2);
			}

			public int GetFC () { return fc;}
		}
	}
}
