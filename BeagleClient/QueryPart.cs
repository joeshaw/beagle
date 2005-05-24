//
// QueryPart.cs
//
// Copyright (C) 2005 Novell, Inc.
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
using System.Xml.Serialization;

using BU = Beagle.Util;

namespace Beagle {

	public class QueryPart {

		public const string TargetAll = "_all";
		public const string TargetText = "_text";
		public const string TargetProperties = "_prop";

		private string target;
		private string text;
		private bool is_keyword;
		private bool is_required;
		private bool is_prohibited;

		public QueryPart ()
		{ }

		public string Target {
			get { return target; }
			set { target = value; }
		}

		public string Text {
			get { return text; }
			set { text = value; }
		}

		public bool IsKeyword {
			get { return is_keyword; }
			set { is_keyword = value; }
		}

		public bool IsRequired {
			get { return is_required; }
			set { is_required = value; }
		}

		public bool IsProhibited {
			get { return is_prohibited; }
			set { is_prohibited = value; }
		}

		[XmlIgnore]
		public bool TargetIsAll {
			get { return target == TargetAll; }
		}

		[XmlIgnore]
		public bool TargetIsText {
			get { return target == TargetText; }
		}

		[XmlIgnore]
		public bool TargetIsProperties {
			get { return target == TargetProperties; }
		}

		[XmlIgnore]
		public bool TargetIsSpecificProperty {
			get { return target != TargetAll && target != TargetText && target != TargetProperties; }
		}
	}
}
