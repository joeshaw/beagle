//
// Relevancy.cs
//
// Copyright (C) 2004 Novell, Inc.
//

//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//

using System;
using System.Collections;
using System.Reflection;

namespace Beagle {

	public abstract class RelevancyRule {

		public virtual bool IsApplicable (Hit hit)
		{
			return true;
		}

		public abstract double GetMultiplier (Hit hit);
	}

	// Meta-FIXME: These rules are very ad-hoc.

	public class RelevancyRule_FileExists : RelevancyRule {

		public override bool IsApplicable (Hit hit)
		{
			return hit.IsFile;
		}

		public override double GetMultiplier (Hit hit)
		{
			// Filter out non-existent files.
			return hit.FileInfo.Exists ? 1.0 : 0.0;
		}
	}

	public class RelevancyRule_FileAccessTime : RelevancyRule {

		public override bool IsApplicable (Hit hit)
		{
			return hit.IsFile;
		}

		public override double GetMultiplier (Hit hit)
		{
			double days = (DateTime.Now - hit.FileInfo.LastAccessTime).TotalDays;
			if (days < 0)
				return 1.0;
			// Relevancy half-life is three months.
			return Math.Pow (0.5, days / 91.0);
		}
	}

	public class RelevancyRule_FileWriteTime : RelevancyRule {
		
		public override bool IsApplicable (Hit hit)
		{
			return hit.IsFile;
		}

		public override double GetMultiplier (Hit hit)
		{
			double days = (DateTime.Now - hit.FileInfo.LastWriteTime).TotalDays;
			// Boost relevancy if the file has been touched within the last seven days.
			if (0 <= days && days < 7)
				return 1.2;
			return 1.0;
		}
	}
	
	public class Relevancy {

		static ArrayList rules = null;
		
		static void Initialize ()
		{
			if (rules != null)
				return;

			rules = new ArrayList ();
			
			Assembly a = Assembly.GetExecutingAssembly ();
			foreach (Type t in a.GetTypes ()) {
				if (t.IsSubclassOf (typeof (RelevancyRule))) {
					RelevancyRule rule = (RelevancyRule) Activator.CreateInstance (t);
					rules.Add (rule);
				}
			}
		}

		static public bool AdjustScore (Hit hit)
		{
			const double EPSILON = 1e-8;
			double multiplier = 1.0;

			Initialize ();
			foreach (RelevancyRule rule in rules)
				if (rule.IsApplicable (hit)) {
					double m = rule.GetMultiplier (hit);
					if (m < EPSILON)
						return false;
					multiplier *= m;
				}
			hit.ScoreMultiplier = (float) multiplier;
			return hit.Score > EPSILON;
		}
	}

}
