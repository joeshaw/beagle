
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
	-r:$(top_builddir)/tray/TrayIcon/TrayIcon.dll

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

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" < $^ > $@
	chmod +x $(WRAPPER)

EXTRA_DIST =            			\
	$(CSFILES)      			\
	$(WRAPPER).in

CLEANFILES =            \
	$(TARGET)       \
	$(WRAPPER)
