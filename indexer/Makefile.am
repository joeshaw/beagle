
CSC = mcs -g

TARGET = Indexer.dll

CSFLAGS = -target:library

CSFILES = 				\
	$(srcdir)/PathFinder.cs		\
	$(srcdir)/Versioned.cs		\
	$(srcdir)/Indexable.cs		\
	$(srcdir)/IndexableFile.cs	\
	$(srcdir)/Query.cs		\
	$(srcdir)/Hit.cs		\
	$(srcdir)/Relevancy.cs		\
	$(srcdir)/IQueryResult.cs	\
	$(srcdir)/IQueryable.cs		\
	$(srcdir)/QueryResult.cs	\
	$(srcdir)/QueryDriver.cs	\
	$(srcdir)/IndexDriver.cs	\
	$(srcdir)/GoogleDriver.cs

ASSEMBLIES = 				\
	-r:Mono.Posix			\
	-r:ICSharpCode.SharpZipLib.dll	\
	-r:../Util/Util.dll		\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../Filters/Filters.dll

RESOURCES =


if ENABLE_EVO_SHARP
CSFLAGS += -define:ENABLE_EVO_SHARP
CSFILES += $(srcdir)/EvolutionDataServerDriver.cs
ASSEMBLIES += $(EVO_SHARP_LIBS)
endif



$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
