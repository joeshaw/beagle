ASSEMBLY = Beagle.Daemon.Backend.Opera
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:$(top_builddir)/bin/Beagle.Daemon.Backend.FileSystem.dll -r:ICSharpCode.SharpZipLib -pkg:glib-sharp-2.0

SOURCES = \
	Beagle.Daemon.Backend.Opera/OperaIndexableGenerator.cs	\
	Beagle.Daemon.Backend.Opera/OperaIndexer.cs		\
	Beagle.Daemon.Backend.Opera/OperaQueryable.cs

include $(top_srcdir)/build/build.mk
