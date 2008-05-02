using System;
using System.IO;
using System.Text;

using Beagle;

public class RdfQueryTool {
	public static void Main (string[] args)
	{

		if (args.Length != 3) {
			Console.WriteLine ("Usage: program-name <subject> <predicate> <predicate-type> <object>");
			Console.WriteLine ("      <subject>         : URI or path");
			Console.WriteLine ("      <predicate>       : property name (string)");
			Console.WriteLine ("      <object>          : object (string)");
			Console.WriteLine ("      Use \"\" (empty string) for unspecified subject, predicate, type or object");
			return;
		}

		RDFQueryResult result;

		Console.WriteLine ("subject:'{0}' predicate:'{1}' object:'{2}'", args [0], args [1], args [2]);

		Uri subject = null;
		if (args [0] != String.Empty)
			subject = new Uri (args [0]);

		RDFQuery query = new RDFQuery (subject, args [1], args [2]);
		result = (RDFQueryResult) query.Send ();

		if (result == null) {
			Console.WriteLine ("null.......");
			return;
		}

		foreach (Hit hit in result.Hits) {
			foreach (Property prop in hit.Properties)
				Console.WriteLine ("<{0}> <{1}> \"{2}\" .",
						hit.Uri,
						prop.Key,
						prop.Value);
		}
	
	}
}
