
SUBDIRS = TrayIcon

CSC = mcs -g

TARGET = BeagleTray.exe
WRAPPER = beagle-tray

CSFLAGS = -target:exe

CSFILES = 				\
	$(srcdir)/Utils.cs		\
	$(srcdir)/BeagleTray.cs		\
	$(srcdir)/Main.cs

ASSEMBLIES = 					\
	$(BEAGLE_UI_LIBS)			\
	$(LOCAL_ASSEMBLIES:%=-r:%)

LOCAL_ASSEMBLIES =			\
	TrayIcon/TrayIcon.dll 		\
	../Util/Util.dll		\
	../images/Images.dll		\
	../BeagleClient/Beagle.dll      \
	../Tiles/Tiles.dll

RESOURCES = \
	-resource:$(top_srcdir)/images/trayicon.png,trayicon.png

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES) 
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_PROGRAM) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

bin_SCRIPTS = $(WRAPPER)

WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@target\@|$(TARGET)|g"

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	$(WRAPPER_SED) < $^ > $@
	chmod +x $(WRAPPER)

EXTRA_DIST =            			\
	$(CSFILES)      			\
	$(WRAPPER).in

CLEANFILES =            \
	$(TARGET)       \
	$(WRAPPER)
