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
		static Logger log = Logger.Get ("ExternalAccessFilter");
		string FileUriPrefix = "file://";
		string HttpUriBase = "http://hostname:8888/beagle/";
		string[] reserved_suffixes;
		static readonly string ConfigFile = "publicfolders.cfg";
		
// Exported Folders: The leaf folder name should be unique. 
// This leaf name will be used for the BeagleXSP application list.
						
		public ExternalAccessFilter (string HttpUriBase, string[] reserved_suffixes)
		{						
			this.HttpUriBase = HttpUriBase;
			this.reserved_suffixes = reserved_suffixes;
			
			//Check for ~/.beagle/config/webservices.xml configuration:
			ArrayList publicFolders = Conf.WebServices.PublicFolders;
			
			if ((publicFolders != null) && (publicFolders.Count > 0)) {
			
				SetupFilters(publicFolders);
				
				if (File.Exists (Path.Combine (PathFinder.StorageDir, ConfigFile)))
				{
					log.Warn("ExternalAccessFilter: Detected deprecated configuration file for PublicFolders !");
					log.Info("ExternalAccessFilter: Remove '~/.beagle/publicfolders.cfg' file.\n Use 'beagle-settings' or 'beagle-config' instead to setup public folders.");								
				}
				return;
			}

	    	publicFolders = new ArrayList(); 		
/*	    		
			//Fallback to ~/.beagle/publicfolders.cfg
			if (File.Exists (Path.Combine (PathFinder.StorageDir, ConfigFile))) {
			
            	StreamReader reader = new StreamReader(
                 			Path.Combine (PathFinder.StorageDir, ConfigFile));

            	string entry;
            	while ( ((entry = reader.ReadLine ()) != null) && (entry.Trim().Length > 1)) {
            	   	if (entry[0] != '#')            	
						publicFolders.Add(entry);                 
            	} 
            } 
*/	      
	     	SetupFilters(publicFolders);
		}

/*
		public void ReplaceAccessFilter(ArrayList newlist)
		{					
			bool fa = SetupFilters(newlist);
			
			if (usingPublicFoldersDotCfgFile && fa) {
				usingPublicFoldersDotCfgFile = false;
				log.Warn("ExternalAccessFilter: Duplicate configuration of PublicFolders in '~/.beagle/publicfolders.cfg' and '~/.beagle/config/webservices.xml' !");
				log.Info("ExternalAccessFilter: Remove '~/.beagle/publicfolders.cfg' file. Use 'beagle-config' instead to setup public folders.");		
				log.Info("ExternalAccessFilter: Replacing PublicFoldersList with new list from \"webservices.xml\"");
			}
			
			//Initialize();
		}
*/		
		public ArrayList Matchers {
		
			get { return matchers; } 
		}
				
		public void Initialize() {
		
			foreach (SimpleMatcher sm in matchers)			
				if (! sm.Match.StartsWith(FileUriPrefix)) {
					sm.Match = FileUriPrefix + sm.Match + "/";
					sm.Rewrite = HttpUriBase + sm.Rewrite + "/";
										
					Logger.Log.Debug("ExternalAccessFilter: Adding Match: " + sm.Match + "," + sm.Rewrite); 					
				}
		}

		private bool SetupFilters(ArrayList folders)
		{
			bool filterAdded = false;
			matchers = new ArrayList();			
			ArrayList suffixes = new ArrayList(); 
			
			//Populate reserved suffixes
			suffixes.AddRange(reserved_suffixes);
			
			//Check if 'public' folder exists and setup default mapping for it:
			if (Directory.Exists(PathFinder.HomeDir + "/public"))
			{				
				SimpleMatcher defaultMatcher = new SimpleMatcher();
				
				//file:///home/userid/public/
				defaultMatcher.Match = PathFinder.HomeDir + "/public"; 			
				//http://hostname:8888/beagle/public/
				defaultMatcher.Rewrite = "public";	
				
				matchers.Add(defaultMatcher);				
				suffixes.Add("public");
			}
            
            //string[] folders = entry.Split (',');
            foreach (string d in folders) {
                	    
                //Each entry must start with ~/            	            	
            	if ((d.Trim().Length > 1) && 
				(d.StartsWith("~/") || (d.StartsWith(PathFinder.HomeDir + "/"))) ) {
                	string d2;
			if (d.StartsWith("~/"))			
            		 	d2 = d.Replace("~/", PathFinder.HomeDir + "/");
			else
				d2 = d;							
                 			
            		if (!Directory.Exists(d2))
						continue;
                 			         			
            		string[] comp = d2.Split('/');
            		string leaf;
            		if (comp.Length > 1)
            			for (int li = comp.Length; li > 0; --li) {
            				if ((leaf = comp[li - 1].Trim()).Length > 0) {
            					//Check the leaf component is unique
            					if (suffixes.Contains(leaf))
            					{
            						Logger.Log.Warn("ExternalAccessFilter: Ignoring entry {0}. Reason: Entry suffix not unique", d);
            						break;
            					}
            					else
            						suffixes.Add(leaf);
            							
								filterAdded = true;
								SimpleMatcher matcher = new SimpleMatcher ();
										
								matcher.Match = d2;  
								matcher.Rewrite = leaf; 
								matchers.Add (matcher);
								break;										                															                															
            				} 
	      				}                 				
                } //end if
            } //end foreach
			return filterAdded;
		}
				
		//Returns: false, if Hit does not match any filter
		//		   true,  if Hit URI is part of any specified filter		
		public bool FilterHit (Hit hit)
		{
			if ((hit == null) || (matchers.Count == 0))
				return false;

			string uri = hit.UriAsString;			
			foreach (SimpleMatcher matcher in matchers)
			{
				if (uri.IndexOf (matcher.Match) == -1)
					continue;

				return hit.IsFile;   //return true;
			}

			return false;		
		}
		
		//Returns: null, if Hit does not match any filter
		//		   Uri,  after Translation
		public string TranslateHit (Hit hit)
		{
			if ((hit == null) || (matchers.Count == 0))
				return null;
							
			string uri = hit.UriAsString;			
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
	}	
	
	public class SimpleMatcher
	{
		public string Match;
		public string Rewrite;
	}	
}
