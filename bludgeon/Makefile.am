
CSC = mcs -debug

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	$(LOCAL_ASSEMBLIES:%=-r:%)

WRAPPER_IN = bludgeon.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@bindir\@|$(bindir)|g"			\
	-e "s|\@sbindir\@|$(sbindir)|g"			\
	-e "s|\@sysconfdir\@|$(sysconfdir)|g"		\
	-e "s|\@localstatedir\@|$(localstatedir)|g"     \
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@bash\@|$(BASH)|"

WRAPPER = bludgeon

TARGET = Bludgeon.exe

CSFILES = 				\
	$(srcdir)/Daemon.cs		\
	$(srcdir)/FileModel.cs		\
	$(srcdir)/Log.cs		\
	$(srcdir)/Token.cs		\
	$(srcdir)/QueryFu.cs		\
	$(srcdir)/SanityCheck.cs	\
	$(srcdir)/Bludgeon.cs

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

$(WRAPPER) : $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(WRAPPER)

bin_SCRIPTS = 			\
	$(WRAPPER)

all: $(TARGET) $(WRAPPER)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGET)

EXTRA_DIST =				\
	$(WRAPPER_IN)
	$(CSFILES)

CLEANFILES = 				\
	$(TARGET)			\
	$(TARGET:%=%.mdb)		\
	$(WRAPPER)


