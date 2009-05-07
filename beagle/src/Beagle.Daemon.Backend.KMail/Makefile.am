ASSEMBLY = Beagle.Daemon.Backend.KMail
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -pkg:gmime-sharp -pkg:glib-sharp-2.0 -r:Mono.Posix

SOURCES = \
	Beagle.Daemon.Backend.KMail/KMailIndexableGenerator.cs	\
	Beagle.Daemon.Backend.KMail/KMailIndexer.cs		\
	Beagle.Daemon.Backend.KMail/KMailQueryable.cs

include $(top_srcdir)/build/build.mk
