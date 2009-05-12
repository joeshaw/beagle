ASSEMBLY = Beagle.Tools.ExtractContent
TARGET = exe
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:$(top_builddir)/bin/Beagle.Daemon.Plugins.dll -pkg:glib-sharp-2.0

SOURCES = \
	Beagle.Tools.ExtractContent/ExtractContent.cs

include $(top_srcdir)/build/build.mk
