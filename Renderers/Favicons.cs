//
// Favicons.cs: Finds Epiphany favicons.
//
// Original Author:
//     Jonas Heylen <jonas.heylen@pandora.be> 
//
// Butched by Jon Trowbridge <trow@ximian.com>
//


using System;
using System.IO;
using System.Xml;
using System.Collections;

namespace Beagle {

	public class Favicons  {

		static XmlDocument         favicondoc;

		static DateTime lastRefresh;
		static DateTime timestamp;

		static private string GetPath (string fileName)
		{
			string home_dir = Environment.GetEnvironmentVariable ("HOME");
			string ephy_dir = Path.Combine (home_dir, ".gnome2/epiphany");
			return Path.Combine (ephy_dir, fileName);
		}
		
		static private bool Refresh ()
		{
			if ((DateTime.Now - lastRefresh).TotalMinutes < 10)
				return true;

			try {
				string path = GetPath ("ephy-favicon-cache.xml");

				if (! File.Exists (path))
					return false;

				DateTime lastWrite = File.GetLastWriteTime (path);
				if (timestamp >= lastWrite)
					return true;
				
				timestamp = lastWrite;

				favicondoc = new XmlDocument ();
				favicondoc.Load (path);

				return true;
			} catch {
				return false;
			}
		}

		static public string GetIconPath (string url)
		{
			Refresh ();

			if (favicondoc == null)
				return null;

			int index = url.IndexOf ("/", 7);
			if (index > -1)
			   url = url.Substring (0, index);

			string xpath = "descendant::node[starts-with(child::property[1]/child::text(), '" + url + "')]";
			XmlNode fav_node = favicondoc.SelectSingleNode (xpath);

			if (fav_node != null) {
				xpath = "child::property[position()=2]";
				XmlNode favicon = fav_node.SelectSingleNode (xpath);
				string path = GetPath ("favicon_cache");
				return Path.Combine (path, favicon.InnerText);
			}

			return null;
		}
	}
}


