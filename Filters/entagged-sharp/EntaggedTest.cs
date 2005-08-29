using System;
using System.IO;
using Entagged;

public class EntaggedTest
{
	public static void Main(string [] args)
	{
		string testDir = args.Length > 0 ?
			args[0] :
			"../tests/samples";
	
		foreach(string file in Directory.GetFiles(testDir)) {
			try {
				AudioFileWrapper af = new AudioFileWrapper(file);
				Console.WriteLine(af);
			} catch(Exception) {}
		}
	}
}
