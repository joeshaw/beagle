
CSC = mcs -g

TARGET = Best.exe
WRAPPER = best

CSFLAGS = -target:exe

CSFILES =				\
	$(srcdir)/Best.cs		\
	$(srcdir)/BestWindow.cs		\
	$(srcdir)/BestTray.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../images/Images.dll		\
	../BeagleClient/Beagle.dll      \
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

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)


bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" -e "s|\@gacprefix\@|$(GAC_PREFIX)|g" -e "s|\@mozillahome\@|$(MOZILLA_HOME)|g" < $^ > $@
	chmod +x $(WRAPPER)

desktopdir = $(datadir)/applications
desktop_DATA = best.desktop

man_MANS = best.1

EXTRA_DIST =		\
	$(CSFILES)	\
	$(WRAPPER).in	\
	$(desktop_DATA) \
	$(man_MANS)

CLEANFILES =		\
	$(TARGET)	\
	$(TARGET).mdb	\
	$(WRAPPER)
