CSC=mcs -g
LIBFLAGS =			\
	-target:exe		\
	-L ../Lucene.Net	\
	-r:Lucene.Net		\
	-L ../Filters		\
	-r:Filters		\
	-L ../indexer		\
	-r:Indexer


CRAWLER_TARGET  = Crawler.exe
CRAWLER_CSFILES = Crawler.cs

$(CRAWLER_TARGET): $(CRAWLER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


CRAWLER_RSS20_TARGET = CrawlerRSS20.exe
CRAWLER_RSS20_CSFILES = CrawlerRSS20.cs

$(CRAWLER_RSS20_TARGET): $(CRAWLER_RSS20_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


QUERY_TARGET  = Query.exe
QUERY_CSFILES = Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


EXTRACT_CONTENT_TARGET  = ExtractContent.exe
EXTRACT_CONTENT_CSFILES = ExtractContent.cs

$(EXTRACT_CONTENT_TARGET): $(EXTRACT_CONTENT_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


INDEX_WEB_CONTENT_TARGET = IndexWebContent.exe
INDEX_WEB_CONTENT_CSFILES = IndexWebContent.cs

$(INDEX_WEB_CONTENT_TARGET): $(INDEX_WEB_CONTENT_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


TARGETS = 				\
	$(CRAWLER_TARGET)		\
	$(CRAWLER_RSS20_TARGET)		\
	$(QUERY_TARGET)			\
	$(EXTRACT_CONTENT_TARGET)	\
	$(INDEX_WEB_CONTENT_TARGET)

all: $(TARGETS)