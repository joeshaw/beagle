//
// FilterPackage.cs
//
// Copyright (C) 2006 Debajyoti Bera <dbera.web@gmail.com>
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

using Beagle.Util;

namespace Beagle.Filters {

	public abstract class FilterPackage : Beagle.Daemon.Filter {

		public FilterPackage ()
		{
		}

		protected virtual void PullPackageProperties () { }

		private string package_name, package_version, description, license, homepage, summary;

		protected string PackageName  {
			get { return package_name; }
			set { package_name = value; }
		}

		protected string PackageVersion {
			get { return package_version; }
			set { package_version = value; }
		}

		protected string Description {
			get { return description; }
			set { description = value; }
		}

		protected string License {
			get { return license; }
			set { license = value; }
		}

		protected string Homepage {
			get { return homepage; }
			set { homepage = value; }
		}

		protected string Summary {
			get { return summary; }
			set { summary = value; }
		}

		protected override void DoPullProperties ()
		{
			PullPackageProperties ();

			AddProperty (Beagle.Property.New ("dc:title", package_name));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:version", package_version));
			AddProperty (Beagle.Property.New ("dc:description", description));
			AddProperty (Beagle.Property.New ("dc:rights", license));
			AddProperty (Beagle.Property.NewUnsearched ("dc:source", homepage));
			AddProperty (Beagle.Property.New ("dc:subject", summary));

			Finished ();
		}

	}
}
