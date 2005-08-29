using System;

namespace Entagged.Audioformats.Util {
	
	[AttributeUsage (AttributeTargets.Class, AllowMultiple=true)]
	public class SupportedMimeType : Attribute {
		private string mime_type;
		public string MimeType {
			get { return mime_type; }
		}

		public SupportedMimeType (string mime_type)
		{
			this.mime_type = mime_type;
		}
	}

}

