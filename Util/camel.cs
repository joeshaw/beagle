//
// camel.cs: Parser for Evolution mbox summary files.
//
// Authors:
//    Miguel de Icaza <miguel@ximian.com>
//
//    Imap support by Erik Bågfors <erik@bagfors.nu>
//

using System.IO;
using System;
using System.Globalization;
using System.Text;

namespace Beagle.Util {
namespace Camel {
        public class Summary { 
	    public SummaryHeader header;
	    public MessageInfo [] messages;

	    public static Summary load (string file) 
	    {
		Summary s;
		if (file.EndsWith ("/summary")) { 
		    s = new ImapSummary (file);
		} else {
		    s = new MBoxSummary (file);
		}
		return s;
	    }
	}

	public class MBoxSummary: Summary {
		public MBoxSummary (string file)
		{
			using (FileStream f = File.OpenRead (file)){
				header = new MBoxSummaryHeader (f);

				messages = new MessageInfo [header.count];
				
				for (int i = 0; i < header.count; i++){
					messages [i] = new MBoxMessageInfo (f);
				}
			}
		}
	}

	public class ImapSummary: Summary {
		public ImapSummary (string file)
		{
			using (FileStream f = File.OpenRead (file)){
				header = new ImapSummaryHeader (f);

				messages = new MessageInfo [header.count];
				
				for (int i = 0; i < header.count; i++){
					messages [i] = new ImapMessageInfo (f);
				}
			}
		}
	}

	public class MessageInfo {
		public string uid, subject, from, to, cc, mlist;
		public uint size, flags;
		public DateTime sent, received;
		public int x, y;

		private void SkipContentInfo (FileStream f)
		{
			Decode.Token (f); // type
			Decode.Token (f); // subtype
			uint count = Decode.UInt (f); // count
			for (int i = 0; i < count; ++i) {
				Decode.Token (f); // name
				Decode.Token (f); // value
			}
			Decode.Token (f); // id
			Decode.Token (f); // description
			Decode.Token (f); // encoding
			Decode.UInt (f); // size

			count = Decode.UInt (f); // child count
			for (int i = 0; i < count; ++i) // recursively skip children
				SkipContentInfo (f);
		}
		
		public MessageInfo (FileStream f)
		{
			uid      = Decode.String (f);
			flags    = Decode.UInt (f);
			size     = Decode.UInt (f);
			sent     = Decode.Time (f);
			received = Decode.Time (f);
			subject  = Decode.String (f);
			from     = Decode.String (f);
			to       = Decode.String (f);
			cc       = Decode.String (f);
			mlist    = Decode.String (f);

			Decode.FixedInt (f);
			Decode.FixedInt (f);

			uint count;

			// references
			count = Decode.UInt (f);
			if (count > 0) {
				for (int i = 0; i < count; i++) {
					Decode.FixedInt (f);
					Decode.FixedInt (f);
				}
			}

			// user flags
			count = Decode.UInt (f);
			if (count > 0) {
				for (int i = 0; i < count; i++) {
					Decode.String (f);
				}
			}

			// user tags
			count = Decode.UInt (f);
			if (count > 0){
				for (int i = 0; i < count; i++){
					Decode.String (f);
					Decode.String (f);
				}
			}

			// FIXME: How do we know if there is content info in there?
			// SkipContentInfo (f);
		}

		public override string ToString ()
		{
			return String.Format ("From: {0}\nTo: {1}\nSubject: {2}\nUID: {3}\n", from, to, subject, uid);
		}

		public DateTime Date {
			get { return received.Ticks != 0 ? received : sent; }
		}
	}

	public class MBoxMessageInfo : MessageInfo {
		public uint from_pos;
		
		public MBoxMessageInfo (FileStream f) : base (f)
		{
			from_pos = Decode.Offset (f);
		}

		public override string ToString ()
		{
			return String.Format ("From: {0}\nTo: {1}\nSubject: {2}\nPos: {3} Size: {4}\n", from, to, subject,
					      from_pos, size);
		}
	}



	public class ImapMessageInfo : MessageInfo {
		public uint server_flags;
		
		public ImapMessageInfo (FileStream f) : base (f)
		{
			server_flags = Decode.UInt (f);
			PerformContentInfoLoad (f);
		}

		public override string ToString ()
		{
			return String.Format ("From: {0}\nTo: {1}\nSubject: {2}\nSize: {3}\n", from, to, subject, size);
		}

		private bool PerformContentInfoLoad (FileStream f)
		{
		    bool ci = ContentInfoLoad (f);
		    if (!ci) 
			return false;

		    uint count = Decode.UInt (f);
		    if (count == -1 || count > 500) {
			return false;
		    }
		    
		    for (int i = 0; i < count; i++) {

			bool part = PerformContentInfoLoad (f);
			if (!part) 
			    throw new Exception ();
		    }
		    return true;
		}

		private bool ContentInfoLoad (FileStream f)
		{
		    string token;

		    if (f.ReadByte () == 0) 
			return true;

		    // type
		    token = Decode.Token (f);
		    // subtype
		    token = Decode.Token (f);

		    uint count;
		    count = Decode.UInt (f);
		    if (count == -1 || count > 500)
			return false;
		    for (int i = 0; i < count; i++) {
			// Name
			token = Decode.Token (f);
			// Value
			token = Decode.Token (f);
		    }
		    
		    // id
		    token = Decode.Token (f);

		    // description
		    token = Decode.Token (f);

		    // encoding
		    token = Decode.Token (f);

		    // size
		    Decode.UInt (f);
		    return true;
		}
	}
	
	public class SummaryHeader {
		public int      version;
		public int      flags;
		public int      nextuid;
		public DateTime time;
		public int      count;
		public int      unread;
		public int      deleted;
		public int      junk;
		
		public SummaryHeader (FileStream f)
		{
			version = Decode.FixedInt (f);
			flags   = Decode.FixedInt (f);
			nextuid = Decode.FixedInt (f);
			time    = Decode.Time (f);
			count   = Decode.FixedInt (f);
			unread  = Decode.FixedInt (f);
			deleted = Decode.FixedInt (f);
			junk    = Decode.FixedInt (f);

			//Console.WriteLine ("V={0} time={1}, count={2} unread={3} deleted={4} junk={5}", version, time, count, unread, deleted, junk);
		}
	}

	public class MBoxSummaryHeader : SummaryHeader {
		public int local_version;
		public int mbox_version;
		public int folder_size;
		
		public MBoxSummaryHeader (FileStream f) : base (f)
		{
			local_version = Decode.FixedInt (f);
			mbox_version = Decode.FixedInt (f);
			folder_size = Decode.FixedInt (f);
		}
	}

	public class ImapSummaryHeader : SummaryHeader {
		
		public ImapSummaryHeader (FileStream f) : base (f)
		{
			int version = Decode.FixedInt (f);
			int validity = Decode.FixedInt (f);
		}
	}
	
	public class Decode {
		static Encoding e = Encoding.UTF8;
		static long UnixBaseTicks;

		static Decode ()
		{
			UnixBaseTicks = new DateTime (1970, 1, 1, 0, 0, 0).Ticks;
		}

		public static string Token (FileStream f) 
		{
		    int len = (int) UInt (f);
		    if (len < 32) {
			if (len <= 0) 
			    return "NULL"; 
			
			// Ok, this is a token from the list, we can ignore it
			return "token_from_list";
		    } else if (len > 10240) {
			throw new Exception ();
		    } else {
			len -= 32;
			byte [] buffer = new byte [len];
			f.Read (buffer, 0, (int) len);
			return new System.String (e.GetChars (buffer, 0, len));
		    }
		}
	       
		public static string String (FileStream f)
		{
			int len = (int) UInt (f);
			len--;

			if (len > 65535)
				throw new Exception ();
			byte [] buffer = new byte [len];
			f.Read (buffer, 0, (int) len);
			return new System.String (e.GetChars (buffer, 0, len));
		}

		public static uint UInt (FileStream f)
		{
			uint value = 0;
			int v;
			
			while (((v = f.ReadByte ()) & 0x80) == 0 && v != -1){
				value |= (byte) v;
				value <<= 7;
			}
			return value | ((byte)(v & 0x7f));
		}
		
		public static int FixedInt (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);

			return (b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3];
		}

		public static DateTime Time (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);
			long seconds = (b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3];

			if (seconds == 0)
				return new DateTime (0);

			return new DateTime (UnixBaseTicks).AddSeconds (seconds);
		}

		public static uint Offset (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);

			return (uint)((b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3]);
		}
	}

	class Test {
		void Main (string [] args)
		{
			string file;
			
			if (args.Length == 0)
				file = "./summary";
			else
				file = args [0];
			
			Summary s = Summary.load (file);
			for (int i = 0; i < s.header.count; i++) {
			    Console.WriteLine(s.messages [i]);
			}
			
		}
		
	}
}
}
