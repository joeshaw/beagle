CSC=mcs -g
LIBFLAGS =				\
	-target:library			\
	-r:ICSharpCode.SharpZipLib.dll	\
	-L ../Lucene.Net		\
	-r:Lucene.Net.dll		\
	-L ../filters			\
	-r:Filters.dll

INDEXER_CSFILES = 		\
	Indexable.cs		\
	IndexableFile.cs	\
	Query.cs		\
	Hit.cs			\
	IndexDriver.cs


Indexer.dll: $(INDEXER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Indexer.dll
