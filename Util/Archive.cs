/* -*- Mode: csharp; tab-width: 8; indent-tabs-mode: t; c-basic-offset: 8 -*- */
//
// Archive.cs
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
using ICSharpCode.SharpZipLib;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Tar;

namespace Beagle.Util {
	
	public class Archive : Stream
	{
		Stream baseStream;
		string uri;
		string method;
		delegate string GetNextEntryType ();
		GetNextEntryType getNextEntry;
		bool first = true;

		public Archive (string filename, string mimeType) {
			this.uri = "file://" + filename;
			baseStream = new FileStream (filename,
						     FileMode.Open);
			switch (mimeType) {
			case "application/zip":
				baseStream = new ZipInputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntryZip);
				method = "#zip:";
				break;
			case "application/x-bzip-compressed-tar":
				baseStream = new BZip2InputStream (baseStream);
				baseStream = new TarInputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntryTar);
				method = "#bzip2:#tar:";
				break;
			case "application/x-compressed-tar":
				baseStream = new GZipInputStream (baseStream);
				baseStream = new TarInputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntryTar);
				method = "#gzip:#tar:";
				break;
			case "application/x-tar":
				baseStream = new TarInputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntryTar);
				method = "#tar:";
				break;
			case "application/x-gzip":
				baseStream = new GZipInputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntrySingle);
				method = "#gzip:";
				break;
			case "application/x-bzip":
				baseStream = new BZip2InputStream (baseStream);
				getNextEntry = new GetNextEntryType (GetNextEntrySingle);
				method = "#bzip:";
				break;
			default:
				throw new ArgumentException ("Invalid or unsupported mime type.");
			}
		}

		/* Returns the URI of the next string. */
		public string GetNextEntry () {
			return getNextEntry();
		}

		public string GetNextEntryZip () {
			ZipInputStream inputStream = baseStream as ZipInputStream;
			ZipEntry entry = inputStream.GetNextEntry();
			if (entry != null)
				return uri + method + entry.Name;
			else
				return null;
		}

		public string GetNextEntrySingle () {
			if (first) {
				first = false;
				return uri + method;
			} else
				return null;
		}

		public string GetNextEntryTar () {
			TarInputStream inputStream = baseStream as TarInputStream;
			TarEntry entry = inputStream.GetNextEntry();
			if (entry != null)
				return uri + method + entry.Name;
			else
				return null;
		}

		public override int Read (byte[] buffer, int offset, int length) {
			return baseStream.Read (buffer, offset, length);
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int length,
							AsyncCallback cback, object state)
		{
			return baseStream.BeginRead (buffer, offset, length, cback, state);
		}

		public override int EndRead(IAsyncResult async_result) {
			return baseStream.EndRead (async_result);
		}

		public override void Write (byte[] buffer, int offset, int length) {
			throw new NotSupportedException ();
		}
		public override void Flush () {
			throw new NotSupportedException ();
		}
		public override long Seek (long offset, SeekOrigin origin) {
			throw new NotSupportedException ();
		}
		public override void SetLength (long value) {
			throw new System.NotSupportedException();
		}
		public override bool CanRead {
			get {
				return baseStream.CanRead;
			}
		}
		public override bool CanSeek {
			get {
				return false;
			}
		}
		public override bool CanWrite {
			get {
				return false;
			}
		}
		public override long Length {
			get {
				throw new System.NotSupportedException();
			}
			set {
				throw new System.NotSupportedException();
			}
		}
		public override long Position {
			get {
				throw new System.NotSupportedException();
			}
			set {
				throw new System.NotSupportedException();
			}
		}
	}
}
