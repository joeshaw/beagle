using System;
using System.IO;
using System.Text;
using System.Configuration;
using System.Runtime.CompilerServices;
using System.Collections;
using Lucene.Net.Util;
using System.Diagnostics; // FIXED trow 2004 May 14 - for lock debugging
using System.Security.Cryptography;

namespace Lucene.Net.Store
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001, 2002, 2003 The Apache Software Foundation.  All
	 * rights reserved.
	 *
	 * Redistribution and use in source and binary forms, with or without
	 * modification, are permitted provided that the following conditions
	 * are met:
	 *
	 * 1. Redistributions of source code must retain the above copyright
	 *    notice, this list of conditions and the following disclaimer.
	 *
	 * 2. Redistributions in binary form must reproduce the above copyright
	 *    notice, this list of conditions and the following disclaimer in
	 *    the documentation and/or other materials provided with the
	 *    distribution.
	 *
	 * 3. The end-user documentation included with the redistribution,
	 *    if any, must include the following acknowledgment:
	 *       "This product includes software developed by the
	 *        Apache Software Foundation (http://www.apache.org/)."
	 *    Alternately, this acknowledgment may appear in the software itself,
	 *    if and wherever such third-party acknowledgments normally appear.
	 *
	 * 4. The names "Apache" and "Apache Software Foundation" and
	 *    "Apache Lucene" must not be used to endorse or promote products
	 *    derived from this software without prior written permission. For
	 *    written permission, please contact apache@apache.org.
	 *
	 * 5. Products derived from this software may not be called "Apache",
	 *    "Apache Lucene", nor may "Apache" appear in their name, without
	 *    prior written permission of the Apache Software Foundation.
	 *
	 * THIS SOFTWARE IS PROVIDED ``AS IS'' AND ANY EXPRESSED OR IMPLIED
	 * WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
	 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
	 * DISCLAIMED.  IN NO EVENT SHALL THE APACHE SOFTWARE FOUNDATION OR
	 * ITS CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
	 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
	 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF
	 * USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
	 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
	 * OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
	 * OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF
	 * SUCH DAMAGE.
	 * ====================================================================
	 *
	 * This software consists of voluntary contributions made by many
	 * individuals on behalf of the Apache Software Foundation.  For more
	 * information on the Apache Software Foundation, please see
	 * <http://www.apache.org/>.
	 */

	/// <summary>
	/// Straightforward implementation of Directory as a directory of files.
	/// <p>If the system property 'disableLuceneLocks' has the String value of
	/// "true", lock creation will be disabled.</p>
	/// <see cref="Directory"/>
	/// </summary>
	/// <author>Doug Cutting</author>
	public sealed class FSDirectory : Directory 
	{
		/// <summary>
		/// This cache of directories ensures that there is a unique Directory
		/// instance per path, so that synchronization on the Directory can be used to
		/// synchronize access between readers and writers.
		/// </summary>
		private static readonly Hashtable DIRECTORIES = new System.Collections.Hashtable();

		private static readonly bool DISABLE_LOCKS =
			ConfigurationSettings.AppSettings.Get("disableLuceneLocks") != null;

		private static MD5 DIGESTER = MD5.Create();

		/// A buffer optionally used in RenameTo method 
		private byte[] buffer = null;

		/// <summary>
		/// Returns the directory instance for the named location.
		/// <p>Directories are cached, so that, for a given canonical path, the same
		/// FSDirectory instance will always be returned.  This permits
		/// synchronization on directories.
		/// </p>
		/// </summary>
		/// <param name="path">the path to the directory.</param>
		/// <param name="create">if true, create, or erase any existing contents.</param>
		/// <returns>the FSDirectory for the named file.</returns>
		public static FSDirectory GetDirectory(String path, bool create)
		{
			DirectoryInfo dirinfo = new DirectoryInfo(path);
			return GetDirectory(dirinfo, create);
		}

		/// <summary>
		/// Returns the directory instance for the named location.
		/// <p>Directories are cached, so that, for a given canonical path, the same
		/// FSDirectory instance will always be returned.  This permits
		/// synchronization on directories.</p>
		/// </summary>
		/// <param name="file">the path to the directory.</param>
		/// <param name="_create">if true, create, or erase any existing contents.</param>
		/// <returns>the FSDirectory for the named file.</returns>
		public static FSDirectory GetDirectory(DirectoryInfo file, bool _create)
		{
			FSDirectory dir;
			lock (DIRECTORIES) 
			{
				dir = (FSDirectory)DIRECTORIES[file];

				if (dir == null) 
				{
					dir = new FSDirectory(file, _create);
					DIRECTORIES.Add(file, dir);
				} 
				else if (_create) 
				{
					dir.Create();
				}
			}
			lock (dir) 
			{
				dir.refCount++;
			}
			return dir;
		}

		private DirectoryInfo directory = null;
		private int refCount;

		private FSDirectory(DirectoryInfo path, bool create)  
		{
			directory = path;

			if (create)
				Create();
		}

		[MethodImpl(MethodImplOptions.Synchronized)]
		private void Create()  
		{
			if (!directory.Exists)
			{
				try
				{
					directory.Create();
				}
				catch (Exception ex)
				{
					throw new IOException("Cannot create directory: " + directory, ex);
				}
			}

			FileInfo[] files = directory.GetFiles();            // clear old files
			for (int i = 0; i < files.Length; i++) 
			{
				try
				{
					files[i].Delete();
				}
				catch (Exception ex)
				{
					throw new IOException("couldn't delete " + files[i], ex);
				}
			}
			
			string lockPrefix = GetLockPrefix().ToString(); // clear old locks

			// FIXED trow@ximian.com 14 May 2004  Use TempDirectoryName to find where locks live
			DirectoryInfo tmpdir = new DirectoryInfo(TempDirectoryName);
			files = tmpdir.GetFiles();
			for (int i = 0; i < files.Length; i++) 
			{      
				if (!files[i].Name.StartsWith(lockPrefix))
					continue;

				try
				{
					files[i].Delete();
				}
				catch(Exception ex)
				{
					throw new IOException("couldn't delete " + files[i], ex);
				}
			}

		}

		/// <summary>
		/// Returns an array of strings, one for each file in the directory.
		/// </summary>
		/// <returns></returns>
		public override String[] List()  
		{
			FileInfo[] files = directory.GetFiles();
			string[] str = new string[files.Length];
			for(int i=0; i < str.Length; i++)
			{
				str[i] = files[i].FullName;
			}
			return str;
		}

		/// <summary>
		/// Returns true iff a file with the given name exists.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override bool FileExists(String name)  
		{
			return GetFileInfo(name).Exists;
		}

		/// <summary>
		/// Returns the time the named file was last modified.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override long FileModified(String name)  
		{
			return Date.GetTime(GetFileInfo(name).LastWriteTime);
		}

		public static long GetFileModified(string name)
		{
			return Date.GetTime(new FileInfo(name).LastWriteTime);
		}

		/// <summary>
		/// Returns the time the named file was last modified.
		/// </summary>
		/// <param name="directory"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static long FileModified(DirectoryInfo directory, String name)
		{
			FileInfo fileInfo = new FileInfo(directory.FullName + "/" + name);
			return Date.GetTime(fileInfo.LastWriteTime);
		}

		/// <summary>
		/// Set the modified time of an existing file to now.
		/// </summary>
		/// <param name="name"></param>
		public override void TouchFile(String name)  
		{
			FileInfo file = GetFileInfo(name);
			file.LastWriteTime = DateTime.Now;
		}

		/// <summary>
		/// Returns the length in bytes of a file in the directory.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		override public long FileLength(String name)  
		{
			return GetFileInfo(name).Length;
		}

		private FileInfo GetFileInfo(string file) 
		{
			return new FileInfo(GetFileFullName(file));
		}

		private string GetFileFullName(string file)
		{
			return directory.FullName + "/" + file;
		}

		/// <summary>
		/// Removes an existing file in the directory.
		/// </summary>
		/// <param name="name"></param>
		override public void DeleteFile(String name)  
		{
			try
			{
				GetFileInfo(name).Delete();
			}
			catch (Exception ex)
			{
				throw new IOException("couldn't delete " + name, ex);
			}
		}

		/// <summary>
		/// Renames an existing file in the directory.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		override public void RenameFile(String from, String to)
		{
			FileInfo nu = GetFileInfo(to);

			if (nu.Exists)
			{
				try
				{
					nu.Delete();
				}
				catch (IOException)
				{
					throw new IOException("couldn't delete " + to);
				}
			}

			// Rename the old file to the new one. 
			try
			{
				GetFileInfo(from).MoveTo(GetFileFullName(to));
			}
			catch(Exception e)
			{
				throw new IOException("couldn't rename " + from + " to " + to, e);
			}
		}

		/// <summary>
		/// Creates a new, empty file in the directory with the given name.
		/// Returns a stream writing this file.
		/// </summary>
		public override OutputStream CreateFile(String name)  
		{
			return new FSOutputStream(GetFileInfo(name));
		}

		/// <summary>
		/// Returns a stream reading an existing file.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public override InputStream OpenFile(String name)  
		{
			return new FSInputStream(GetFileInfo(name));
		}

		// ADDED trow 4 June 2004
		static public Beagle.Util.Logger Logger = null;
		static private void Log (string format, params object[] args)
		{
			if (Logger != null)
				Logger.Log (format, args);
		}
		static private void Log (Exception e)
		{
			if (Logger != null)
				Logger.Log (e);
		}

		class FSDirectoryLock : Lock
		{

			FileInfo lockFile;
			internal FSDirectoryLock(FileInfo lockFile) : base()
			{
				this.lockFile = lockFile;
			}

			public override bool Obtain()  
			{
				if (DISABLE_LOCKS)
					return true;
				
				try
				{
					lock (this)
					{
						FileStream fs = new FileStream(lockFile.FullName,
									       FileMode.CreateNew,
									       FileAccess.Write);

						// ADDED trow@ximian.com 4 Jun 2004 - lock debugging
						Log ("Obtained lock {0}", lockFile.FullName);

						StreamWriter sw = new StreamWriter (fs);
						Process us = Process.GetCurrentProcess ();
						sw.WriteLine (us.Id.ToString ());
						sw.WriteLine (DateTime.Now.ToString ());
						sw.Close ();
						fs.Close();
					}
				}
				catch (Exception e)
				{
					// ADDED trow@ximian.com 4 June 2004 - lock debugging
					Log ("Could not obtain lock {0}", lockFile.FullName);
					Log (e);
					return false;
				}
				return true;
			}
			public override void Release() 
			{
				if (DISABLE_LOCKS)
					return;

				lock (this)
				{
					// ADDED trow@ximian.com 4 June 2004 - lock debugging
					try {
						lockFile.Delete();
						Log ("Released lock {0}", lockFile.FullName);
					} catch (Exception e) {
						Log ("Failed to release lock {0}", lockFile.FullName);
						Log (e);
					}
				}
			}
			
			public override bool IsLocked() 
			{
				if (DISABLE_LOCKS)
					return false;
				return lockFile.Exists;
			}
			
			public override String ToString() 
			{
				return "Lock@" + lockFile;
			}
		}


		/**
		 * So we can do some byte-to-hexchar conversion below
		 */
		private static char[] HEX_DIGITS =
		{'0','1','2','3','4','5','6','7','8','9','a','b','c','d','e','f'};


		/// <summary>
		/// Constructs a Lock with the specified name.  Locks are implemented
		/// with File.CreateNewFile().
		/// <p>In JDK 1.1 or if system property <I>disableLuceneLocks</I> is the
		/// string "true", locks are disabled.  Assigning this property any other
		/// string will <B>not</B> prevent creation of lock files.  This is useful for
		/// using Lucene on read-only medium, such as CD-ROM.</p>
		/// </summary>
		/// <param name="name">the name of the lock file</param>
		/// <returns>an instance of <code>Lock</code> holding the lock</returns>
		override public Lock MakeLock(String name) 
		{
			StringBuilder buf = GetLockPrefix();
			buf.Append("-");
			buf.Append(name);

			// FIXED trow@ximian.com 14 May 2004  Use TempDirectoryName to find where locks live
			DirectoryInfo tmpDir = new DirectoryInfo(TempDirectoryName);
			// make the lock file in tmp, where anyone can create files.
			FileInfo lockFile = new FileInfo(tmpDir.FullName + "/" + buf.ToString());

			return new FSDirectoryLock(lockFile);
		}

		/// <summary>
		/// Closes the store to future operations.
		/// </summary>
		[MethodImpl(MethodImplOptions.Synchronized)]
		public override void Close()  
		{
			if (--refCount <= 0) 
			{
				lock (DIRECTORIES) 
				{
					DIRECTORIES.Remove(directory);
				}
			}
		}

		/// <summary>
		/// For debug output.
		/// </summary>
		/// <returns></returns>
		override public String ToString() 
		{
			return "FSDirectory@" + directory;
		}

		/// <summary>
		/// Get the name of the directory to use for temporary files,
		/// and create that directory if it doesn't already exist
		/// </summary>
		/// FIXED trow@ximian.com 14 May 2004 Give us control over where locks are stored
		static private String tempDirectoryName = null;
		static public String TempDirectoryName {
			get {
				if (tempDirectoryName == null) {
					String user_name = Environment.GetEnvironmentVariable("USER");
					if (user_name == null)
						user_name = "unknown";
					TempDirectoryName = "/tmp/" + user_name + "-lucene.net";
				}
				return tempDirectoryName;
			}

			set {
				tempDirectoryName = value;
				if (! System.IO.Directory.Exists (tempDirectoryName))
					System.IO.Directory.CreateDirectory (tempDirectoryName);
			}
		}

		private StringBuilder GetLockPrefix() 
		{
			String dirName = directory.FullName;;                               // name to be hashed
    
			byte[] digest;
				
			lock(DIGESTER) 
			{
				digest = DIGESTER.ComputeHash(Encoding.UTF8.GetBytes(dirName));
			}
			
			StringBuilder buf = new StringBuilder();
			buf.Append("lucene-");
			for (int i = 0; i < digest.Length; i++) 
			{
				int b = digest[i];
				buf.Append(HEX_DIGITS[(b >> 4) & 0xf]);
				buf.Append(HEX_DIGITS[b & 0xf]);
			}

			return buf;
		}

	}

	public sealed class FSInputStream : InputStream, ICloneable
	{
		private class Descriptor : BinaryReader
		{
			public long position;
			public Descriptor(FileInfo file, FileAccess fileAccess) 
				: base(new FileStream(file.FullName, FileMode.Open, fileAccess, FileShare.ReadWrite))
			{
			}
		};

		Descriptor file = null;
		bool isClone;

		public bool IsClone
		{
			get
			{
				return isClone;
			}
		}
		
		public FSInputStream(FileInfo path)  
		{
			file = new Descriptor(path, FileAccess.Read);
			length = file.BaseStream.Length;
		}

		~FSInputStream()  
		{
			Close();            // close the file
		}

		/** InputStream methods */
		protected override void ReadInternal(byte[] b, int offset, int len)
		{
			lock (file) 
			{
				long position = GetFilePointer();
				if (position != file.position) 
				{
					file.BaseStream.Seek(position, SeekOrigin.Begin);
					file.position = position;
				}
				int total = 0;
				do 
				{
					int i = file.Read(b, offset+total, len-total);
					if (i == 0)
						throw new IOException("read past EOF");
					file.position += i;
					total += i;
				} while (total < len);
			}
		}

		override public void Close()  
		{
			if (!isClone && file != null)
				file.Close();
		}

		/// <summary>
		/// Random-access method
		/// </summary>
		/// <param name="position"></param>
		override protected void SeekInternal(long position)  
		{
		}

		public override Object Clone() 
		{
			FSInputStream clone = (FSInputStream)base.Clone();
			clone.isClone = true;
			return clone;
		}
		
		/// <summary>
		/// Method used for testing. Returns true if the underlying
		/// file descriptor is valid.
		/// </summary>
		/// <returns></returns>
		public bool IsFDValid()
		{
			return file.BaseStream.CanRead;
		}
	}


	sealed class FSOutputStream : OutputStream 
	{
		internal BinaryWriter file = null;
		internal FileInfo path;

		public FSOutputStream(FileInfo path)  
		{
			this.path = path;
			file = new BinaryWriter(
				new FileStream(path.FullName, FileMode.OpenOrCreate, 
					FileAccess.Write, FileShare.ReadWrite)
			);
		}

		/// Output methods
		 
		override public void FlushBuffer(byte[] b, int size)  
		{
			file.Write(b, 0, size);
		}
		override public void Close()  
		{
			base.Close();
			file.Close();
		}

		/// Random-access methods 
		override public void Seek(long pos)  
		{
			base.Seek(pos);
			file.BaseStream.Seek(pos, SeekOrigin.Begin);
		}
		override public long Length()  
		{
			return file.BaseStream.Length;
		}
		
		~FSOutputStream()  
		{
			if (file != null)
			{
				file.Close();          // close the file
			}
		}
	}
}
