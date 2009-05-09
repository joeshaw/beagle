ASSEMBLY = Beagle.Daemon.Backend.EvolutionDataServer
TARGET = library
LINK = -r:$(top_builddir)/bin/Beagle.Util.dll -r:$(top_builddir)/bin/Beagle.dll -r:$(top_builddir)/bin/Beagle.Daemon.dll -pkg:evolution-sharp -pkg:glib-sharp-2.0

EDS_DIR = Beagle.Daemon.Backend.EvolutionDataServer

SOURCES = \
	$(EDS_DIR)/BookContainer.cs			\
	$(EDS_DIR)/CalContainer.cs			\
	$(EDS_DIR)/Container.cs				\
	$(EDS_DIR)/EvolutionDataServerQueryable.cs	\
	$(EDS_DIR)/SchedulingIndexableGenerator.cs	\
	$(EDS_DIR)/SourcesHandler.cs

include $(top_srcdir)/build/build.mk
