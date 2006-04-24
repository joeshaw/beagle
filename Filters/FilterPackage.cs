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
			SnippetMode = true;
		}

		protected virtual void PullPackageProperties () { }

		private string package_name, package_version, category;
		private string homepage, summary, packager_name, packager_email, size;

		/* Some of the metadata common in all packages.
		 * Use them to display general package information in beagle frontends.
		 */

		// Name of the package
		protected string PackageName  {
			get { return package_name; }
			set { package_name = value; }
		}

		// Version
		protected string PackageVersion {
			get { return package_version; }
			set { package_version = value; }
		}

		// A short summary. Some packages might not have this.
		// The longer description stored as AppendText. When you request snippet, it is fetched from the description.
		// It is not possible to retrieve the whole of description from frontends. Use summary for a short description.
		protected string Summary {
			get { return summary; }
			set { summary = value; }
		}

		// Category/section to which the package might belong. Not all packages might have this.
		protected string Category {
			get { return category; }
			set { category = value; }
		}

		/* Use either the homepage or packager to provide a external link for more information
		 * Not all packages have both set; however most have at least one
		 */

		// Homepage of the package
		protected string Homepage {
			get { return homepage; }
			set { homepage = value; }
		}

		// Packager.
		protected string PackagerName {
			get { return packager_name; }
			set { packager_name = value; }
		}

		protected string PackagerEmail {
			get { return packager_email; }
			set { packager_email = value; }
		}

		// Size of the package - in bytes.
		// Depending on package, its either the installed size or the size of the package.
		protected string Size {
			get { return size; }
			set { size = value; }
		}

		protected override void DoPullProperties ()
		{
			PullPackageProperties ();

			AddProperty (Beagle.Property.New ("dc:title", package_name));
			AddProperty (Beagle.Property.NewKeyword ("fixme:version", package_version));
			AddProperty (Beagle.Property.New ("dc:subject", summary));
			AddProperty (Beagle.Property.New ("fixme:category", category));
			AddProperty (Beagle.Property.NewUnsearched ("dc:source", homepage));
			AddProperty (Beagle.Property.New ("fixme:packager_name", packager_name));
			AddProperty (Beagle.Property.New ("fixme:packager_email", packager_email));
			AddProperty (Beagle.Property.NewUnsearched ("fixme:size", size));

			Finished ();
		}

	}
}
