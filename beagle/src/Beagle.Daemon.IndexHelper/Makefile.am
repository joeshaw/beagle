ASSEMBLY = Beagle.Daemon.IndexHelper
TARGET = exe
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -r:$(top_builddir)/bin/Beagle.Daemon.Plugins.dll -pkg:glib-sharp-2.0 -r:Mono.Posix

SOURCES = \
	Beagle.Daemon.IndexHelper/IndexHelper.cs		\
	Beagle.Daemon.IndexHelper/RemoteIndexerExecutor.cs

WRAPPER = beagled-index-helper

include $(top_srcdir)/build/build.mk
