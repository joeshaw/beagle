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
using IndexOutput = Lucene.Net.Store.IndexOutput;
namespace Lucene.Net.Index
{
	
	sealed class FieldsWriter
	{
        internal const byte FIELD_IS_TOKENIZED = (byte) (0x1);
        internal const byte FIELD_IS_BINARY = (byte) (0x2);
        internal const byte FIELD_IS_COMPRESSED = (byte) (0x4);

        private FieldInfos fieldInfos;
		
        private IndexOutput fieldsStream;
		
        private IndexOutput indexStream;
		
		internal FieldsWriter(Directory d, System.String segment, FieldInfos fn)
		{
			fieldInfos = fn;
			fieldsStream = d.CreateOutput(segment + ".fdt");
			indexStream = d.CreateOutput(segment + ".fdx");
		}
		
		internal void  Close()
		{
			fieldsStream.Close();
			indexStream.Close();
		}
		
		internal void  AddDocument(Document doc)
		{
			indexStream.WriteLong(fieldsStream.GetFilePointer());
			
			int storedCount = 0;
            foreach (Field field  in doc.Fields())
            {
				if (field.IsStored())
					storedCount++;
			}
			fieldsStream.WriteVInt(storedCount);
			
            foreach (Field field in doc.Fields())
            {
				if (field.IsStored())
				{
					fieldsStream.WriteVInt(fieldInfos.FieldNumber(field.Name()));
					
					byte bits = 0;
					if (field.IsTokenized())
						bits |= FieldsWriter.FIELD_IS_TOKENIZED;
                    if (field.IsBinary())
                        bits |= FieldsWriter.FIELD_IS_BINARY;
                    if (field.IsCompressed())
                        bits |= FieldsWriter.FIELD_IS_COMPRESSED;
					
                    fieldsStream.WriteByte(bits);
					
                    if (field.IsCompressed())
                    {
                        // compression is enabled for the current field
                        byte[] data = null;
                        // check if it is a binary field
                        if (field.IsBinary())
                        {
                            data = Compress(field.BinaryValue());
                        }
                        else
                        {
	                        byte[] encodingByteArray = System.Text.Encoding.GetEncoding("UTF-8").GetBytes(field.StringValue());
		                    byte[] byteArray = new byte[encodingByteArray.Length];
			                for (int ii = 0; ii < encodingByteArray.Length; ii++)
				                byteArray[ii] = (byte) encodingByteArray[ii];

                            data = Compress(byteArray);
                        }
                        int len = data.Length;
                        fieldsStream.WriteVInt(len);
                        fieldsStream.WriteBytes(data, len);
                    }
                    else
                    {
                        // compression is disabled for the current field
                        if (field.IsBinary())
                        {
                            byte[] data = field.BinaryValue();
                            int len = data.Length;
                            fieldsStream.WriteVInt(len);
                            fieldsStream.WriteBytes(data, len);
                        }
                        else
                        {
                            fieldsStream.WriteString(field.StringValue());
                        }
                    }
				}
			}
		}
		
        private byte[] Compress(byte[] input)
        {
            // {{Aroush-1.9}} how can we do this in .NET
            return input;

            /*
            // Create the compressor with highest level of compression
            //UPGRADE_ISSUE: Class 'java.util.zip.Deflater' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            //UPGRADE_ISSUE: Constructor 'java.util.zip.Deflater.Deflater' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            Deflater compressor = new Deflater();
            //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.setLevel' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            //UPGRADE_ISSUE: Field 'java.util.zip.Deflater.BEST_COMPRESSION' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            compressor.setLevel(Deflater.BEST_COMPRESSION);
			
            // Give the compressor the data to compress
            //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.setInput' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            compressor.setInput(input);
            //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.finish' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            compressor.finish();
			
            / *
            * Create an expandable byte array to hold the compressed data.
            * You cannot use an array that's the same size as the orginal because
            * there is no guarantee that the compressed data will be smaller than
            * the uncompressed data.
            * /
            System.IO.MemoryStream bos = new System.IO.MemoryStream(input.Length);
			
            // Compress the data
            byte[] buf = new byte[1024];
            //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.finished' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            while (!compressor.finished())
            {
                //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.deflate' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
                int count = compressor.deflate(buf);
                bos.Write(SupportClass.ToByteArray(buf), 0, count);
            }
			
            //UPGRADE_ISSUE: Method 'java.util.zip.Deflater.end' was not converted. 'ms-help://MS.VSCC.2003/commoner/redir/redirect.htm?keyword="jlca1000_javautilzipDeflater_3"'
            compressor.end();
			
            // Get the compressed data
            return SupportClass.ToSByteArray(bos.ToArray());
            */
        }
    }
}