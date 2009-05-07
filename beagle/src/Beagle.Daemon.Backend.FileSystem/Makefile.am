ASSEMBLY = Beagle.Daemon.Backend.FileSystem
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:Mono.Posix

SOURCES = \
	Beagle.Daemon.Backend.FileSystem/DirectoryIndexableGenerator.cs	\
	Beagle.Daemon.Backend.FileSystem/DirectoryModel.cs		\
	Beagle.Daemon.Backend.FileSystem/DirectoryState.cs		\
	Beagle.Daemon.Backend.FileSystem/FileCrawlTask.cs		\
	Beagle.Daemon.Backend.FileSystem/FileNameFilter.cs		\
	Beagle.Daemon.Backend.FileSystem/FileSystemQueryable.cs		\
	Beagle.Daemon.Backend.FileSystem/IFileEventBackend.cs		\
	Beagle.Daemon.Backend.FileSystem/InotifyBackend.cs		\
	Beagle.Daemon.Backend.FileSystem/InternalUriManager.cs		\
	Beagle.Daemon.Backend.FileSystem/NullFileEventBackend.cs	\
	Beagle.Daemon.Backend.FileSystem/TreeCrawlTask.cs		\
	Beagle.Daemon.Backend.FileSystem/XmpSidecarStore.cs

include $(top_srcdir)/build/build.mk
