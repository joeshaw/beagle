ASSEMBLY = Beagle.Daemon.Backend.Kopete
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -pkg:glib-sharp-2.0

SOURCES = \
	Beagle.Daemon.Backend.Kopete/KopeteCrawler.cs	\
	Beagle.Daemon.Backend.Kopete/KopeteQueryable.cs

include $(top_srcdir)/build/build.mk
