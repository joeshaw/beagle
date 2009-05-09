ASSEMBLY = Beagle.Daemon.Backend.EvolutionMail
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -pkg:gmime-sharp -pkg:gconf-sharp-2.0 -pkg:glib-sharp-2.0 -r:Mono.Data.Sqlite -r:Mono.Posix

SOURCES = \
	Beagle.Daemon.Backend.EvolutionMail/EvolutionMailQueryable.cs	\
	Beagle.Daemon.Backend.EvolutionMail/EvolutionMailIndexableGenerator.cs \
	Beagle.Daemon.Backend.EvolutionMail/EvolutionSummaryTracker.cs	\
	Beagle.Daemon.Backend.EvolutionMail/GConfThreadHelper.cs	\
	Beagle.Daemon.Backend.EvolutionMail/MailCrawler.cs

include $(top_srcdir)/build/build.mk
