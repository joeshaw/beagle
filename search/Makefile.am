CSC = mcs -debug -nowarn:0618
CSFLAGS = -target:exe

bin_SCRIPTS = beagle-search
pkglib_DATA = Search.exe Search.exe.mdb

beagle-search: beagle-search.in
	sed -e "s:@PKGLIBDIR@:${pkglibdir}:" < $(srcdir)/beagle-search.in > beagle-search
	chmod a+x beagle-search

CSFILES =					\
	$(srcdir)/Category.cs			\
	$(srcdir)/ConversationCategory.cs	\
	$(srcdir)/Entry.cs			\
	$(srcdir)/GroupView.cs			\
	$(srcdir)/Panes.cs			\
	$(srcdir)/Search.cs			\
	$(srcdir)/SortedTileList.cs		\
	$(srcdir)/Spinner.cs			\
	$(srcdir)/TileCategory.cs		\
	$(srcdir)/TypeFilter.cs			\
	$(srcdir)/UIManager.cs

TILES = 					\
	$(srcdir)/Tiles/ActionMenuItem.cs	\
	$(srcdir)/Tiles/Application.cs		\
	$(srcdir)/Tiles/Audio.cs		\
	$(srcdir)/Tiles/CApplet.cs		\
	$(srcdir)/Tiles/Contact.cs		\
	$(srcdir)/Tiles/File.cs			\
	$(srcdir)/Tiles/Folder.cs		\
	$(srcdir)/Tiles/HitFlavor.cs		\
	$(srcdir)/Tiles/IMLog.cs		\
	$(srcdir)/Tiles/Image.cs		\
	$(srcdir)/Tiles/MailMessage.cs		\
	$(srcdir)/Tiles/MailAttachment.cs	\
	$(srcdir)/Tiles/Presentation.cs		\
	$(srcdir)/Tiles/RSSFeed.cs		\
	$(srcdir)/Tiles/Spreadsheet.cs		\
	$(srcdir)/Tiles/TextDocument.cs		\
	$(srcdir)/Tiles/Tile.cs			\
	$(srcdir)/Tiles/TileAction.cs		\
	$(srcdir)/Tiles/TileActivator.cs	\
	$(srcdir)/Tiles/TileGroup.cs		\
	$(srcdir)/Tiles/TileTemplate.cs		\
	$(srcdir)/Tiles/Utils.cs		\
	$(srcdir)/Tiles/Video.cs		\
	$(srcdir)/Tiles/WebHistory.cs		\
	$(srcdir)/Tiles/WidgetFu.cs


PAGES =						\
	$(srcdir)/Pages/Base.cs			\
	$(srcdir)/Pages/NoMatch.cs		\
	$(srcdir)/Pages/QuickTips.cs		\
	$(srcdir)/Pages/StartDaemon.cs

TRAY =						\
	$(srcdir)/Tray/NotificationArea.cs	\
	$(srcdir)/Tray/TrayIcon.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../Util/UiUtil.dll		\
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
