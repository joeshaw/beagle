
CSC = mcs -g

TARGET = Tiles.dll

CSFLAGS = -target:library

CSFILES =					\
	$(srcdir)/HitFlavor.cs			\
	$(srcdir)/HitToHitFlavor.cs		\
	$(srcdir)/Tile.cs			\
	$(srcdir)/TileMenuContext.cs		\
	$(srcdir)/TileRenderContext.cs		\
	$(srcdir)/TileCanvas.cs			\
	$(srcdir)/TileHitCollection.cs		\
	$(srcdir)/TileFromTemplate.cs		\
	$(srcdir)/TileContact.cs		\
	$(srcdir)/TileFile.cs			\
	$(srcdir)/TileFolder.cs			\
	$(srcdir)/TileGoogle.cs			\
	$(srcdir)/TileImLog.cs			\
	$(srcdir)/TileMailMessage.cs		\
	$(srcdir)/TileMusic.cs			\
	$(srcdir)/TilePicture.cs		\
	$(srcdir)/TileWebHistory.cs

LOCAL_ASSEMBLIES =				\
	../Util/Util.dll			\
	../images/Images.dll			\
	../BeagleClient/Beagle.dll

ASSEMBLIES = 					\
	$(EVO_SHARP_LIBS)			\
	$(BEAGLE_UI_LIBS)			\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCEFILES =					\
	$(srcdir)/template-hit-collection.html	\
	$(srcdir)/template-contact.html		\
	$(srcdir)/template-file.html		\
	$(srcdir)/template-folder.html		\
	$(srcdir)/template-google.html		\
	$(srcdir)/template-im-log.html		\
	$(srcdir)/template-mail-message.html	\
	$(srcdir)/template-music.html		\
	$(srcdir)/template-picture.html		\
	$(srcdir)/template-web-history.html

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCEFILES)
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
