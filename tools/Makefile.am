
CSC = mcs -g

ASSEMBLIES =				\
	$(EVO_SHARP_LIBS)		\
	$(BEAGLE_UI_LIBS)		\
	-r:../Util/Util.dll		\
	-r:../Lucene.Net/Lucene.Net.dll	\
	-r:../Filters/Filters.dll	\
	-r:../indexer/Indexer.dll


WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"


CRAWLER_TARGET  = Crawler.exe
CRAWLER_WRAPPER = beagle-index-files
CRAWLER_CSFILES =$(srcdir)/Crawler.cs

$(CRAWLER_TARGET): $(CRAWLER_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CRAWLER_CSFILES) $(ASSEMBLIES)

$(CRAWLER_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(CRAWLER_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(CRAWLER_WRAPPER)


CRAWLER_RSS20_TARGET = CrawlerRSS20.exe
CRAWLER_RSS20_WRAPPER = beagle-index-rss
CRAWLER_RSS20_CSFILES = $(srcdir)/CrawlerRSS20.cs

$(CRAWLER_RSS20_TARGET): $(CRAWLER_RSS20_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CRAWLER_RSS20_CSFILES) $(ASSEMBLIES)

$(CRAWLER_RSS20_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(CRAWLER_RSS20_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(CRAWLER_RSS20_WRAPPER)


QUERY_TARGET  = Query.exe
QUERY_WRAPPER = beagle-query
QUERY_CSFILES = $(srcdir)/Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(QUERY_CSFILES) $(ASSEMBLIES)

$(QUERY_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(QUERY_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(QUERY_WRAPPER)


EXTRACT_CONTENT_TARGET  = ExtractContent.exe
EXTRACT_CONTENT_WRAPPER = beagle-extract-content
EXTRACT_CONTENT_CSFILES = $(srcdir)/ExtractContent.cs

$(EXTRACT_CONTENT_TARGET): $(EXTRACT_CONTENT_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(EXTRACT_CONTENT_CSFILES) $(ASSEMBLIES)

$(EXTRACT_CONTENT_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(EXTRACT_CONTENT_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(EXTRACT_CONTENT_WRAPPER)


INDEX_WEB_CONTENT_TARGET = IndexWebContent.exe
INDEX_WEB_CONTENT_WRAPPER = beagle-epiphany-index
INDEX_WEB_CONTENT_CSFILES = $(srcdir)/IndexWebContent.cs

$(INDEX_WEB_CONTENT_TARGET): $(INDEX_WEB_CONTENT_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(INDEX_WEB_CONTENT_CSFILES) $(ASSEMBLIES)

$(INDEX_WEB_CONTENT_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_WEB_CONTENT_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(INDEX_WEB_CONTENT_WRAPPER)


INDEX_MAIL_TARGET = IndexMail.exe
INDEX_MAIL_WRAPPER = beagle-index-mail
INDEX_MAIL_CSFILES = $(srcdir)/IndexMail.cs

$(INDEX_MAIL_TARGET): $(INDEX_MAIL_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $^ $(ASSEMBLIES)

$(INDEX_MAIL_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_MAIL_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $(INDEX_MAIL_WRAPPER)


INDEX_GAIM_LOGS_TARGET = IndexGaimLogs.exe
INDEX_GAIM_LOGS_WRAPPER = beagle-index-gaim-logs
INDEX_GAIM_LOGS_CSFILES = $(srcdir)/IndexGaimLogs.cs

$(INDEX_GAIM_LOGS_TARGET): $(INDEX_GAIM_LOGS_CSFILES)	
	$(CSC) -o $@ $(CSFLAGS) $^ $(ASSEMBLIES)

$(INDEX_GAIM_LOGS_WRAPPER): $(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_GAIM_LOGS_TARGET)|g" < $(srcdir)/$^ > $@
	chmod +x $@


UPDATE_SCRIPT = beagle-update-index

$(UPDATE_SCRIPT): $(UPDATE_SCRIPT).in
	sed -e "s|\@prefix\@|$(prefix)|g" < $(srcdir)/$^ > $@
	chmod +x $@

TARGETS = 				\
	$(CRAWLER_TARGET)		\
	$(CRAWLER_RSS20_TARGET)		\
	$(QUERY_TARGET)			\
	$(EXTRACT_CONTENT_TARGET)	\
	$(INDEX_WEB_CONTENT_TARGET)	\
	$(INDEX_MAIL_TARGET)		\
	$(INDEX_GAIM_LOGS_TARGET)

WRAPPERS = 				\
	$(CRAWLER_WRAPPER)		\
	$(CRAWLER_RSS20_WRAPPER)	\
	$(QUERY_WRAPPER)		\
	$(EXTRACT_CONTENT_WRAPPER)	\
	$(INDEX_WEB_CONTENT_WRAPPER)	\
	$(INDEX_MAIL_WRAPPER)		\
	$(INDEX_GAIM_LOGS_WRAPPER)

bin_SCRIPTS = $(WRAPPERS) $(UPDATE_SCRIPT)

all: $(TARGETS) $(WRAPPERS) $(UPDATE_SCRIPT)

install-data-local: $(TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(DESTDIR)$(pkglibdir)

uninstall-local:
	cd $(DESTDIR)$(pkglibdir) && rm -f $(TARGETS)

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(CRAWLER_CSFILES)		\
	$(CRAWLER_RSS20_CSFILES)	\
	$(QUERY_CSFILES)		\
	$(EXTRACT_CONTENT_CSFILES)	\
	$(INDEX_WEB_CONTENT_CSFILES)	\
	$(INDEX_MAIL_CSFILES)		\
	$(UPDATE_SCRIPT).in

CLEANFILES = 				\
	$(TARGETS)			\
	$(WRAPPERS)			\
	$(UPDATE_SCRIPT)