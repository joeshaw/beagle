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


CRAWLERRSS20_TARGET = CrawlerRSS20.exe
CRAWLERRSS20_CSFILES = CrawlerRSS20.cs

$(CRAWLERRSS20_TARGET): $(CRAWLERRSS20_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


QUERY_TARGET  = Query.exe
QUERY_CSFILES = Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


EXTRACTCONTENT_TARGET  = ExtractContent.exe
EXTRACTCONTENT_CSFILES = ExtractContent.cs

$(EXTRACTCONTENT_TARGET): $(EXTRACTCONTENT_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


all: $(CRAWLER_TARGET) $(CRAWLERRSS20_TARGET) $(QUERY_TARGET) $(EXTRACTCONTENT_TARGET)