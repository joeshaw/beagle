//
// Logger.cs
//
// Copyright (C) 2004 Novell, Inc.
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
using System.Collections;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Beagle.Util {
		
	public enum LogLevel {
		None,
		Fatal,
		Error,
		Warn,
		Info,
		Debug
	}

	public class Logger {

		private static Hashtable loggers = new Hashtable ();
		private static LogLevel defaultLevel = LogLevel.Info;
		private static TextWriter defaultWriter = System.Console.Out;

		public static Logger Log {
			get {
				return Get ("other");
			}
		}

		public static Logger Get (string logName) 
		{
			if (loggers.ContainsKey (logName)) {
				return (Logger)loggers[logName];
			} else {
				Logger log = new Logger (logName);
				log.Level = defaultLevel;
				log.Writer = defaultWriter;
				loggers[logName] = log;
				return log;
			}			
		}			

		public static LogLevel DefaultLevel {
			get { return defaultLevel; }
			set { defaultLevel = value; }
		}			       

		
		public static TextWriter DefaultWriter {
			get { return defaultWriter; }
			set { defaultWriter = value; }
		}      

		private bool levelSet = false;
		private LogLevel level;
		private TextWriter writer = null;

		private Logger (string name) {
		}

		public LogLevel Level {
			get { lock (this) { return (levelSet) ? level : DefaultLevel; } }
			set { lock (this) { level = value; levelSet = true; } }
		}

		public TextWriter Writer {
			get { return (writer != null) ? writer : DefaultWriter; }
			set { writer = value; }
		}

		private string GetStamp ()
                {
			if (Writer != Console.Out) {
				return string.Format ("{0}[{1}] {2} ",
						      Process.GetCurrentProcess().Id,
						      Environment.CommandLine,
						      DateTime.Now.ToString ("yy-MM-dd HH.mm.ss.ff"));
			} else {
				return "";
			}
		}


		private void WriteLine (string level, string message) {
			Writer.WriteLine ("{0}{1}: {2}", GetStamp (), level, message);
			Writer.Flush ();
		}

		public void Debug (string message, params object [] args)
		{
			if (IsDebugEnabled) {
				WriteLine ("DEBUG", String.Format (message, args));
			}
		}

		public void Debug (Exception e) 
		{
			Debug ("{0}", e);
		}

		public void Info (string message, params object [] args)
		{
			if (IsInfoEnabled) {
				WriteLine ("INFO", String.Format (message, args));
			}
		}

		public void Info (Exception e) 
		{
			Info ("{0}", e);
		}

		public void Warn (string message, params object [] args)
		{
			if (IsWarnEnabled) {
				WriteLine ("WARN", String.Format (message, args));
			}
		}

		public void Warn (Exception e) 
		{
			Warn ("{0}", e);
		}

		public void Error (string message, params object [] args)
		{
			if (IsErrorEnabled) {
				WriteLine ("ERROR", String.Format (message, args));
			}
		}

		public void Error (Exception e) 
		{
			Error ("{0}", e);
		}

		public void Fatal (string message, params object [] args)
		{
			if (IsFatalEnabled) {
				Fatal ("FATAL", String.Format (message, args));
			}
		}
		
		public void Fatal (Exception e) 
		{
			Fatal ("{0}", e);
		}

		public bool IsDebugEnabled { get { return level >= LogLevel.Debug; } }
		public bool IsInfoEnabled { get { return level >= LogLevel.Info; } }
		public bool IsWarnEnabled { get { return level >= LogLevel.Warn; } }
		public bool IsErrorEnabled { get { return level >= LogLevel.Error;} }
		public bool IsFatalEnabled { get { return level >= LogLevel.Fatal; } }
	}
}

