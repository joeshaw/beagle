
CSC = mcs -g

ASSEMBLIES =			\
	-L ../Lucene.Net	\
	-r:Lucene.Net		\
	-L ../Filters		\
	-r:Filters		\
	-L ../indexer		\
	-r:Indexer

WRAPPER_IN = wrapper.in


CRAWLER_TARGET  = Crawler.exe
CRAWLER_WRAPPER = dewey-crawler
CRAWLER_CSFILES = Crawler.cs

$(CRAWLER_TARGET): $(CRAWLER_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CRAWLER_CSFILES) $(ASSEMBLIES)

$(CRAWLER_WRAPPER): $(WRAPPER_IN)
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(CRAWLER_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $(CRAWLER_WRAPPER)


CRAWLER_RSS20_TARGET = CrawlerRSS20.exe
CRAWLER_RSS20_WRAPPER = dewey-crawler-rss-2.0
CRAWLER_RSS20_CSFILES = CrawlerRSS20.cs

$(CRAWLER_RSS20_TARGET): $(CRAWLER_RSS20_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CRAWLER_RSS20_CSFILES) $(ASSEMBLIES)

$(CRAWLER_RSS20_WRAPPER): $(WRAPPER_IN)
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(CRAWLER_RSS20_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $(CRAWLER_RSS20_WRAPPER)


QUERY_TARGET  = Query.exe
QUERY_WRAPPER = dewey-query
QUERY_CSFILES = Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(QUERY_CSFILES) $(ASSEMBLIES)

$(QUERY_WRAPPER): $(WRAPPER_IN)
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(QUERY_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $(QUERY_WRAPPER)


EXTRACT_CONTENT_TARGET  = ExtractContent.exe
EXTRACT_CONTENT_WRAPPER = dewey-extract-content
EXTRACT_CONTENT_CSFILES = ExtractContent.cs

$(EXTRACT_CONTENT_TARGET): $(EXTRACT_CONTENT_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(EXTRACT_CONTENT_CSFILES) $(ASSEMBLIES)

$(EXTRACT_CONTENT_WRAPPER): $(WRAPPER_IN)
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(EXTRACT_CONTENT_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $(EXTRACT_CONTENT_WRAPPER)


INDEX_WEB_CONTENT_TARGET = IndexWebContent.exe
INDEX_WEB_CONTENT_WRAPPER = dewey-epiphany-index
INDEX_WEB_CONTENT_CSFILES = IndexWebContent.cs

$(INDEX_WEB_CONTENT_TARGET): $(INDEX_WEB_CONTENT_CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(INDEX_WEB_CONTENT_CSFILES) $(ASSEMBLIES)

$(INDEX_WEB_CONTENT_WRAPPER): $(WRAPPER_IN)
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(INDEX_WEB_CONTENT_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $(INDEX_WEB_CONTENT_WRAPPER)

TARGETS = 				\
	$(CRAWLER_TARGET)		\
	$(CRAWLER_RSS20_TARGET)		\
	$(QUERY_TARGET)			\
	$(EXTRACT_CONTENT_TARGET)	\
	$(INDEX_WEB_CONTENT_TARGET)

WRAPPERS = 				\
	$(CRAWLER_WRAPPER)		\
	$(CRAWLER_RSS20_WRAPPER)	\
	$(QUERY_WRAPPER)		\
	$(EXTRACT_CONTENT_WRAPPER)	\
	$(INDEX_WEB_CONTENT_WRAPPER)

bin_SCRIPTS = $(WRAPPERS)

all: $(TARGETS) $(WRAPPERS)

install-data-local: $(TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGETS) $(DESTDIR)$(pkglibdir)

EXTRA_DIST =				\
	$(WRAPPER_IN)			\
	$(CRAWLER_CSFILES)		\
	$(CRAWLER_RSS20_CSFILES)	\
	$(QUERY_CSFILES)		\
	$(EXTRACT_CONTENT_CSFILES)	\
	$(INDEX_WEB_CONTENT_CSFILES)

CLEANFILES = 				\
	$(TARGETS)			\
	$(WRAPPERS)