/*
 * MonoTagEditor
 *
 * Copyright (C) 2003, Mariano Cano Pérez <mariano.cano@hispalinux.es>
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 *   1.Redistributions of source code must retain the above copyright notice, this
 *     list of conditions and the following disclaimer.
 *   2.Redistributions in binary form must reproduce the above copyright notice, this
 *     list of conditions and the following disclaimer in the documentation and/or other
 *     materials provided with the distribution.
 *   3.The name of the author may not be used to endorse or promote products derived
 *     from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE AUTHOR "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
 * LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE
 * USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using Gtk;
using GLib;
using System.Runtime.InteropServices;

namespace Beagle.Util {

public enum GnomeVFSMimeApplicationArgumentType
{
	Uris,
	Path,
	UrisForNonFiles
}

[StructLayout(LayoutKind.Sequential)]
public struct GnomeVFSMimeApplication
{
	public string id;
	public string name;
	public string command;
	public bool can_open_multiple_files;
	public GnomeVFSMimeApplicationArgumentType expects_uris;
	//public List supported_uri_schemes;
	private IntPtr supported_uri_schemes;
	public bool requires_terminal;
	
	public IntPtr reserved1;
	public IntPtr reserved2;

	public static GnomeVFSMimeApplication Zero = new GnomeVFSMimeApplication ();
	
	public static GnomeVFSMimeApplication New (IntPtr raw)
	{
		if(raw == IntPtr.Zero)
		{
			return GnomeVFSMimeApplication.Zero;
		}
		GnomeVFSMimeApplication self = new GnomeVFSMimeApplication();
		self = (GnomeVFSMimeApplication) Marshal.PtrToStructure (raw, self.GetType ());
		return self;
	}

	//Fixme: Create the supported uri schemes struct
	public List SupportedUriSchemes {
		get {
			List list = new List (supported_uri_schemes);
			return list;
		}
	}

	public static bool operator == (GnomeVFSMimeApplication a, GnomeVFSMimeApplication b)
	{
		return a.Equals (b);
	}

	public static bool operator != (GnomeVFSMimeApplication a, GnomeVFSMimeApplication b)
	{
		return ! a.Equals (b);
	}

	public override bool Equals (object o)
	{
		//if (!(o is GnomeVFSMimeApplication))
		//	 return false;
		return base.Equals(o)  ;
	}

	public override int GetHashCode ()
	{
		return base.GetHashCode ();
	}
}

public enum GnomeIconLookupFlags
{
	None,
	EmbeddingText,
	SmallImages
}

public enum GnomeIconLookupResultsFlags
{
	None,
	Thumbnail
}

public class GnomeIconLookup{

	[DllImport("libgnomeui-2")]
	static extern string gnome_icon_lookup (IntPtr icon_theme, 
						IntPtr thumbnail_factory,
						string uri,
						string custim_icon,
						IntPtr file_info,
						string mime_type,
						GnomeIconLookupFlags flags,
						ref GnomeIconLookupResultsFlags result);
	[DllImport("libgnomeui-2")]
	static extern string gnome_icon_theme_lookup_icon (IntPtr icon_theme,
						string icon_name,
						IconSize icon_size,
						ref IntPtr icon_data,
						ref int base_size);
	
	[DllImport("libgnomeui-2")]
	static extern void gnome_icon_data_free (IntPtr icon_data);
	
	[DllImport("libgnomeui-2")]
	static extern IntPtr gnome_icon_theme_new ();
	
	[DllImport("libgnomevfs-2")]
	static extern string gnome_vfs_mime_type_from_name (string mime_type);

	[DllImport("libgnomevfs-2")]
	static extern IntPtr gnome_vfs_application_registry_get_applications(string mime_type);
	
	[DllImport("libgnomevfs-2")]
	static extern IntPtr gnome_vfs_mime_get_default_application(string mime_type);
	
	public static string LookupMimeIcon(string mime,IconSize size)
	{
		string icon_name=String.Empty;
		string icon_path=String.Empty;
		GnomeIconLookupResultsFlags result=0;
		IntPtr icon_data=IntPtr.Zero;
		int base_size=0;
		IntPtr icon_theme = gnome_icon_theme_new ();
		
		if(icon_theme==IntPtr.Zero)
		{
			return String.Empty;
		}
		
		icon_name=gnome_icon_lookup(
			icon_theme,
			IntPtr.Zero,
			String.Empty,
			String.Empty,
			IntPtr.Zero,
			mime,
			GnomeIconLookupFlags.None, 
			ref result);
			
		if(icon_name.Length>0)
		{
			icon_path=gnome_icon_theme_lookup_icon (
				icon_theme,
				icon_name,
				size,
				ref icon_data,
				ref base_size);
			if(icon_data!=IntPtr.Zero)
				gnome_icon_data_free(icon_data);
		}	
		return icon_path;
	}
	
	public static string LookupFileIcon(string file,IconSize size)
	{
		string mime = gnome_vfs_mime_type_from_name(file);
		
		if(mime.Length>0)
			return LookupMimeIcon(mime,size);
		
		return String.Empty;
	}
	
	public static string GetMimeType(string file)
	{
		return gnome_vfs_mime_type_from_name(file);
	}
	
	public static GLib.List GetApplications(string mime_type)
	{
		IntPtr raw_ret = gnome_vfs_application_registry_get_applications(mime_type);
		GLib.List app = new GLib.List(raw_ret);
		return app;
	}
	
	public static GnomeVFSMimeApplication GetDefaultAction(string mime_type)
	{
		IntPtr ptr = gnome_vfs_mime_get_default_application(mime_type);
		GnomeVFSMimeApplication ret = GnomeVFSMimeApplication.New(ptr);
		return ret;
	}
}

}
