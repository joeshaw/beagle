/*
 * Copyright 2004 The Apache Software Foundation
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using Directory = Lucene.Net.Store.Directory;
using IndexInput = Lucene.Net.Store.IndexInput;
namespace Lucene.Net.Index
{
	
	/// <summary> Class responsible for access to stored document fields.
	/// 
	/// It uses &lt;segment&gt;.fdt and &lt;segment&gt;.fdx; files.
	/// 
	/// </summary>
	/// <version>  $Id$
	/// </version>
	sealed public class FieldsReader
	{
		private FieldInfos fieldInfos;
		private IndexInput fieldsStream;
		private IndexInput indexStream;
		private int size;
		
		public /*internal*/ FieldsReader(Directory d, System.String segment, FieldInfos fn)
		{
			fieldInfos = fn;
			
			fieldsStream = d.OpenInput(segment + ".fdt");
			indexStream = d.OpenInput(segment + ".fdx");
			
			size = (int) (indexStream.Length() / 8);
		}
		
		public /*internal*/ void  Close()
		{
			fieldsStream.Close();
			indexStream.Close();
		}
		
		public /*internal*/ int Size()
		{
			return size;
		}
		
		public /*internal*/ Document Doc(int n)
		{
			indexStream.Seek(n * 8L);
			long position = indexStream.ReadLong();
			fieldsStream.Seek(position);
			
			Document doc = new Document();
			int numFields = fieldsStream.ReadVInt();
			for (int i = 0; i < numFields; i++)
			{
				int fieldNumber = fieldsStream.ReadVInt();
				FieldInfo fi = fieldInfos.FieldInfo(fieldNumber);
				
				byte bits = fieldsStream.ReadByte();
				
                bool compressed = (bits & FieldsWriter.FIELD_IS_COMPRESSED) != 0;
                bool tokenize = (bits & FieldsWriter.FIELD_IS_TOKENIZED) != 0;
				
                if ((bits & FieldsWriter.FIELD_IS_BINARY) != 0)
                {
                    byte[] b = new byte[fieldsStream.ReadVInt()];
                    fieldsStream.ReadBytes(b, 0, b.Length);
                    if (compressed)
                        doc.Add(new Field(fi.name, Uncompress(b), Field.Store.COMPRESS));
                    else
                        doc.Add(new Field(fi.name, b, Field.Store.YES));
                }
                else
                {
                    Field.Index index;
                    Field.Store store = Field.Store.YES;
					
                    if (fi.isIndexed && tokenize)
                        index = Field.Index.TOKENIZED;
                    else if (fi.isIndexed && !tokenize)
                        index = Field.Index.UN_TOKENIZED;
                    else
                        index = Field.Index.NO;
					
                    if (compressed)
                    {
                        store = Field.Store.COMPRESS;
                        byte[] b = new byte[fieldsStream.ReadVInt()];
                        fieldsStream.ReadBytes(b, 0, b.Length);

	                    byte[] fromByteArray = Uncompress(b);
		                byte[] byteArray = null;

			            byteArray = new byte[fromByteArray.Length];
			            for (int ii = 0; ii < fromByteArray.Length; ii++)
				            byteArray[ii] = (byte) fromByteArray[ii];

                        doc.Add(new Field(fi.name, System.Text.Encoding.GetEncoding("UTF-8").GetString(byteArray), store, index, fi.storeTermVector ? Field.TermVector.YES : Field.TermVector.NO));
                    }
                    else
                        doc.Add(new Field(fi.name, fieldsStream.ReadString(), store, index, fi.storeTermVector?Field.TermVector.YES:Field.TermVector.NO));
                }
			}
			
			return doc;
		}
		
        private byte[] Uncompress(byte[] input)
        {
            // {{Aroush-1.9}} how can we do this in .NET?
            return input;
			
            /*
            //UPGRADE_ISSUE: Class 'java.util.zip.Inflater' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
            //UPGRADE_ISSUE: Constructor 'java.util.zip.Inflater.Inflater' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
            Inflater decompressor = new Inflater();
            //UPGRADE_ISSUE: Method 'java.util.zip.Inflater.setInput' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
            decompressor.setInput(input);
			
            // Create an expandable byte array to hold the decompressed data
            System.IO.MemoryStream bos = new System.IO.MemoryStream(input.Length);
			
            // Decompress the data
            byte[] buf = new byte[1024];
            //UPGRADE_ISSUE: Method 'java.util.zip.Inflater.finished' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
            while (!decompressor.finished())
            {
                try
                {
                    //UPGRADE_ISSUE: Method 'java.util.zip.Inflater.inflate' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
                    int count = decompressor.inflate(buf);
                    bos.Write(SupportClass.ToByteArray(buf), 0, count);
                }
                    //UPGRADE_ISSUE: Class 'java.util.zip.DataFormatException' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDataFormatException_3"'
                catch (DataFormatException e)
                {
                    // this will happen if the field is not compressed
                    throw new System.IO.IOException("field data are in wrong format: " + e.ToString());
                }
            }
			
            //UPGRADE_ISSUE: Method 'java.util.zip.Inflater.end' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipInflater_3"'
            decompressor.end();
			
            // Get the decompressed data
            return SupportClass.ToSByteArray(bos.ToArray());
            */
        }
    }
}