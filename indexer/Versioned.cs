//
// IVersionable.cs
//
// Copyright (C) 2004 Novell, Inc.
//

using System;

namespace Dewey {

	public class Versioned {

		protected DateTime timestamp = new DateTime (0);
		protected long revision = -1;

		public bool ValidTimestamp {
			get { return timestamp.Ticks > 0; }
		}

		public DateTime Timestamp {
			get { return timestamp; }
			set { timestamp = value; }
		}

		public bool ValidRevision {
			get { return revision >= 0; }
		}

		public long Revision {
			get { return revision; }
			set { revision = value; }
		}

		public bool IsObsoletedBy (DateTime timestamp)
		{
			return !ValidTimestamp || Timestamp < timestamp;
		}

		public bool IsObsoletedBy (long revNum)
		{
			return !ValidRevision || Revision < revNum;
		}

		public bool IsObsoletedBy (Versioned other)
		{
			// Anything with a valid timestamp always is
			// more recent than something w/o a timestamp.
			if (ValidTimestamp || other.ValidTimestamp) {
				if (other.ValidTimestamp)
					return IsObsoletedBy (other.Timestamp);
				else
					return false;
			}

			// Anything with a valid revision number is
			// more recent than something w/o a revision number.
			if (ValidRevision || other.ValidRevision) {
				if (other.ValidRevision)
					return IsObsoletedBy (other.Revision);
				else
					return false;
			}

			// FIXME: we should never reach this point

			return false;
		}

		public bool IsNewerThan (DateTime timestamp)
		{
			return ValidTimestamp && Timestamp > timestamp;
		}

		public bool IsNewerThan (long revNum)
		{
			return ValidRevision && Revision > revNum;
		}

		public bool IsNewerThan (Versioned other)
		{
			// Anything with a valid timestamp always is
			// more recent than something w/o a timestamp.
			if (ValidTimestamp || other.ValidTimestamp) {
				if (other.ValidTimestamp)
					return IsNewerThan (other.Timestamp);
				else
					return true;
			}

			// Anything with a valid revision number is
			// more recent than something w/o a revision number.
			if (ValidRevision || other.ValidRevision) {
				if (other.ValidRevision)
					return IsNewerThan (other.Revision);
				else
					return true;
			}

			// FIXME: we should never reach this point

			return false;
		}
	}
}
