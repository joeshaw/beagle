CSC=mcs -g
LIBFLAGS =				\
	-target:library			\
	-r:ICSharpCode.SharpZipLib.dll	\
	-L ../Lucene.Net		\
	-r:Lucene.Net

INDEXER_CSFILES = 		\
	IndexItem.cs		\
	IndexItemWithPayload.cs	\
	Content.cs		\
	XmlContentReader.cs	\
	ContentText.cs		\
	ContentOpenOffice.cs	\
	Query.cs		\
	IndexDriver.cs


Indexer.dll: $(INDEXER_CSFILES)
	$(CSC) $(LIBFLAGS) -out:$@ $^

all: Indexer.dll
