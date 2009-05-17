ASSEMBLY = Beagle.Daemon.Core
TARGET = exe
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -pkg:glib-sharp-2.0 -r:Mono.Posix

SOURCES = \
	Beagle.Daemon.Core/AssemblyInfo.cs	\
	Beagle.Daemon.Core/BeagleDaemon.cs	\
	Beagle.Daemon.Core/RemoteIndexer.cs

include $(top_srcdir)/build/build.mk

EXTRA_BUNDLE = beagled
bin_SCRIPTS = beagled
