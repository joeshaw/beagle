
CSC = mcs -g

TARGET = Tiles.dll

CSFLAGS = -target:library -warn:4

if ENABLE_EVO_SHARP
CSFLAGS += -define:ENABLE_EVO_SHARP
endif

CSFILES =					\
	$(srcdir)/SimpleRootTile.cs		\
	$(srcdir)/HitFlavor.cs			\
	$(srcdir)/HitToHitFlavor.cs		\
	$(srcdir)/Template.cs                   \
	$(srcdir)/TileActionAttribute.cs        \
	$(srcdir)/Tile.cs			\
	$(srcdir)/TileRenderContext.cs		\
	$(srcdir)/TileStyleAttribute.cs         \
	$(srcdir)/TileCanvas.cs			\
	$(srcdir)/TileHitCollection.cs		\
	$(srcdir)/TileFromTemplate.cs		\
	$(srcdir)/TileFromHitTemplate.cs	\
	$(srcdir)/TileContact.cs		\
	$(srcdir)/TileFile.cs			\
	$(srcdir)/TileFolder.cs			\
	$(srcdir)/TileGoogle.cs			\
	$(srcdir)/TileBugzilla.cs		\
	$(srcdir)/TileImLog.cs			\
	$(srcdir)/TileMailMessage.cs		\
	$(srcdir)/TileMusic.cs			\
	$(srcdir)/TileNote.cs			\
	$(srcdir)/TilePicture.cs		\
	$(srcdir)/TileWebHistory.cs		\
	$(srcdir)/TileBlog.cs			\
	$(srcdir)/TileMonodoc.cs		\
	$(srcdir)/TileDocs.cs			\
	$(srcdir)/TilePresentation.cs		\
	$(srcdir)/TileLauncher.cs		\
	$(srcdir)/TileCalendar.cs		\
	$(srcdir)/TileSpreadsheet.cs

LOCAL_ASSEMBLIES =				\
	../Util/Util.dll			\
	../images/Images.dll			\
	../BeagleClient/Beagle.dll

ASSEMBLIES = 					\
	$(EVO_SHARP_LIBS)			\
	$(BEAGLE_UI_LIBS)			\
	$(LOCAL_ASSEMBLIES:%=-r:%)		\
	-r:Mono.Posix

RESOURCEFILES =					\
	$(srcdir)/template-page.css		\
	$(srcdir)/template-head.html            \
	$(srcdir)/template-foot.html            \
	$(srcdir)/template-contact.html		\
	$(srcdir)/template-file.html		\
	$(srcdir)/template-folder.html		\
	$(srcdir)/template-google.html		\
	$(srcdir)/template-bugzilla.html	\
	$(srcdir)/template-im-log.html		\
	$(srcdir)/template-mail-message.html	\
	$(srcdir)/template-music.html		\
	$(srcdir)/template-note.html		\
	$(srcdir)/template-picture.html		\
	$(srcdir)/template-web-history.html	\
	$(srcdir)/template-blog.html		\
	$(srcdir)/template-monodoc.html		\
	$(srcdir)/template-docs.html		\
	$(srcdir)/template-presentation.html	\
	$(srcdir)/template-launcher.html	\
	$(srcdir)/template-calendar.html	\
	$(srcdir)/template-spreadsheet.html


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
	$(RESOURCEFILES)	\
	TranslationHack.cs

CLEANFILES =			\
	$(TARGET)		\
	$(TARGET).mdb
