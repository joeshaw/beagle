using System;
using System.Threading;
using System.IO;

using Lucene.Net.Index;

namespace Lucene.Net.Store
{
	/* ====================================================================
	 * The Apache Software License, Version 1.1
	 *
	 * Copyright (c) 2001 The Apache Software Foundation.  All rights
	 * reserved.
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
	/// An interprocess mutex lock.
	/// <p>Typical use might look like:<pre>
	/// new Lock.With(directory.MakeLock("my.lock")) {
	///     public override Object DoBody() {
	///       <i>... code to execute while locked ...</i>
	///     }
	///   }.run();
	/// </pre></p>
	/// <see cref="Directory.MakeLock(String)"/>
	/// </summary>
	/// <author>Doug Cutting</author> 
	public abstract class Lock 
	{
		 public static int LOCK_POLL_INTERVAL = 1000;
		 
		/// <summary>
		/// Attempt to obtain exclusive access.
		/// </summary>
		/// <returns>iff exclusive access is obtained</returns>
		public abstract bool Obtain();

		/// <summary>
		/// Attempts to obtain an exclusive lock within amount
		///  of time given. Currently polls once per second until
		///  lockWaitTimeout is passed.
		/// </summary>
		/// <param name="lockWaitTimeout">length of time to wait in ms</param>
		/// <returns>true if lock was obtained</returns>
		public bool Obtain(long lockWaitTimeout)
		{
			bool locked = Obtain();
			int maxSleepCount = (int)(lockWaitTimeout / LOCK_POLL_INTERVAL);
			int sleepCount = 0;

			// FIXED trow@ximian.com 2004 May 8
			// We shouldn't just fail right away if lockWaitTimeout < LOCK_POLL_INTERVAL.
			maxSleepCount = Math.Max (maxSleepCount, 1);

			while (!locked) 
			{
				// FIXED trow@ximian.com 2004 May 8
				// Lock would time out before first sleep if maxSleepCount == 1
				if (sleepCount == maxSleepCount) 
				{
					throw new IOException("Lock obtain timed out");
				}
				++sleepCount;

				try 
				{
					Thread.Sleep(LOCK_POLL_INTERVAL);
				} 
				catch (Exception e) 
				{
					throw new IOException(e.ToString());
				}
				locked = Obtain();
			}
			return locked;
		}
		/// <summary>
		/// Release exclusive access.
		/// </summary>
		public abstract void Release();

		/// <summary>
		/// Returns true if the resource is currently locked.  Note that one must
		/// still call {@link #obtain()} before using the resource.
		/// </summary>
		public abstract bool IsLocked();

		/// <summary>
		/// Utility class for executing code with exclusive access.
		/// </summary>
		public abstract class With 
		{
			private Lock _lock;
			//private int sleepInterval = 1000;
			//private int maxSleeps = 60;
			
			private long lockWaitTimeout;
    
			/// <summary>
			/// Constructs an executor that will grab the named _lock.
			/// </summary>
			/// <param name="_lock"></param>
			public With(Lock _lock): this(_lock, IndexWriter.COMMIT_LOCK_TIMEOUT)
			{}

			public With(Lock _lock, long lockWaitTimeout)
			{
				this._lock = _lock;
				this.lockWaitTimeout = lockWaitTimeout;	
			}
			/// <summary>
			/// Code to execute with exclusive access.
			/// </summary>
			/// <returns></returns>
			public abstract Object DoBody();

			/// <summary>
			/// Calls DoBody while <i>_lock</i> is obtained.  Blocks if lock
			/// cannot be obtained immediately.  Retries to obtain lock once per second
			/// until it is obtained, or until it has tried ten times. 
			/// </summary>
			/// <returns></returns>
			public Object Run()  
			{
				bool locked = false;
				try 
				{
					locked = _lock.Obtain(lockWaitTimeout);
					return DoBody();
				} 
				finally 
				{
					if (locked)
						_lock.Release();
				}
			}
		}
	}
}
