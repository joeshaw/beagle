//
// AssemblyInfo.cs
//
// Copyright (C) 2006 Novell, Inc.
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

using System.Reflection;

using Beagle;
using Beagle.Daemon;

// Any request message types in the BeagleDaemonLib.dll file must be registered
// in these two attributes.
[assembly: RequestMessageTypes (
	typeof (RemoteIndexerRequest)
)]
	 
[assembly: ResponseMessageTypes (
	typeof (RemoteIndexerResponse)
)]

// Executors go in here.
[assembly: RequestMessageExecutorTypes (
	typeof (DaemonInformationExecutor),
	typeof (InformationalMessagesRequestExecutor),
	typeof (OptimizeIndexesExecutor),
	typeof (ReloadConfigExecutor),
	typeof (ShutdownExecutor),
	typeof (SnippetExecutor),
	typeof (QueryExecutor)
)]

// All backends in this assembly must be registered here.
[assembly: IQueryableTypes (
	typeof (Beagle.Daemon.KMailQueryable.KMailQueryable),
	typeof (Beagle.Daemon.FileSystemQueryable.FileSystemQueryable),
	typeof (Beagle.Daemon.GaimLogQueryable.GaimLogQueryable),
	typeof (Beagle.Daemon.IndexingServiceQueryable.IndexingServiceQueryable),
	typeof (Beagle.Daemon.TomboyQueryable.TomboyQueryable),
	typeof (Beagle.Daemon.LabyrinthQueryable.LabyrinthQueryable),
	typeof (Beagle.Daemon.BlamQueryable.BlamQueryable),
	typeof (Beagle.Daemon.LifereaQueryable.LifereaQueryable),
	typeof (Beagle.Daemon.AkregatorQueryable.AkregatorQueryable),
	typeof (Beagle.Daemon.KonqQueryable.KonqQueryable),
	typeof (Beagle.Daemon.KBookmarkQueryable.KonqBookmarkQueryable),
	typeof (Beagle.Daemon.KNotesQueryable.KNotesQueryable),
	typeof (Beagle.Daemon.KOrganizerQueryable.KOrganizerQueryable),
	typeof (Beagle.Daemon.KabcQueryable.KabcQueryable),
	typeof (Beagle.Daemon.KopeteQueryable.KopeteQueryable),
	typeof (Beagle.Daemon.StaticQueryable)
)]
