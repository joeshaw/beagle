
using System;
using System.IO;
using System.Text;

namespace Bludgeon {

	public class Log {

		static TextWriter console = Console.Out;
		static TextWriter file = null;

		static private void Write (string prefix, string format, params object [] args)
		{
			StringBuilder builder;
			builder = new StringBuilder ();
			builder.AppendFormat ("{0:yy-MM-dd HH.mm.ss.ff} ", DateTime.Now);
			builder.Append (prefix);
			builder.Append (' ');
			builder.AppendFormat (format, args);

			string message;
			message = builder.ToString ();

			if (console != null)
				console.WriteLine (message);

			if (file != null) {
				file.WriteLine (message);
				file.Flush ();
			}
		}

		static public void Create (string path)
		{
			file = new StreamWriter (path);
		}

		static public void Spew (string format, params object [] args)
		{
			Write ("---", format, args);
		}

		static public void Info (string format, params object [] args)
		{
			Write ("+++", format, args);
		}

		static public void Failure (string format, params object [] args)
		{
			Write ("***", format, args);
		}
	}
}
