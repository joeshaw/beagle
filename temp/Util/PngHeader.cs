//
// EndianConverter.cs
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
						XmpFile xmp = new XmpFile (xmpstream);
						xmp.Select (sink);
						break;
					case "Comment":
						MetadataStore.AddLiteral (sink, "exif:UserComment", text.Text);
						break;
					case "Software":
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
					MetadataStore.AddLiteral (sink, "tiff:XResolution", new FSpot.Tiff.Rational (phys.PixelsPerUnitX, denominator).ToString ());
					MetadataStore.AddLiteral (sink, "tiff:YResolution", new FSpot.Tiff.Rational (phys.PixelsPerUnitY, denominator).ToString ());
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

			public ZtxtChunk (string name, byte [] data) : base (name, data) {}
			
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
					return EndianConverter.ToUInt32 (data, 0, false);
				}
			}

			public uint PixelsPerUnitY {
				get {
					return EndianConverter.ToUInt32 (data, 4, false);
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
			protected string text;
			protected byte [] text_data;
			protected System.Text.Encoding encoding = Latin1;

			public static System.Text.Encoding Latin1 = System.Text.Encoding.GetEncoding (28591);
			public TextChunk (string name, byte [] data) : base (name, data) {}

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

			public byte [] TextData 
			{
				get {
					return text_data;
				}
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

			public ItxtChunk (string name, byte [] data) : base (name, data) 
			{
				encoding = System.Text.Encoding.UTF8;
			}
		}

		public class TimeChunk : Chunk {
			//public static string Name = "tIME";

			System.DateTime time;

			public System.DateTime Time {
				get {
					return new System.DateTime (EndianConverter.ToUInt16 (data, 0, false),
								    data [2], data [3], data [4], data [5], data [6]);

				}
				set {
					byte [] year = EndianConverter.GetBytes ((ushort)value.Year, false);
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
		}

		public class StandardRgbChunk : Chunk {
			public StandardRgbChunk (string name, byte [] data) : base (name, data) {}
		}

		public class GammaChunk : Chunk {
			public GammaChunk (string name, byte [] data) : base (name, data) {}
			private const int divisor = 100000;

			public double Gamma {
				get {
					return EndianConverter.ToUInt32 (data, 0, false) / (double) divisor;
				}
			}
		}
		
		public class ColorChunk : Chunk {
			// FIXME this should be represented like a tiff rational
			public const uint Denominator = 100000;

			public ColorChunk (string name, byte [] data) : base (name, data) {}
			/*
			public FSpot.Tiff.Rational WhiteX {
				get {
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 0, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational WhiteY {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 4, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational RedX {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 8, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational RedY {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 12, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational GreenX {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 16, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational GreenY {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 20, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational BlueX {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 24, false), Denominator);
				}
			}
			public FSpot.Tiff.Rational BlueY {
				get { 
					return new FSpot.Tiff.Rational (FSpot.EndianConverter.ToUInt32 (data, 28, false), Denominator);
				}
			}
			*/
		}

		public enum ColorType : byte {
			Gray = 0,
			Rgb = 2,
			Indexed = 3,
			GrayAlpha = 4,	
			RgbAlpha = 6
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
				Width = EndianConverter.ToUInt32 (data, 0, false);
				Height = EndianConverter.ToUInt32 (data, 4, false);
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
					case ColorType.RgbAlpha:
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

			public bool CheckCrc (uint crc)
			{
				// FIXME implement me
				return true;
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
				while ((inflate_length = inflater.Inflate (buf)) > 0) {
					output.Write (buf, 0, inflate_length);
				}
				
				byte [] result = new byte [output.Length];
				output.Position = 0;
				output.Read (result, 0, result.Length);
				output.Close ();
				return result;
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

	        void Load (System.IO.Stream stream)
		{
			byte [] heading = new byte [8];
			stream.Read (heading, 0, heading.Length);

			if (heading [0] != 137 ||
			    heading [1] != 80 ||
			    heading [2] != 78 ||
			    heading [3] != 71 ||
			    heading [4] != 13 ||
			    heading [5] != 10 ||
			    heading [6] != 26 ||
			    heading [7] != 10)
			    throw new System.Exception ("Invalid PNG magic number");

			chunk_list = new System.Collections.ArrayList ();

			for (int i = 0; stream.Read (heading, 0, heading.Length) == heading.Length; i++) {
				uint length = EndianConverter.ToUInt32 (heading, 0, false);
				string name = System.Text.Encoding.ASCII.GetString (heading, 4, 4);
				byte [] data = new byte [length];
				if (length > 0)
					stream.Read (data, 0, data.Length);

				stream.Read (heading, 0, 4);
				uint crc = EndianConverter.ToUInt32 (heading, 0, false);

				Chunk chunk = Chunk.Generate (name, data);
				if (! chunk.CheckCrc (crc)) {
					if (chunk.Critical) {
						throw new System.Exception ("Chunk CRC check failed");
					} else {
						System.Console.WriteLine ("bad CRC in Chunk {0}... skipping", 
									  chunk.Name);
						continue;
					}
				} else {
					chunk_list.Add (chunk);
				}

				if (chunk.Name == "IEND")
					break;
			}
		}

		public string LookupText (string keyword)
		{
			TextChunk chunk = LookupTextChunk (keyword);
			if (chunk != null)
				return chunk.Text;

			return null;
		}

		public TextChunk LookupTextChunk (string keyword)
		{
			foreach (Chunk chunk in Chunks) {
				TextChunk text = chunk as TextChunk;
				if (text != null && text.Keyword == keyword)
					return text;
			}
			return null;	
		}
	}
}
