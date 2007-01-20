//
// SafeProcess.cs
//
// Copyright (C) 2006 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using Mono.Unix;
using GLib;

namespace Beagle.Util {

	public class SafeProcess {

		private delegate void ChildProcessSetupWrapperDelegate (IntPtr user_data);
		public delegate void ChildProcessSetupDelegate ();

		private bool redirect_stdin, redirect_stdout, redirect_stderr;
		private string[] args;
		private UnixStream stdin_stream, stdout_stream, stderr_stream;
		private int pid;

		public ChildProcessSetupDelegate ChildProcessSetup;

		public string[] Arguments {
			get { return args; }
			set { args = value; }
		}

		public bool RedirectStandardInput {
			get { return redirect_stdin; }
			set { redirect_stdin = value; }
		}

		public bool RedirectStandardOutput {
			get { return redirect_stdout; }
			set { redirect_stdout = value; }
		}

		public bool RedirectStandardError {
			get { return redirect_stderr; }
			set { redirect_stderr = value; }
		}

		public Stream StandardInput {
			get { return stdin_stream; }
		}

		public Stream StandardOutput {
			get { return stdout_stream; }
		}

		public Stream StandardError {
			get { return stderr_stream; }
		}

		public int Id {
			get { return pid; }
		}

		[DllImport ("libglib-2.0.so.0")]
		static extern bool g_spawn_async_with_pipes (string working_directory,
							     string[] argv,
							     string[] envp,
							     int flags,
							     ChildProcessSetupWrapperDelegate child_setup,
							     IntPtr child_data,
							     out int pid,
							     [In,Out] IntPtr standard_input,
							     [In,Out] IntPtr standard_output,
							     [In,Out] IntPtr standard_error,
							     out IntPtr error);

		public void Start ()
		{
			if (args == null)
				throw new ArgumentException ("Arguments cannot be empty");

			IntPtr error;

			if (args [args.Length - 1] != null) {
				// Need to null-terminate the array.
				string[] tmp_args = new string [args.Length + 1];
				Array.Copy (args, tmp_args, args.Length);
				args = tmp_args;
			}

			IntPtr in_ptr = IntPtr.Zero, out_ptr = IntPtr.Zero, err_ptr = IntPtr.Zero;
			ChildProcessSetupWrapperDelegate setup_func = null;

			try {
				if (RedirectStandardInput)
					in_ptr = Marshal.AllocHGlobal (IntPtr.Size);
				
				if (RedirectStandardOutput)
					out_ptr = Marshal.AllocHGlobal (IntPtr.Size);
				
				if (RedirectStandardError)
					err_ptr = Marshal.AllocHGlobal (IntPtr.Size);

				if (ChildProcessSetup != null) {
					setup_func = delegate { 
						// Exceptions in here go to stderr
						try {
							ChildProcessSetup ();
						} catch (Exception e) {
							Console.Error.WriteLine ("Exception caught in child setup function:");
							Console.Error.WriteLine (e);
						}
					};
				}

				g_spawn_async_with_pipes (null, args, null,
							  1 << 2, // G_SPAWN_SEARCH_PATH
							  setup_func,
							  IntPtr.Zero,
							  out pid,
							  in_ptr, out_ptr, err_ptr,
							  out error);

				if (error != IntPtr.Zero)
					throw new SafeProcessException (new GException (error));

				if (in_ptr != IntPtr.Zero) {
					IntPtr v = Marshal.ReadIntPtr (in_ptr);
					stdin_stream = new UnixStream ((int) v);
				}

				if (out_ptr != IntPtr.Zero) {
					IntPtr v = Marshal.ReadIntPtr (out_ptr);
					stdout_stream = new UnixStream ((int) v);
				}

				if (err_ptr != IntPtr.Zero) {
					IntPtr v = Marshal.ReadIntPtr (err_ptr);
					stderr_stream = new UnixStream ((int) v);
				}

			} finally {
				if (in_ptr != IntPtr.Zero)
					Marshal.FreeHGlobal (in_ptr);

				if (out_ptr != IntPtr.Zero)
					Marshal.FreeHGlobal (out_ptr);

				if (err_ptr != IntPtr.Zero)
					Marshal.FreeHGlobal (err_ptr);
			}
		}

		public void Close ()
		{
			if (stdin_stream != null)
				stdin_stream.Close ();

			if (stdout_stream != null)
				stdout_stream.Close ();

			if (stderr_stream != null)
				stderr_stream.Close ();
		}
	}

	public class SafeProcessException : Exception {

		internal SafeProcessException (GException gexception) : base (gexception.Message) { }
	}
			
}