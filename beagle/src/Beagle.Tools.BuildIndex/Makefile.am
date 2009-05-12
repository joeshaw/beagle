ASSEMBLY = Beagle.Tools.BuildIndex
TARGET = exe
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:$(top_builddir)/bin/Beagle.Daemon.Plugins.dll -r:$(top_builddir)/bin/Beagle.Daemon.Backend.FileSystem.dll -pkg:glib-sharp-2.0 -r:Mono.Posix

SOURCES = \
	Beagle.Tools.BuildIndex/BuildIndex.cs

include $(top_srcdir)/build/build.mk
