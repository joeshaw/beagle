//
// PixbufUtils.cs
//
// Authors:
//     Larry Ewing <lewing@novell.com>
//
//
// Copyright (C) 2004 - 2006 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.IO;

/**
  1        2       3      4         5            6           7          8

888888  888888      88  88      8888888888  88                  88  8888888888
88          88      88  88      88  88      88  88          88  88      88  88
8888      8888    8888  8888    88          8888888888  8888888888          88
88          88      88  88
88          88  888888  888888

t-l     t-r     b-r     b-l     l-t         r-t         r-b             l-b

**/

namespace Beagle.Util {
public enum PixbufOrientation {
	TopLeft = 1,
	TopRight = 2,
	BottomRight = 3,
	BottomLeft = 4,
	LeftTop = 5,
	RightTop = 6,
	RightBottom = 7,
	LeftBottom = 8
}

class PixbufUtils {
		
	static public PixbufOrientation Rotate270 (PixbufOrientation orientation)
	{
		PixbufOrientation [] rot = new PixbufOrientation [] {
			PixbufOrientation.LeftBottom, 
			PixbufOrientation.LeftTop,
			PixbufOrientation.RightTop,
			PixbufOrientation.RightBottom, 
			PixbufOrientation.BottomLeft,
			PixbufOrientation.TopLeft,
			PixbufOrientation.TopRight,
			PixbufOrientation.BottomRight
		};

		orientation = rot [((int)orientation) -1];
		return orientation;
	}

	static public PixbufOrientation Rotate90 (PixbufOrientation orientation)
	{
		orientation = Rotate270 (orientation);
		orientation = Rotate270 (orientation);
		orientation = Rotate270 (orientation);
		return orientation;
	}
}
}
