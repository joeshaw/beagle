CSC=mcs -g
LIBFLAGS =				\
	-target:library			\
	-r:ICSharpCode.SharpZipLib.dll	\
	-L ../Lucene.Net		\
	-r:Lucene.Net.dll		\
	-L ../Filters			\
	-r:Filters.dll

INDEXER_CSFILES = 		\
	Versioned.cs		\
	Indexable.cs		\
	IndexableFile.cs	\
	IndexableHttp.cs	\
	Query.cs		\
	Hit.cs			\
	IndexDriver.cs


Indexer.dll: $(INDEXER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Indexer.dll
