
CSC = mcs -debug -nowarn:0219

TARGET = ImLogViewer.exe
WRAPPER = beagle-imlogviewer

CSFLAGS = -target:exe

CSFILES =				\
	$(srcdir)/ImLogViewer.cs	\
	$(srcdir)/ImLogWindow.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll

ASSEMBLIES =				\
	$(BEAGLE_UI_LIBS)		\
	$(LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix

RESOURCEFILES =				\
	$(srcdir)/ImLogViewer.glade

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


bin_SCRIPTS = $(WRAPPER)

$(WRAPPER): $(srcdir)/$(WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" -e "s|\@gacprefix\@|$(GAC_PREFIX)|g" < $^ > $@
	chmod +x $(WRAPPER)

EXTRA_DIST =			\
	$(CSFILES)		\
	$(WRAPPER).in		\
	$(RESOURCEFILES)

CLEANFILES =		\
	$(TARGET)	\
	$(TARGET).mdb	\
	$(WRAPPER)	\
	ImLogViewer.glade.h
