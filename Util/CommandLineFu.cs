//
// CommandLineFu.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Reflection;
using System.Text;

namespace CommandLineFu {

	[AttributeUsage (AttributeTargets.Field)]
	public class OptionAttribute : Attribute {
		public string Name;
		public string LongName;
		public string Description = "(no description)";
		public string ArgDescription = "arg";
	}

        internal class Processor {

		object target_object;

		public Processor (object target_object)
		{
			this.target_object = target_object;
		}

		///////////////////////////////////////////////////////////////////

		private class Pair {

			public FieldInfo Field;
			public OptionAttribute Option;

			private object target_object;
			private bool touched = false;


			public Pair (object target)
			{
				target_object = target;
			}

			public string Name {
				get { return Field.Name; }
			}

			public string OptionName {
				get {
					if (Option.LongName != null)
						return "--" + Option.LongName;
					else
						return "-" + Option.Name;
				}
			}

			public string UsageName {
				get {
					StringBuilder builder = new StringBuilder ();
					if (Option.LongName != null) {
						builder.Append ("--");
						builder.Append (Option.LongName);
						
						if (Option.Name != null)
							builder.Append (", ");
					}
					if (Option.Name != null) {
						builder.Append ("-");
						builder.Append (Option.Name);
					}
					if (Field.FieldType != typeof (System.Boolean)) {
						builder.Append (" [");
						builder.Append (Option.ArgDescription);
						builder.Append ("]");
					}
					return builder.ToString ();
				}
			}

			public bool Touched {
				get { return touched; }
			}

			public object Value {
				get { return Field.GetValue (target_object); }
			}

			// Return true if value was actually used
			public bool Set (string value)
			{
				touched = true;

				// Deal with bools first, since they are easy.
				if (Field.FieldType == typeof (System.Boolean)) {
					Field.SetValue (target_object, true);
					return false;
				}

				object parsed_value = null;

				if (Field.FieldType == typeof (System.String)) {

					parsed_value = value;

				}  else if (Field.FieldType == typeof (System.Int32)) {

					try {
						parsed_value = System.Int32.Parse (value);
					} catch (Exception ex) {
						// parsed_value will still be null, so the
						// "Couldn't parse..." error will be displayed.
					}

				} else if (Field.FieldType == typeof (System.Double)) {

					try {
						parsed_value = System.Double.Parse (value);
					} catch (Exception ex) { }				
				}
				
				if (parsed_value != null) {
					Field.SetValue (target_object, parsed_value);
				} else {
					Console.WriteLine ("Couldn't parse '{0}' as {1}", value, Field.FieldType);
				}

				return true;
			}
		}

		ArrayList all_pairs = new ArrayList ();
		Hashtable by_name = new Hashtable ();
		Hashtable by_long_name = new Hashtable ();
		
		public void AddOption (FieldInfo field, OptionAttribute option)
		{
			Pair pair = new Pair (target_object);
			pair.Field = field;
			pair.Option = option;

			all_pairs.Add (pair);

			if (option.Name != null)
				by_name [option.Name] = pair;

			if (option.LongName != null)
				by_long_name [option.LongName] = pair;
		}

		private static bool IsOption (string arg)
		{
			return arg.StartsWith ("-") || arg.StartsWith ("/");
		}

		private Pair ParseOption (string arg, out string next_arg)
		{
			next_arg = null;

			char [] separator_chars = new char [] { '=', ':' };
			int i = arg.IndexOfAny (separator_chars);
			if (i != -1) {
				next_arg = arg.Substring (i+1);
				arg = arg.Substring (0, i);
			}

			string stripped_arg = null;
			if (arg.StartsWith ("/")) {
				stripped_arg = arg.Substring (1);
			} else if (arg.StartsWith ("-")) {
				int pos = 1;
				while (pos < arg.Length && arg [pos] == '-')
					++pos;
				stripped_arg = arg.Substring (pos);
			}

			Pair pair = null;
			pair = by_long_name [stripped_arg] as Pair;
			if (pair == null)
				pair = by_name [stripped_arg] as Pair;

			return pair;
		}

		///////////////////////////////////////////////////////////////////

		public void Spew ()
		{
			foreach (Pair pair in all_pairs) {
				Console.WriteLine ("DEBUG  {0}: {1}={2} {3}",
						   pair.OptionName,
						   pair.Name,
						   pair.Value,
						   pair.Touched ? "" : "(default)");
			}
		}

		///////////////////////////////////////////////////////////////////

		public void SpewVersion ()
		{
			Console.WriteLine (CommandLine.ProgramVersion != null ? CommandLine.ProgramVersion : "unknown");
		}

		public void SpewBanner ()
		{
			if (CommandLine.ProgramName == null)
				return;
			Console.Write (CommandLine.ProgramName);
			if (CommandLine.ProgramVersion != null) {
				Console.Write (" ");
				Console.Write (CommandLine.ProgramVersion);
			}
			if (CommandLine.ProgramDate != null) {
				Console.Write (" - ");
				Console.Write (CommandLine.ProgramDate);
			}
			Console.WriteLine ();

			if (CommandLine.ProgramCopyright != null)
				Console.WriteLine (CommandLine.ProgramCopyright);
				
		}

		public void SpewOptionDocs ()
		{
			int max_usage_name_len = 0;

			// FIXME: This need better formatting, wrapping of
			// description lines, etc.
			// It should also be put in a sane order.

			Console.WriteLine ("Options:");

			foreach (Pair pair in all_pairs) {
				int len = pair.UsageName.Length;
				if (len > max_usage_name_len)
					max_usage_name_len = len;
			}
			
			foreach (Pair pair in all_pairs) {
				StringBuilder builder = new StringBuilder ();
				string usage_name = pair.UsageName;
				builder.Append (usage_name);
				builder.Append (' ', max_usage_name_len - usage_name.Length);
				builder.Append ("  ");
				builder.Append (pair.Option.Description);
				
				Console.WriteLine (builder.ToString ());
			}
		}

		///////////////////////////////////////////////////////////////////
		
		private string [] TheRealWork (string [] args)
		{
			ArrayList parameters = new ArrayList ();

			int i = 0;
			bool parameters_only = false;
			while (i < args.Length) {
				string arg = args [i];
				++i;

				string next_arg = null;
				if (i < args.Length)
					next_arg = args [i];
					
				if (parameters_only || ! IsOption (arg)) {
					parameters.Add (arg);
				} else if (arg == "--") {
					parameters_only = true;
				} else {
					string attached_next_arg = null;
					Pair pair = ParseOption (arg, out attached_next_arg);

					if (pair == null) {
						Console.WriteLine ("Ignoring unknown argument '{0}'", arg);
					} else if (attached_next_arg != null) {
						if (! pair.Set (attached_next_arg)) {
							// FIXME: If we didn't use the attached arg, something must be wrong.
							// Throw an exception?
							Console.WriteLine ("FIXME: Didn't use attached arg '{0}' on {1}",
									   attached_next_arg,
									   pair.OptionName);
						}
					} else {
						if (pair.Set (next_arg))
							++i;
					}
				}
			}

			// If we ended prematurely, treat everything that is left
			// as a parameter.
			while (i < args.Length)
				parameters.Add (args [i]);
			
			// Convert the list of parameters to an array and return it.
			string [] parameter_array = new string [parameters.Count];
			for (int j = 0; j < parameters.Count; ++j)
				parameter_array [j] = parameters [j] as string;
			return parameter_array;
		}

		public string [] Process (string [] args)
		{
			foreach (string arg in args) {
				// FIXME: These should be displayed in the banner information.
				if (arg == "--version") {
					SpewVersion ();
					return null;
				} else if (arg == "--help") {
					SpewBanner ();
					SpewOptionDocs ();
					return null;
				}
			}

			string [] parameters = TheRealWork (args);

			if (CommandLine.Debug) {
				Spew ();
				for (int i = 0; i < parameters.Length; ++i)
					Console.WriteLine ("DEBUG  Param {0}: {1}", i, parameters [i]);
			}
			
			return parameters;
		}
	}

	public class CommandLine {

		static public bool Debug = false;

		static public string ProgramName = null;
		static public string ProgramVersion = null;
		static public string ProgramDate = null;
		static public string ProgramCopyright = null;
		static public string ProgramHomePage = null;

		static private Processor BuildProcessor (Type type, object obj, BindingFlags flags)
		{
			Processor proc = new Processor (obj);

			flags |= BindingFlags.NonPublic;
			flags |= BindingFlags.Public;

			FieldInfo [] field_info_array = type.GetFields (flags);
			foreach (FieldInfo fi in field_info_array) {

				object [] attributes = fi.GetCustomAttributes (true);
				foreach (object attr in attributes) {
					OptionAttribute option_attr = attr as OptionAttribute;
					if (option_attr != null)
						proc.AddOption (fi, option_attr);
				}
			}

			return proc;
		}

		static public string [] Process (object obj, string [] args)
		{
			Processor proc = BuildProcessor (obj.GetType (), obj, BindingFlags.Instance);
			return proc.Process (args);
		}

		static public string [] Process (Type t, string [] args)
		{
			Processor proc = BuildProcessor (t, null, BindingFlags.Static);
			return proc.Process (args);
		}

	}



#if false	
	class CommandLineFu_SampleCode {

		[Option (Name="f",
			 LongName="foo",
			 Description="The Foo Option",
			 ArgDescription="FOOARG")]
		static private string foo = "foo_default";

		[Option (LongName="bar",
			 Description="The Bar Option")]
		static private int bar = 12345;

		[Option (LongName="baz",
			 Description="The Baz Option")]
		static private bool baz = false;

		[Option (Name="d",
			 Description="As you might expect, the d option")]
		static private double d = 3.14159;

		static void Main (string [] args)
		{
			CommandLine.Debug = true;
			CommandLine.Process (typeof (CommandLineFu_SampleCode), args);
		}
	}
#endif

}
