
CSC = mcs -g

TARGET = Best.exe
WRAPPER = best

CSFLAGS = -target:exe

CSFILES =				\
	$(srcdir)/Best.cs		\
	$(srcdir)/BestWindow.cs		\
	$(srcdir)/BestRootTile.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../images/Images.dll		\
	../Lucene.Net/Lucene.Net.dll	\
	../Filters/Filters.dll		\
	../indexer/Indexer.dll		\
	../Tiles/Tiles.dll

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" < $^ > $@
	chmod +x $(WRAPPER)

EXTRA_DIST =		\
	$(CSFILES)	\
	$(WRAPPER).in

CLEANFILES =		\
	$(TARGET)	\
	$(WRAPPER)
