
CSC = mcs -g

TARGET = Indexer.dll

CSFLAGS = -target:library

CSFILES = 				\
	$(srcdir)/PathFinder.cs		\
	$(srcdir)/Versioned.cs		\
	$(srcdir)/Indexable.cs		\
	$(srcdir)/IndexableFile.cs	\
	$(srcdir)/IndexableHttp.cs	\
	$(srcdir)/Query.cs		\
	$(srcdir)/Hit.cs		\
	$(srcdir)/QueryResult.cs	\
	$(srcdir)/IQueryable.cs		\
	$(srcdir)/QueryDriver.cs	\
	$(srcdir)/IndexDriver.cs	\
	$(srcdir)/GoogleDriver.cs

ASSEMBLIES = 				\
	-r:ICSharpCode.SharpZipLib	\
	-r:../Util/Util.dll		\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../Filters/Filters.dll

$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

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
