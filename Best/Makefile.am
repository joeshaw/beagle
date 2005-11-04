
CSC = mcs -debug

TARGET = Best.exe
WRAPPER = best

WRAPPER_SED = sed					\
	-e "s|\@prefix\@|$(prefix)|g" 			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@target\@|$(TARGET)|g" 			\
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@mozillahome\@|$(MOZILLA_HOME)|g"	\
	-e "s|\@bash\@|$(BASH)|g"

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
	$(LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix

RESOURCES =

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) $(RESOURCES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)


bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	$(WRAPPER_SED) < $^ > $@
	chmod +x $(WRAPPER)

@INTLTOOL_DESKTOP_RULE@

desktop_in_files = best.desktop.in
desktop_files = $(desktop_in_files:.desktop.in=.desktop)

desktopdir = $(datadir)/applications
desktop_DATA = $(desktop_files)

man_MANS = best.1

EXTRA_DIST =		\
	$(CSFILES)	\
	$(WRAPPER).in	\
	$(desktop_DATA) \
	$(man_MANS)

CLEANFILES =		\
	$(desktop_DATA) \
	$(TARGET)	\
	$(TARGET).mdb	\
	$(WRAPPER)
