
CSC = mcs -g

TARGET = Indexer.dll

CSFLAGS = -target:library

CSFILES = 			\
	Versioned.cs		\
	Indexable.cs		\
	IndexableFile.cs	\
	IndexableHttp.cs	\
	Query.cs		\
	Hit.cs			\
	IndexDriver.cs

ASSEMBLIES = 				\
	-r:ICSharpCode.SharpZipLib	\
	-L ../Lucene.Net		\
	-r:Lucene.Net			\
	-L ../Filters			\
	-r:Filters

$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
