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
		private static TextWriter defaultWriter = null;
		private static bool defaultEcho = false;
		private static string defaultLogName = null;

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
				log.Echo = defaultEcho;
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

		public static bool DefaultEcho {
			get { return defaultEcho; }
			set { defaultEcho = value; }
		}      

		private bool levelSet = false;
		private LogLevel level;
		private TextWriter writer = null;
		private bool echo = false;

		private Logger (string name) {
		}

		public LogLevel Level {
			get { lock (this) { return (levelSet) ? level : DefaultLevel; } }
			set { lock (this) { level = value; levelSet = true; } }
		}

		public TextWriter Writer {
			get { return writer; }
			set { writer = value; }
		}

		public bool Echo {
			get { return echo; }
			set { echo = value; }
		}

		// Multiple logs can be merge-sorted via "sort -m log1 log2 ..."
		private string GetStamp ()
                {
			StringBuilder builder = new StringBuilder ();
			builder.AppendFormat ("{0:yy-MM-dd HH.mm.ss.ff} ", DateTime.Now);
			builder.AppendFormat ("{0:00000} ", Process.GetCurrentProcess().Id);
			if (defaultLogName != null) {
				builder.AppendFormat (defaultLogName);
				builder.Append (' ');
			}
			return builder.ToString ();
		}

		private void WriteLine (string level, string message) {
			if (Writer != null) {
				Writer.WriteLine ("{0}{1}: {2}", GetStamp (), level, message);
				Writer.Flush ();
			} 
			if (Echo)
				System.Console.WriteLine ("{0}: {1}", level, message);
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
				WriteLine ("FATAL", String.Format (message, args));
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

		////////////////////////////////////////////////////////////////////////////////

		static public void LogToFile (string path, string name, bool foreground_mode)
		{
			defaultLogName = name;
			if (defaultLogName.Length > 6)
				defaultLogName = defaultLogName.Substring (0, 6);
			else
				defaultLogName = defaultLogName.PadRight (6);

			string timestamped_name = String.Format ("{0:yyyy-MM-dd-HH-mm-ss}-{1}", DateTime.Now, name);
			string log_path = Path.Combine (path, timestamped_name);
			string log_link = Path.Combine (path, "current-" + name);

			// Open the log file and set it as the default
			// destination for log messages.
			// Also redirect stdout and stderr to the same file.
			FileStream log_stream = new FileStream (log_path,
								FileMode.Append,
								FileAccess.Write,
								FileShare.Write);
			TextWriter log_writer = new StreamWriter (log_stream);

			File.Delete (log_link);
			Mono.Posix.Syscall.symlink (log_path, log_link);

			Logger.DefaultWriter = log_writer;
			Logger.DefaultEcho = foreground_mode;

			if (! foreground_mode) {

				// Redirect stdout and stderr to the logfile
				Console.SetOut (Logger.DefaultWriter);
				Console.SetError (Logger.DefaultWriter);

				// Redirect stdin to /dev/null
				FileStream dev_null_stream = new FileStream ("/dev/null",
									     FileMode.Open,
									     FileAccess.Read,
									     FileShare.ReadWrite);
				TextReader dev_null_reader = new StreamReader (dev_null_stream);
				Console.SetIn (dev_null_reader);
			}
		}

		////////////////////////////////////////////////////////////////////////////////

		static Logger ()
		{
			// Parse the contents of the BEAGLE_DEBUG environment variable
			// and adjust the default log levels accordingly.
			string debug = System.Environment.GetEnvironmentVariable ("BEAGLE_DEBUG");
			if (debug != null) {
				string[] debugArgs = debug.Split (',');
				foreach (string arg in debugArgs) {
					if (arg.Trim () == "all") {
						Logger.DefaultLevel = LogLevel.Debug;
					}
				}
				
				foreach (string arg_raw in debugArgs) {
					string arg = arg_raw.Trim ();

					if (arg.Length == 0 || arg == "all")
						continue;

					if (arg[0] == '-') {
						string name = arg.Substring (1);
						Logger log = Logger.Get (name);
						log.Level = LogLevel.Info;
					} else {
						Logger log = Logger.Get (arg);
						log.Level = LogLevel.Debug;
					}
				}
			}
		}
	}
}

