
CSC = mcs -debug

TARGET = Tiles.dll

CSFLAGS = -target:library -warn:4

if ENABLE_EVO_SHARP
CSFLAGS += -define:ENABLE_EVO_SHARP
endif

CSFILES =					\
	$(srcdir)/HitFlavor.cs			\
	$(srcdir)/HitToHitFlavor.cs		\
	$(srcdir)/SimpleRootTile.cs		\
	$(srcdir)/Template.cs                   \
	$(srcdir)/TileActionAttribute.cs        \
	$(srcdir)/Tile.cs			\
	$(srcdir)/TileBlog.cs			\
	$(srcdir)/TileBugzilla.cs		\
	$(srcdir)/TileCalendar.cs		\
	$(srcdir)/TileCanvas.cs			\
	$(srcdir)/TileContact.cs		\
	$(srcdir)/TileDocs.cs			\
	$(srcdir)/TileFile.cs			\
	$(srcdir)/TileFolder.cs			\
	$(srcdir)/TileFromHitTemplate.cs	\
	$(srcdir)/TileFromTemplate.cs		\
	$(srcdir)/TileGoogle.cs			\
	$(srcdir)/TileHitCollection.cs		\
	$(srcdir)/TileImLog.cs			\
	$(srcdir)/TileLauncher.cs		\
	$(srcdir)/TileMailMessage.cs		\
	$(srcdir)/TileMonodoc.cs		\
	$(srcdir)/TileMusic.cs			\
	$(srcdir)/TileNote.cs			\
	$(srcdir)/TilePicture.cs		\
	$(srcdir)/TilePresentation.cs		\
	$(srcdir)/TileRenderContext.cs		\
	$(srcdir)/TileSpreadsheet.cs		\
	$(srcdir)/TileStyleAttribute.cs         \
	$(srcdir)/TileWebHistory.cs


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

if ENABLE_WEBSERVICES
CSFILES += $(srcdir)/TileNetwork.cs
RESOURCEFILES += $(srcdir)/template-network.html
endif

# Makefile-fu to map /path/filename to -resource:/path/filename,filename
RESOURCES = $(join $(RESOURCEFILES:%=-resource:%,),$(notdir $(RESOURCEFILES)))

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCEFILES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =				\
	$(CSFILES)			\
	$(RESOURCEFILES)		\
	$(srcdir)/TileNetwork.cs	\
	$(srcdir)/template-network.html	\
	TranslationHack.cs

CLEANFILES =			\
	$(TARGET)		\
	$(TARGET).mdb
