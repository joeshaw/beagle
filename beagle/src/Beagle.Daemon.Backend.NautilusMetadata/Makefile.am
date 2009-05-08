ASSEMBLY = Beagle.Daemon.Backend.NautilusMetadata
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:$(top_builddir)/bin/Beagle.Daemon.Backend.FileSystem.dll -pkg:glib-sharp-2.0

SOURCES = \
	Beagle.Daemon.Backend.NautilusMetadata/NautilusMetadataQueryable.cs

include $(top_srcdir)/build/build.mk
