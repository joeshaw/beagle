CSC=mcs -g
LIBFLAGS =			\
	-target:library		\
	-L ../Lucene.Net	\
	-r:Lucene.Net

INDEXER_CSFILES = 		\
	IndexItem.cs		\
	IndexItemWithPayload.cs	\
	Content.cs		\
	ContentText.cs		\
	IndexDriver.cs


Indexer.dll: $(INDEXER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Indexer.dll
