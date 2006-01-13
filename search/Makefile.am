CSC = mcs -debug -nowarn:0618
CSFLAGS = -target:exe

bin_SCRIPTS = beagle-search
pkglib_DATA = Search.exe

beagle-search: beagle-search.in
	sed -e "s:@PKGLIBDIR@:${pkglibdir}:" < $(srcdir)/beagle-search.in > beagle-search
	chmod a+x beagle-search

CSFILES =				\
	Category.cs			\
	ConversationCategory.cs		\
	Entry.cs			\
	GroupView.cs			\
	Panes.cs			\
	Search.cs			\
	SortedTileList.cs		\
	Spinner.cs			\
	TileCategory.cs			\
	TypeFilter.cs			\
	UIManager.cs

TILES = 				\
	Tiles/ActionMenuItem.cs		\
	Tiles/Application.cs		\
	Tiles/Audio.cs			\
	Tiles/CApplet.cs		\
	Tiles/Contact.cs		\
	Tiles/File.cs			\
	Tiles/Folder.cs			\
	Tiles/HitFlavor.cs		\
	Tiles/IMLog.cs			\
	Tiles/Image.cs			\
	Tiles/MailMessage.cs		\
	Tiles/MailAttachment.cs		\
	Tiles/Presentation.cs		\
	Tiles/RSSFeed.cs		\
	Tiles/Spreadsheet.cs		\
	Tiles/TextDocument.cs		\
	Tiles/Tile.cs			\
	Tiles/TileAction.cs		\
	Tiles/TileActivator.cs		\
	Tiles/TileGroup.cs		\
	Tiles/TileTemplate.cs		\
	Tiles/Utils.cs			\
	Tiles/Video.cs			\
	Tiles/WebHistory.cs		\
	Tiles/WidgetFu.cs


PAGES =					\
	Pages/Base.cs			\
	Pages/NoMatch.cs		\
	Pages/QuickTips.cs		\
	Pages/StartDaemon.cs

TRAY =					\
	Tray/NotificationArea.cs	\
	Tray/TrayIcon.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../images/Images.dll		\
	../BeagleClient/Beagle.dll

ASSEMBLIES =				\
	$(BEAGLE_UI_LIBS)		\
	$(LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix

Search.exe: $(CSFILES) $(TILES) $(PAGES) $(TRAY) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(TILES) $(PAGES) $(TRAY) $(ASSEMBLIES)

desktopdir       = $(datadir)/applications
desktop_DATA     = beagle-search.desktop

$(desktop_DATA): $(desktop_DATA).in
	sed -e "s|\@BINDIR\@|$(bindir)|" $< > $@


EXTRA_DIST =			\
	$(CSFILES)		\
	$(TILES)		\
	$(PAGES)		\
	$(TRAY)			\
	$(desktop_DATA).in	\
	beagle-search.in

CLEANFILES =			\
	Search.exe		\
	Search.exe.mdb		\
	beagle-search		\
	$(desktop_DATA)
