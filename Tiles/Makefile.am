
CSC = mcs -g

TARGET = Tiles.dll

CSFLAGS = -target:library

CSFILES =					\
	$(srcdir)/Tile.cs			\
	$(srcdir)/TileRenderContext.cs		\
	$(srcdir)/TileCanvas.cs			\
	$(srcdir)/TileHitCollection.cs		\
	$(srcdir)/TileFromTemplate.cs		\
	$(srcdir)/TileFile.cs			\
	$(srcdir)/TileMailMessage.cs

ASSEMBLIES = 					\
	$(EVO_SHARP_LIBS)			\
	$(BEAGLE_UI_LIBS)			\
	-r:../Util/Util.dll			\
	-r:../images/Images.dll			\
	-r:../Lucene.Net/Lucene.Net.dll		\
	-r:../Filters/Filters.dll		\
	-r:../indexer/Indexer.dll

RESOURCEFILES =					\
	$(srcdir)/template-file.html		\
	$(srcdir)/template-mail-message.html

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(RESOURCEFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =			\
	$(CSFILES)		\
	$(RESOURCEFILES)

CLEANFILES =			\
	$(TARGET)
