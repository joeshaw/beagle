CSC=mcs -g
LIBFLAGS =			\
	-target:exe		\
	-L ../Lucene.Net	\
	-r:Lucene.Net		\
	-L ../indexer		\
	-r:Indexer

CRAWLER_TARGET  = Crawler.exe
CRAWLER_CSFILES = Crawler.cs

$(CRAWLER_TARGET): $(CRAWLER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

QUERY_TARGET  = Query.exe
QUERY_CSFILES = Query.cs

$(QUERY_TARGET): $(QUERY_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^


all: $(CRAWLER_TARGET) $(QUERY_TARGET)