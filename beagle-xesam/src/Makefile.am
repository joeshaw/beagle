CSC = $(MCS) -debug $(MCS_FLAGS) $(CSFLAGS)

WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@PACKAGE\@|$(PACKAGE)|g"			\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@libexecdir\@|$(libexecdir)|g"		\
	-e "s|\@bindir\@|$(bindir)|g"			\
	-e "s|\@sbindir\@|$(sbindir)|g"			\
	-e "s|\@sysconfdir\@|$(sysconfdir)|g"		\
	-e "s|\@localstatedir\@|$(localstatedir)|g"     \
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@bash\@|$(BASH)|"

DBUS_SHARP_DIR =  dbus-sharp

XESAMADAPTOR_DIST_CSFILES =			\
	$(srcdir)/Session.cs			\
	$(srcdir)/Search.cs			\
	$(srcdir)/Searcher.cs			\
	$(srcdir)/Parser.cs			\
	$(srcdir)/Ontologies.cs			\
	$(srcdir)/XesamAdaptor.cs		

GLIBSHARP_ASSEMBLIES = ${GLIBSHARP_LIBS}

DBUS_SHARP_ASSEMBLIES = 				\
	$(NDESK_DBUS_LIBS)

BEAGLE_ASSEMBLIES = ${BEAGLE_LIBS}

XESAMADAPTOR_ASSEMBLIES = $(DBUS_SHARP_ASSEMBLIES)	\
			  $(GLIBSHARP_ASSEMBLIES)	\
			  $(BEAGLE_ASSEMBLIES)

XESAMADAPTOR_RESOURCES = 

XESAMADAPTOR_TARGET = XesamAdaptor.exe
XESAMADAPTOR_WRAPPER = beagle-xesam-adaptor
XESAMADAPTOR_CSFILES = $(XESAMADAPTOR_DIST_CSFILES)

$(XESAMADAPTOR_TARGET): $(XESAMADAPTOR_CSFILES)
	$(CSC) -out:$@ $(XESAMADAPTOR_CSFILES) $(XESAMADAPTOR_ASSEMBLIES) $(XESAMADAPTOR_RESOURCES)

$(XESAMADAPTOR_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(XESAMADAPTOR_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(XESAMADAPTOR_WRAPPER)

TARGETS = $(XESAMADAPTOR_TARGET)
TARGETS_MDB = $(TARGETS:%=%.mdb)

BUILT_WRAPPERS = $(XESAMADAPTOR_WRAPPER)
WRAPPERS = $(BUILT_WRAPPERS)

all: $(TARGETS) $(WRAPPERS)

install-data-local: $(TARGETS) $(WRAPPERS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(TARGETS_MDB) $(DESTDIR)$(pkglibdir)

	$(mkinstalldirs) $(DESTDIR)$(bindir)

	@for i in $(BUILT_WRAPPERS); do						\
		sed -e "s|\#installed=1|installed=1|" < $$i > $$i.tmp;		\
		echo "$(INSTALL_SCRIPT) $$i.tmp $(DESTDIR)$(bindir)/$$i";	\
		$(INSTALL_SCRIPT) $$i.tmp $(DESTDIR)$(bindir)/$$i || exit 1;	\
		rm -f $$i.tmp;							\
	done

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGETS) && rm -f $(TARGETS_MDB)

	@for i in $(BIN_WRAPPERS); do				\
		echo "rm -f $(DESTDIR)$(bindir)/$$i";		\
		rm -f $(DESTDIR)$(bindir)/$$i || exit 1;	\
	done

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(XESAMADAPTOR_DIST_CSFILES)

CLEANFILES = 				\
	$(TARGETS)			\
	$(TARGETS_MDB)			\
	$(BUILT_WRAPPERS)

MAINTAINERCLEANFILES = \
	Makefile.in
