
CSC = mcs -g

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	-r:Mono.Posix			\
	$(LOCAL_ASSEMBLIES:%=-r:%)


WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"


QUERY_TARGET  = Query.exe
QUERY_WRAPPER = beagle-query
QUERY_CSFILES = $(srcdir)/Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(CSFLAGS) $(QUERY_CSFILES) $(ASSEMBLIES)

$(QUERY_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(QUERY_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(QUERY_WRAPPER)

INDEX_WEB_CONTENT_TARGET = IndexWebContent.exe
INDEX_WEB_CONTENT_WRAPPER = beagle-index-url
INDEX_WEB_CONTENT_CSFILES = $(srcdir)/IndexWebContent.cs

$(INDEX_WEB_CONTENT_TARGET): $(INDEX_WEB_CONTENT_CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(CSFLAGS) $(INDEX_WEB_CONTENT_CSFILES) $(ASSEMBLIES)

$(INDEX_WEB_CONTENT_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_WEB_CONTENT_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(INDEX_WEB_CONTENT_WRAPPER)


TARGETS = 				\
	$(QUERY_TARGET)                 \
	$(INDEX_WEB_CONTENT_TARGET)

WRAPPERS = 				\
	$(QUERY_WRAPPER)		\
	$(INDEX_WEB_CONTENT_WRAPPER)

bin_SCRIPTS = 			\
	$(WRAPPERS) 		\
	$(UPDATE_SCRIPT)	\
	beagle-ping             \
	beagle-shutdown		\
	beagle-status

all: $(TARGETS) $(WRAPPERS) $(UPDATE_SCRIPT)

install-data-local: $(TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGETS)

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(QUERY_CSFILES)		\
	$(INDEX_WEB_CONTENT_CSFILES)	\
	$(UPDATE_SCRIPT).in		\
	beagle-ping

CLEANFILES = 				\
	$(TARGETS)			\
	$(WRAPPERS)			\
	$(UPDATE_SCRIPT)
