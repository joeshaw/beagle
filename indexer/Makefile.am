
CSC = mcs -g

TARGET = Indexer.dll

CSFLAGS = 					\
	-target:library				\
	-define:PREFIX=$(prefix)		\
	-define:PKGLIBDIR=$(pkglibdir)

CSFILES = 				\
	$(srcdir)/PathFinder.cs		\
	$(srcdir)/Property.cs		\
	$(srcdir)/Flavor.cs		\
	$(srcdir)/Filter.cs		\
	$(srcdir)/Versioned.cs		\
	$(srcdir)/Indexable.cs		\
	$(srcdir)/IndexableFile.cs	\
	$(srcdir)/Query.cs		\
	$(srcdir)/Hit.cs		\
	$(srcdir)/Relevancy.cs		\
	$(srcdir)/IQueryResult.cs	\
	$(srcdir)/IQueryable.cs		\
	$(srcdir)/QueryableFlavor.cs	\
	$(srcdir)/QueryResult.cs	\
	$(srcdir)/QueryDriver.cs	\
	$(srcdir)/IndexDriver.cs	\
	$(srcdir)/GoogleDriver.cs


LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../Lucene.Net/Lucene.Net.dll


ASSEMBLIES = 				\
	-r:Mono.Posix			\
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =


if ENABLE_EVO_SHARP
CSFLAGS += -define:ENABLE_EVO_SHARP
CSFILES += $(srcdir)/EvolutionDataServerDriver.cs
ASSEMBLIES += $(EVO_SHARP_LIBS)
endif

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in

$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"	\
		< $^ > $@

$(TARGET): $(CSFILES) $(EXTSTR) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(EXTSTR) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)	\
	$(EXTSTR_IN)

CLEANFILES =		\
	$(TARGET)	\
	$(EXTSTR)
