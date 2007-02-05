//
// PngHeader.cs
//
// Authors:
//     Larry Ewing <lewing@novell.com>
//
//
// Copyright (C) 2004 - 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using ICSharpCode.SharpZipLib.Zip.Compression;
using SemWeb;
using System;
using System.IO;
using System.Collections;
using System.Reflection;

#if ENABLE_NUNIT
using NUnit.Framework;
#endif

namespace Beagle.Util {
	public class PngHeader {
		System.Collections.ArrayList chunk_list;
		
		public PngHeader (System.IO.Stream stream)
		{
			Load (stream);
		}

		/**
		   Title 	Short (one line) title or caption for image 
		   Author 	Name of image's creator
		   Description 	Description of image (possibly long)
		   Copyright 	Copyright notice
		   Creation Time 	Time of original image creation
		   Software 	Software used to create the image
		   Disclaimer 	Legal disclaimer
		   Warning 	Warning of nature of content
		   Source 	Device used to create the image
		   Comment 	Miscellaneous comment
		   
		   xmp is XML:com.adobe.xmp

		   Other keywords may be defined for other purposes. Keywords of general interest can be registered with th
		*/
		/*
		public void Select (SemWeb.StatementSink sink)
		{
			foreach (Chunk c in Chunks) {
				if (c is IhdrChunk) {
					IhdrChunk ih = c as IhdrChunk;
					MetadataStore.AddLiteral (sink, "tiff:ImageWidth", ih.Width.ToString ());
					MetadataStore.AddLiteral (sink, "tiff:ImageLength", ih.Height.ToString ());
				} else if(c is TimeChunk) {
					TimeChunk tc = c as TimeChunk;

					MetadataStore.AddLiteral (sink, "xmp:ModifyDate", tc.Time.ToString ("yyyy-MM-ddThh:mm:ss"));
				} else if (c is TextChunk) {
					TextChunk text = c as TextChunk;

					switch (text.Keyword) {
					case "XMP":
					case "XML:com.adobe.xmp":
						System.IO.Stream xmpstream = new System.IO.MemoryStream (text.TextData);
						Xmp.XmpFile xmp = new Xmp.XmpFile (xmpstream);
						xmp.Select (sink);
						break;
					case "Comment":
						MetadataStore.AddLiteral (sink, "exif:UserComment", text.Text);
						break;
					case "Software":
						MetadataStore.AddLiteral (sink, "xmp:CreatorTool", text.Text);
						break;
					case "Title":
						MetadataStore.AddLiteral (sink, "dc:title", "rdf:Alt", new Literal (text.Text, "x-default", null));
						break;
					case "Author":
						MetadataStore.AddLiteral (sink, "dc:creator", "rdf:Seq", new Literal (text.Text));
						break;
					case "Copyright":
						MetadataStore.AddLiteral (sink, "dc:rights", "rdf:Alt", new Literal (text.Text, "x-default", null));
						break;
					case "Description":
						MetadataStore.AddLiteral (sink, "dc:description", "rdf:Alt", new Literal (text.Text, "x-default", null));
						break;
					case "Creation Time":
						try {
							System.DateTime time = System.DateTime.Parse (text.Text);
							MetadataStore.AddLiteral (sink, "xmp:CreateDate", time.ToString ("yyyy-MM-ddThh:mm:ss"));
						} catch (System.Exception e) {
							System.Console.WriteLine (e.ToString ());
						}
						break;
					}
				} else if (c is ColorChunk) {
					ColorChunk color = (ColorChunk)c;
					string [] whitepoint = new string [2];
					whitepoint [0] = color.WhiteX.ToString ();
					whitepoint [1] = color.WhiteY.ToString ();
					MetadataStore.Add (sink, "tiff:WhitePoint", "rdf:Seq", whitepoint);
					int i = 0;
					string [] rgb = new string [6];
					rgb [i++] = color.RedX.ToString ();
					rgb [i++] = color.RedY.ToString ();
					rgb [i++] = color.GreenX.ToString ();
					rgb [i++] = color.GreenY.ToString ();
					rgb [i++] = color.BlueX.ToString ();
					rgb [i++] = color.BlueY.ToString ();
					MetadataStore.Add (sink, "tiff:PrimaryChromaticities", "rdf:Seq", rgb);
				} else if (c.Name == "sRGB") {
					MetadataStore.AddLiteral (sink, "exif:ColorSpace", "1");
				} else if (c is PhysChunk) {
					PhysChunk phys = (PhysChunk)c;
					uint denominator = (uint) (phys.InMeters ? 100 : 1);
					
					MetadataStore.AddLiteral (sink, "tiff:ResolutionUnit", phys.InMeters ? "3" : "1");
					MetadataStore.AddLiteral (sink, "tiff:XResolution", new Tiff.Rational (phys.PixelsPerUnitX, denominator).ToString ());
					MetadataStore.AddLiteral (sink, "tiff:YResolution", new Tiff.Rational (phys.PixelsPerUnitY, denominator).ToString ());
				}
			}
		}
		*/

		public System.Collections.ArrayList Chunks {
			get { return chunk_list; }
		}

		public class ZtxtChunk : TextChunk {
			//public static string Name = "zTXt";

			protected bool compressed = true;
			public bool Compressed {
				get {
					return compressed;
				}
			}
			
			byte compression;
			public byte Compression {
			        get {
					return compression;
				}
				set {
					if (compression != 0)
						throw new System.Exception ("Unknown compression method");
				}
			}

			public ZtxtChunk (string keyword, string text) : base ()
			{
				Name = "zTXt";
				Compression = 0;
				this.keyword = keyword;
			}

			public ZtxtChunk (string name, byte [] data) : base (name, data)
			{
			}			

			protected ZtxtChunk ()
			{
			}

			public override void SetText (string text)
			{
				/* FIXME this is broken */
				text_data = encoding.GetBytes (text);
				data = Chunk.Deflate (text_data, 0, text_data.Length);
			}
			
			public override void Load (byte [] data) 
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				Compression = data [i++];

				text_data = Chunk.Inflate (data, i, data.Length - i);
			}
		}

		public class PhysChunk : Chunk {
			public PhysChunk (string name, byte [] data) : base (name, data) {}
			
			public uint PixelsPerUnitX {
				get {
					return BitConverter.ToUInt32 (data, 0, false);
				}
			}

			public uint PixelsPerUnitY {
				get {
					return BitConverter.ToUInt32 (data, 4, false);
				}
			}
			
			public bool InMeters {
				get {
					return data [8] == 0;
				}
			}
		}
		
		public class TextChunk : Chunk {
			//public static string Name = "tEXt";

			protected string keyword;
			protected byte [] text_data;
			protected System.Text.Encoding encoding = Latin1;

			public static System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding (28591);

			public TextChunk (string name, byte [] data) : base (name, data) 
			{
			}

			protected TextChunk ()
			{
			}


			public TextChunk (string keyword, string text)
			{
				this.Name = "tEXt";
				this.keyword = keyword;
				SetText (text);
			}

			public override void Load (byte [] data)
			{
				int i = 0;

				keyword = GetString (ref i);
				i++;
				int len = data.Length - i;
				text_data = new byte [len];
				System.Array.Copy (data, i, text_data, 0, len);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}

			public byte [] TextData {
				get {
					return text_data;
				}
			}
			
			public virtual void SetText (string text)
			{
				text_data = encoding.GetBytes (text);

				byte [] keyword_data = Latin1.GetBytes (keyword);
				data = new byte [keyword_data.Length + 1 + text_data.Length];
				System.Array.Copy (keyword_data, 0, data, 0, keyword_data.Length);
				data [keyword_data.Length] = 0;
				System.Array.Copy (text_data, 0, data, keyword_data.Length + 1, text_data.Length);
			}

			public string Text {
				get {
					return encoding.GetString (text_data, 0, text_data.Length);
				}
			}
		}
		
		public class IccpChunk : Chunk {
			string keyword;
			byte [] profile;

			public IccpChunk (string name, byte [] data) : base (name, data) {}
			
			public override void Load (byte [] data)
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				int compression = data [i++];
				if (compression != 0)
					throw new System.Exception ("Unknown Compression type");

				profile = Chunk.Inflate (data, i, data.Length - i);
			}

			public string Keyword {
				get {
					return keyword;
				}
			}
			
			public byte [] Profile {
				get {
					return profile;
				}
			}
		}

		public class ItxtChunk : ZtxtChunk{
			//public static string Name = "zTXt";

			string Language;
			string LocalizedKeyword;

			public override void Load (byte [] data)
			{
				int i = 0;
				keyword = GetString (ref i);
				i++;
				compressed = (data [i++] != 0);
				Compression = data [i++];
				Language = GetString (ref i);
				i++;
				LocalizedKeyword = GetString (ref i, System.Text.Encoding.UTF8);
				i++;

				if (Compressed) {
					text_data = Chunk.Inflate (data, i, data.Length - i);
				} else {
					int len = data.Length - i;
					text_data = new byte [len];
					System.Array.Copy (data, i, text_data, 0, len);
				}
			}

			public override void SetText (string text)
			{
				byte [] raw = System.Text.Encoding.UTF8.GetBytes (text);
				SetText (raw);
			}

			public void SetText (byte [] raw)
			{
				MemoryStream stream = new MemoryStream ();
				byte [] tmp;

				text_data = raw;

				tmp = Latin1.GetBytes (keyword);
				stream.Write (tmp, 0, tmp.Length);
				stream.WriteByte (0);

				stream.WriteByte ((byte)(compressed ? 1 : 0));
				stream.WriteByte (Compression);

				if (Language != null && Language != "") {
					tmp = Latin1.GetBytes (Language);
					stream.Write (tmp, 0, tmp.Length);
				}
				stream.WriteByte (0);

				if (LocalizedKeyword != null && LocalizedKeyword != "") {
					tmp = System.Text.Encoding.UTF8.GetBytes (LocalizedKeyword);
					stream.Write (tmp, 0, tmp.Length);
				}
				stream.WriteByte (0);
				
				if (compressed) {
					tmp = Deflate (text_data, 0, text_data.Length);
					stream.Write (tmp, 0, tmp.Length);
				} else {
					stream.Write (text_data, 0, text_data.Length);
				}
				this.data = stream.ToArray ();
			}

			public ItxtChunk (string name, byte [] data) : base (name, data) 
			{
				this.Name = name;
				encoding = System.Text.Encoding.UTF8;
			}

			public ItxtChunk (string keyword, string language, bool compressed) : base ()
			{
				encoding = System.Text.Encoding.UTF8;
				this.Name = "iTXt";
				this.keyword = keyword;
				this.Language = language;
				this.LocalizedKeyword = "";
				this.compressed = compressed;
				this.Compression = 0;
			}
		}

		public class TimeChunk : Chunk {
			//public static string Name = "tIME";

			System.DateTime time;

			public System.DateTime Time {
				get {
					return new System.DateTime (BitConverter.ToUInt16 (data, 0, false),
								    data [2], data [3], data [4], data [5], data [6]);

				}
				set {
					byte [] year = BitConverter.GetBytes ((ushort)value.Year, false);
					data [0] = year [0];
					data [1] = year [1];
					data [2] = (byte) value.Month;
					data [3] = (byte) value.Day;
					data [4] = (byte) value.Hour;
					data [6] = (byte) value.Minute;
					data [7] = (byte) value.Second;
				}
			}
			
			public TimeChunk (string name, byte [] data) : base (name, data) {}
			
			public TimeChunk ()
			{
				this.Name = "tIME";
				this.Time = System.DateTime.Now;
			}
		}

		public class StandardRgbChunk : Chunk {
			public StandardRgbChunk (string name, byte [] data) : base (name, data) {}
			
#if false
			public Cms.Intent RenderingIntent {
				get {
					return (Cms.Intent) data [0];
				}
			}
#endif
		}

		public class GammaChunk : Chunk {
			public GammaChunk (string name, byte [] data) : base (name, data) {}
			private const int divisor = 100000;

			public double Gamma {
				get {
					return BitConverter.ToUInt32 (data, 0, false) / (double) divisor;
				}
			}
		}
		
		public class ColorChunk : Chunk {
			// FIXME this should be represented like a tiff rational
			public const uint Denominator = 100000;

			public ColorChunk (string name, byte [] data) : base (name, data) {}
/*
			public Tiff.Rational WhiteX {
				get {
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 0, false), Denominator);
				}
			}
			public Tiff.Rational WhiteY {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 4, false), Denominator);
				}
			}
			public Tiff.Rational RedX {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 8, false), Denominator);
				}
			}
			public Tiff.Rational RedY {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 12, false), Denominator);
				}
			}
			public Tiff.Rational GreenX {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 16, false), Denominator);
				}
			}
			public Tiff.Rational GreenY {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 20, false), Denominator);
				}
			}
			public Tiff.Rational BlueX {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 24, false), Denominator);
				}
			}
			public Tiff.Rational BlueY {
				get { 
					return new Tiff.Rational (BitConverter.ToUInt32 (data, 28, false), Denominator);
				}
			}
			*/
		}

		public enum ColorType : byte {
			Gray = 0,
			Rgb = 2,
			Indexed = 3,
			GrayAlpha = 4,	
			RgbA = 6
		};
		
		public enum CompressionMethod : byte {
			Zlib = 0
		};
		
		public enum InterlaceMethod : byte {
			None = 0,
			Adam7 = 1
		};

		public enum FilterMethod : byte {
			Adaptive = 0
		}

		// Filter Types Show up as the first byte of each scanline
		public enum FilterType  {
			None = 0,
			Sub = 1,
			Up = 2,
			Average = 3,
			Paeth = 4
		};

		public class IhdrChunk : Chunk {
			public uint Width;
			public uint Height;
			public byte Depth;
			public ColorType Color;
			public PngHeader.CompressionMethod Compression;
			public FilterMethod Filter;
			public InterlaceMethod Interlace;

			public IhdrChunk (string name, byte [] data) : base (name, data) {}
			
			public override void Load (byte [] data)
			{
				Width = BitConverter.ToUInt32 (data, 0, false);
				Height = BitConverter.ToUInt32 (data, 4, false);
				Depth = data [8];
				Color = (ColorType) data [9];
				//if (Color != ColorType.Rgb)
				//	throw new System.Exception (System.String.Format ("unsupported {0}", Color));

				this.Compression = (CompressionMethod) data [10];
				if (this.Compression != CompressionMethod.Zlib)
					throw new System.Exception (System.String.Format ("unsupported {0}", Compression));

				Filter = (FilterMethod) data [11];
				if (Filter != FilterMethod.Adaptive)
					throw new System.Exception (System.String.Format ("unsupported {0}", Filter));
					
				Interlace = (InterlaceMethod) data [12];
				//if (Interlace != InterlaceMethod.None)
				//	throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));

			}

			public int ScanlineComponents {
				get {
					switch (Color) {
					case ColorType.Gray:
					case ColorType.Indexed:
						return 1;
					case ColorType.GrayAlpha:
						return 2;
					case ColorType.Rgb:
						return 3;
					case ColorType.RgbA:
						return 4;
					default:
						throw new System.Exception (System.String.Format ("Unknown format {0}", Color));
					}
				}
			}

			public uint GetScanlineLength (int pass)
			{
				uint length = 0;
				if (Interlace == InterlaceMethod.None) {
					int bits = ScanlineComponents * Depth;
					length = (uint) (this.Width * bits / 8);

					// and a byte for the FilterType
					length ++;
				} else {
					throw new System.Exception (System.String.Format ("unsupported {0}", Interlace));
				}

				return length;
			}
		}

		public class Crc {
			static uint [] lookup;
			uint value = 0xffffffff;
			uint length;
			System.IO.Stream stream;

			public uint Value {
				get { return (value ^ 0xffffffff); }
			}

			public uint Length {
				get { return length; }
			}

			static Crc () {
				lookup = new uint [265];
				uint c, n;
				int k;
				
				for (n = 0; n < 256; n++) {
					c = n;
					for (k = 0; k < 8; k++) {
						if ((c & 1) != 0)
							c = 0xedb88320 ^ (c >> 1);
						else
							c = c >> 1;
					}
					lookup [n] = c;
				}
			}

			public Crc ()
			{
			}

			public Crc (System.IO.Stream stream)
			{
				this.stream = stream;
			}
			
			public void Write (byte [] buffer)
			{
				Write (buffer, 0, buffer.Length);
			}

			public void Write (byte [] buffer, int offset, int len)
			{
				for (int i = offset; i < len; i++) 
					value = lookup [(value ^ buffer[i]) & 0xff] ^ (value >> 8); 

				length += (uint)len;

				if (stream != null)
					stream.Write (buffer, offset, len);
			}

			public void WriteSum ()
			{
				byte [] data = BitConverter.GetBytes (Value, false);
				stream.Write (data, 0, data.Length);
			}
		}

		public class Chunk {
			public string Name;
			protected byte [] data;
			protected static System.Collections.Hashtable name_table;

			public byte [] Data {
				get {
					return data;
				}
				set {
					Load (value);
				}
			}
			
			static Chunk () 
			{

				name_table = new System.Collections.Hashtable ();
				name_table ["iTXt"] = typeof (ItxtChunk);
				name_table ["tXMP"] = typeof (ItxtChunk);
				name_table ["tEXt"] = typeof (TextChunk);
				name_table ["zTXt"] = typeof (ZtxtChunk);
				name_table ["tIME"] = typeof (TimeChunk);
				name_table ["iCCP"] = typeof (IccpChunk);
				name_table ["IHDR"] = typeof (IhdrChunk);
				name_table ["cHRM"] = typeof (ColorChunk);
				name_table ["pHYs"] = typeof (PhysChunk);
				name_table ["gAMA"] = typeof (GammaChunk);
				name_table ["sRGB"] = typeof (StandardRgbChunk);
			}
			
			protected Chunk ()
			{
			}
			
			public Chunk (string name, byte [] data) 
			{
				this.Name = name;
				this.data = data;
				Load (data);
			}
			
			protected string GetString  (ref int i, System.Text.Encoding enc) 
			{
				for (; i < data.Length; i++) {
					if (data [i] == 0)
						break;
				}	
				
				return enc.GetString (data, 0, i);
			}

			protected string GetString  (ref int i) 
			{
				return GetString (ref i, TextChunk.Latin1);
			}

			public virtual void Load (byte [] data)
			{
				
			}
			
			public virtual void Save (System.IO.Stream stream)
			{
				byte [] name_bytes = System.Text.Encoding.ASCII.GetBytes (Name);
				byte [] length_bytes = BitConverter.GetBytes ((uint)data.Length, false);
				stream.Write (length_bytes, 0, length_bytes.Length);
				Crc crc = new Crc (stream);
				crc.Write (name_bytes);
				crc.Write (data);
				crc.WriteSum ();
			}

			public bool Critical {
				get {
					return !System.Char.IsLower (Name, 0);
				}
			}

			public bool Private {
				get {
					return System.Char.IsLower (Name, 1);
				}
			}
			
			public bool Reserved {
				get {
					return System.Char.IsLower (Name, 2);
				}
			}
			
			public bool Safe {
				get {
					return System.Char.IsLower (Name, 3);
				}
			}

			public bool CheckCrc (uint value)
			{
				byte [] name = System.Text.Encoding.ASCII.GetBytes (Name);
				Crc crc = new Crc ();
				crc.Write (name);
				crc.Write (data);

				return crc.Value == value;
			}

			public static Chunk Generate (string name, byte [] data)
			{
				System.Type t = (System.Type) name_table [name];

				Chunk chunk;
				if (t != null)
					chunk = (Chunk) System.Activator.CreateInstance (t, new object[] {name, data});
				else
				        chunk = new Chunk (name, data);

				return chunk;
			}

			public static byte [] Inflate (byte [] input, int start, int length)
			{
				System.IO.MemoryStream output = new System.IO.MemoryStream ();
				Inflater inflater = new Inflater ();
				
				inflater.SetInput (input, start, length);
				
				byte [] buf = new byte [1024];
				int inflate_length;
				while ((inflate_length = inflater.Inflate (buf)) > 0)
					output.Write (buf, 0, inflate_length);
				
				output.Close ();
				return output.ToArray ();
			}

			public static byte [] Deflate (byte [] input, int offset, int length)
			{
				System.IO.MemoryStream output = new System.IO.MemoryStream ();
				Deflater deflater = new Deflater ();
				deflater.SetInput (input, offset, length);
				
				byte [] buf = new byte [1024];
				int deflate_length;
				while ((deflate_length = deflater.Deflate (buf)) > 0)
					output.Write (buf, 0, deflate_length);

				output.Close ();
				return output.ToArray ();
			}
		}

		public class ChunkInflater {
			private Inflater inflater;
			private System.Collections.ArrayList chunks;

			public ChunkInflater ()
			{
				inflater = new Inflater ();
				chunks = new System.Collections.ArrayList ();
			}

			public bool Fill () 
			{
				while (inflater.IsNeedingInput && chunks.Count > 0) {
					inflater.SetInput (((Chunk)chunks[0]).Data);
					//System.Console.WriteLine ("adding chunk {0}", ((Chunk)chunks[0]).Data.Length);
					chunks.RemoveAt (0);
				}
				return true;
			}
			
			public int Inflate (byte [] data, int start, int length)
			{
				int result = 0;
				do {
					Fill ();
					result += inflater.Inflate (data, start + result, length - result);
					//System.Console.WriteLine ("Attempting Second after fill Inflate {0} {1} {2}", attempt, result, length - result);
				} while (result < length && chunks.Count > 0);
				
				return result;
			}
		       
			public void Add (Chunk chunk)
			{
				chunks.Add (chunk);
			}
		}

		private static byte [] magic = new byte [] { 137, 80, 78, 71, 13, 10, 26, 10 };

		void Load (Stream stream) 
		{
			byte [] heading = new byte [8];
			byte [] crc_data = new byte [4];
			stream.Read (heading, 0, heading.Length);
			
			for (int i = 0; i < heading.Length; i++)
			if (heading [i] != magic [i])
				throw new System.Exception ("Invalid PNG magic number");
			
			chunk_list = new System.Collections.ArrayList ();
			
			for (int i = 0; stream.Read (heading, 0, heading.Length) == heading.Length; i++) {
				uint length = BitConverter.ToUInt32 (heading, 0, false);
				string name = System.Text.Encoding.ASCII.GetString (heading, 4, 4);
				byte [] data = new byte [length];
				if (length > 0)
					stream.Read (data, 0, data.Length);
				
				stream.Read (crc_data, 0, 4);
				uint crc = BitConverter.ToUInt32 (crc_data, 0, false);
				
				Chunk chunk = Chunk.Generate (name, data);
				if (! chunk.CheckCrc (crc))
					throw new System.Exception ("chunk crc check failed");
				
				//System.Console.Write ("read one {0} {1}", chunk, chunk.Name);
				chunk_list.Add (chunk);
				
#if false		       
				if (chunk is TextChunk) {
					TextChunk text = (TextChunk) chunk;
					//System.Console.Write (" Text Chunk {0} {1}", 
					//		      text.Keyword, "", "");
				}
				
				TimeChunk time = chunk as TimeChunk;
				//if (time != null)
				//	System.Console.Write(" Time {0}", time.Time);

				//System.Console.WriteLine ("");
#endif
				
				if (chunk.Name == "IEND")
					break;
			}
		}

		internal string LookupText (string keyword)
		{
			TextChunk chunk = LookupTextChunk (keyword);
			if (chunk != null)
				return chunk.Text;

			return null;
		}
			
		internal TextChunk LookupTextChunk (string keyword)
		{
			foreach (Chunk chunk in Chunks) {
				TextChunk text = chunk as TextChunk;
				if (text != null && text.Keyword == keyword)
					return text;
			}
			return null;	
		}

		public string Description {
			get {
				string description = LookupText ("Description");

				if (description != null)
					return description;
				else
					return LookupText ("Comment");
			}
		}

		public Xmp.XmpFile GetXmp ()
		{
			TextChunk xmpchunk  = LookupTextChunk ("XML:com.adobe.xmp");
			if (xmpchunk == null)
				xmpchunk = LookupTextChunk ("XMP");

			if (xmpchunk == null)
				return null;
			
			using (MemoryStream stream = new MemoryStream (xmpchunk.TextData)) {
				return new Xmp.XmpFile (stream);
			}
		}

		public System.DateTime Date {
			get {
				// FIXME: we should first try parsing the
				// LookupText ("Creation Time") as a valid date

				foreach (Chunk chunk in Chunks) {
					TimeChunk time = chunk as TimeChunk;
					if (time != null)
						return time.Time.ToUniversalTime ();
				}
				return DateTime.UtcNow;
			}
		}
		
#if ENABLE_NUNIT
		[TestFixture]
		public class Tests {
			public Tests ()
			{
				Gnome.Vfs.Vfs.Initialize ();
				Gtk.Application.Init ();
			}

			[Test]
			public void Save ()
			{
				Gdk.Pixbuf test = new Gdk.Pixbuf (null, "f-spot-32.png");
				string path = ImageFile.TempPath ("joe.png");
				test.Save (path, "png");
				PngFile pimg = new PngFile (path);

				string desc = "this is a png test";
				string desc2 = "\000xa9 Novell Inc.";
				pimg.SetDescription (desc);
				using (Stream stream = File.OpenWrite (path)) {
					pimg.Save (stream);
				}
				PngFile mod = new PngFile (path);
				Assert.AreEqual (mod.Orientation, PixbufOrientation.TopLeft);
				Assert.AreEqual (mod.Description, desc);
				pimg.SetDescription (desc2);

				using (Stream stream = File.OpenWrite (path)) {
					pimg.Save (stream);
				}
				mod = new PngFile (path);
				Assert.AreEqual (mod.Description, desc2);
				
				File.Delete (path);
			}

			[Test]
			public void Load ()
			{
				string desc = "(c) 2004 Jakub Steiner\n\nCreated with The GIMP";
				Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly ();
				string path  = ImageFile.TempPath ("maddy.png");
				using (Stream output = File.OpenWrite (path)) {
					using (Stream source = assembly.GetManifestResourceStream ("f-spot-adjust-colors.png")) {
						byte [] buffer = new byte [256];
						while (source.Read (buffer, 0, buffer.Length) > 0) {
							output.Write (buffer, 0, buffer.Length);
						}
					}
				}
				PngFile pimg = new PngFile (path);
				Assert.AreEqual (pimg.Description, desc);

				File.Delete (path);
			}
		}
#endif

#if false
		public class ImageFile {
			string Path;
			public ImageFile (string path)
			{
				this.Path = path;
			}
		}

		public static void Main (string [] args) 
		{
			System.Collections.ArrayList failed = new System.Collections.ArrayList ();
			Gtk.Application.Init ();
			foreach (string path in args) {
				Gtk.Window win = new Gtk.Window (path);
				Gtk.HBox box = new Gtk.HBox ();
				box.Spacing = 12;
				win.Add (box);
				Gtk.Image image;
				image = new Gtk.Image ();

				System.DateTime start = System.DateTime.Now;
				System.TimeSpan one = start - start;
				System.TimeSpan two = start - start;
				try {
					start = System.DateTime.Now;
					image.Pixbuf = new Gdk.Pixbuf (path);
					one = System.DateTime.Now - start;
				}  catch (System.Exception e) {
				}
				box.PackStart (image);

				image = new Gtk.Image ();
				try {
					start = System.DateTime.Now;
					PngFile png = new PngFile (path);
					image.Pixbuf = png.GetPixbuf ();
					two = System.DateTime.Now - start;
				} catch (System.Exception e) {
					failed.Add (path);
					//System.Console.WriteLine ("Error loading {0}", path);
					//System.Console.WriteLine (e.ToString ());
				}

				//System.Console.WriteLine ("{2} Load Time {0} vs {1}", one.TotalMilliseconds, two.TotalMilliseconds, path); 
				box.PackStart (image);
				win.ShowAll ();
			}
			
			//System.Console.WriteLine ("{0} Failed to Load", failed.Count);
			//foreach (string fail_path in failed) {
			//	System.Console.WriteLine (fail_path);
			//}

			Gtk.Application.Run ();
		}
#endif
	}
}
