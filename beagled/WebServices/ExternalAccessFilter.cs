//
// ExternalAccessFilter.cs
//
// Copyright (C) 2005 Novell, Inc.
//
// Authors:
//   Vijay K. Nanjundaswamy (knvijay@novell.com)
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
using System.IO;
using System.Collections;

using Beagle;
using Beagle.Util;

namespace Beagle.WebService {

	public class ExternalAccessFilter 
	{
		ArrayList matchers;

		static readonly string configuration = "shares.cfg";
		
		bool SharesFileExists = false;
		
		public ExternalAccessFilter (string hostname, string port)
		{
			SimpleMatcher defaultMatcher = null;
			 
			matchers = new ArrayList();

			//Check if public folder exists and setup default mapping for it:
			if (!hostname.Equals("localhost") && Directory.Exists(PathFinder.HomeDir + "/public"))
			{				
				defaultMatcher = new SimpleMatcher ();
				//file:///home/userid/public/
				defaultMatcher.Match = "file://" + PathFinder.HomeDir + "/public/"; 
				//http://hostname:8888/beagle/public/
				defaultMatcher.Rewrite = "http://" + hostname + ":" + port + "/beagle/public/";	
			}
			
			if (!File.Exists (Path.Combine (PathFinder.StorageDir, configuration))) {
				matchers.Add(defaultMatcher);
                return;
			}
		
			SharesFileExists = true;
			
            StreamReader reader = new StreamReader(
                         Path.Combine (PathFinder.StorageDir, configuration));

            string entry;

            while ( ((entry = reader.ReadLine ()) != null) && (entry.Trim().Length > 1)) {
            	//Console.WriteLine("Line: " + entry);
            	if ((entry[0] != '#') && (entry.IndexOf(';') > 0)) {
                	string[] data = entry.Split (';');
					SimpleMatcher matcher = new SimpleMatcher ();
					matcher.Match = data[0]; 
					matcher.Rewrite = data[1]; 
					matchers.Add (matcher);
				}
            }
            //Include defaultMatcher at the end of the list. 'shares.cfg', if it exists, 
            //takes higher precedence.
            if (defaultMatcher != null)
            	matchers.Add(defaultMatcher);            	
		}
		
		//Returns: false, if Hit does not match any filter
		//		   true,  if Hit URI is part of any specified filter		
		public bool FilterHit (Hit hit)
		{
			if (matchers.Count == 0)
				return false;
			
			foreach (SimpleMatcher matcher in matchers)
			{
				if (hit.Uri.ToString ().IndexOf (matcher.Match) == -1)
					continue;

				return true;
			}

			return false;		
		}
		
		//Returns: null, if Hit does not match any filter
		//		   Uri,  after Translation
		public string TranslateHit (Hit hit)
		{
			if ((hit == null) || (matchers.Count == 0))
				return null;
							
			string uri = hit.Uri.ToString();			
			string newuri = null;
			
			foreach (SimpleMatcher matcher in matchers)
			{
				if (uri.IndexOf (matcher.Match) == -1)
					continue;
						
				newuri = uri.Replace (matcher.Match, matcher.Rewrite);
				//Console.WriteLine("TranslateHit: " + newuri);
				
				return newuri;
			}

			return null;	//Hit does not match any specified filter	
		}		
		
		internal class SimpleMatcher
		{
			public string Match;
			public string Rewrite;
		}		
	}	
}
