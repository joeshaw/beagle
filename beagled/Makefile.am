
CSC = mcs -g


EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in

$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"	\
		< $^ > $@



PLUGIN_TARGET = BeagleDaemonPlugins.dll

PLUGIN_CFLAGS = -target:library

PLUGIN_CSFILES =			\
	$(srcdir)/PathFinder.cs		\
	$(srcdir)/Property.cs		\
	$(srcdir)/Flavor.cs		\
	$(srcdir)/Filter.cs		\
	$(srcdir)/Indexable.cs		\
	$(srcdir)/PreIndexHandler.cs	\
	$(srcdir)/PostIndexHandler.cs

PLUGIN_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

PLUGIN_ASSEMBLIES =				\
	-r:Mono.Posix				\
	$(BEAGLED_LIBS)                 	\
	$(PLUGIN_LOCAL_ASSEMBLIES:%=-r:%)

$(PLUGIN_TARGET): $(PLUGIN_CSFILES) $(EXTSTR) $(PLUGIN_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(PLUGIN_CFLAGS) $(PLUGIN_CSFILES) $(EXTSTR) $(PLUGIN_ASSEMBLIES)



DAEMON_TARGET = BeagleDaemon.exe

DAEMON_WRAPPER = beagled

DAEMON_CSFLAGS = 			\
	-target:exe			\
	-main:BeagleDaemon.BeagleDaemon

lucenedir = $(srcdir)/Lucene.Net

LUCENE_CSFILES =								\
	$(lucenedir)/AssemblyInfo.cs					\
	$(lucenedir)/Analysis/Analyzer.cs				\
	$(lucenedir)/Analysis/CharTokenizer.cs				\
	$(lucenedir)/Analysis/LetterTokenizer.cs			\
	$(lucenedir)/Analysis/LowerCaseFilter.cs			\
	$(lucenedir)/Analysis/LowerCaseTokenizer.cs			\
	$(lucenedir)/Analysis/PerFieldAnalyzerWrapper.cs		\
	$(lucenedir)/Analysis/PorterStemFilter.cs			\
	$(lucenedir)/Analysis/PorterStemmer.cs				\
	$(lucenedir)/Analysis/SimpleAnalyzer.cs				\
	$(lucenedir)/Analysis/StopAnalyzer.cs				\
	$(lucenedir)/Analysis/StopFilter.cs				\
	$(lucenedir)/Analysis/Token.cs					\
	$(lucenedir)/Analysis/TokenFilter.cs				\
	$(lucenedir)/Analysis/Tokenizer.cs				\
	$(lucenedir)/Analysis/TokenStream.cs				\
	$(lucenedir)/Analysis/WhitespaceAnalyzer.cs			\
	$(lucenedir)/Analysis/WhitespaceTokenizer.cs			\
	$(lucenedir)/Analysis/Standard/CharStream.cs			\
	$(lucenedir)/Analysis/Standard/FastCharStream.cs		\
	$(lucenedir)/Analysis/Standard/ParseException.cs		\
	$(lucenedir)/Analysis/Standard/StandardAnalyzer.cs		\
	$(lucenedir)/Analysis/Standard/StandardFilter.cs		\
	$(lucenedir)/Analysis/Standard/StandardTokenizerConstants.cs	\
	$(lucenedir)/Analysis/Standard/StandardTokenizer.cs		\
	$(lucenedir)/Analysis/Standard/StandardTokenizerTokenManager.cs	\
	$(lucenedir)/Analysis/Standard/Token.cs				\
	$(lucenedir)/Analysis/Standard/TokenMgrError.cs			\
	$(lucenedir)/Document/DateField.cs				\
	$(lucenedir)/Document/Document.cs				\
	$(lucenedir)/Document/Field.cs					\
	$(lucenedir)/Index/CompoundFileReader.cs			\
	$(lucenedir)/Index/CompoundFileWriter.cs			\
	$(lucenedir)/Index/DocumentWriter.cs				\
	$(lucenedir)/Index/FieldInfo.cs					\
	$(lucenedir)/Index/FieldInfos.cs				\
	$(lucenedir)/Index/FieldsReader.cs				\
	$(lucenedir)/Index/FieldsWriter.cs				\
	$(lucenedir)/Index/FilterIndexReader.cs				\
	$(lucenedir)/Index/IndexReader.cs				\
	$(lucenedir)/Index/IndexWriter.cs				\
	$(lucenedir)/Index/MultipleTermPositions.cs			\
	$(lucenedir)/Index/SegmentInfo.cs				\
	$(lucenedir)/Index/SegmentInfos.cs				\
	$(lucenedir)/Index/SegmentMergeInfo.cs				\
	$(lucenedir)/Index/SegmentMergeQueue.cs				\
	$(lucenedir)/Index/SegmentMerger.cs				\
	$(lucenedir)/Index/SegmentReader.cs				\
	$(lucenedir)/Index/SegmentsReader.cs				\
	$(lucenedir)/Index/SegmentTermDocs.cs				\
	$(lucenedir)/Index/SegmentTermEnum.cs				\
	$(lucenedir)/Index/SegmentTermPositions.cs			\
	$(lucenedir)/Index/Term.cs					\
	$(lucenedir)/Index/TermDocs.cs					\
	$(lucenedir)/Index/TermEnum.cs					\
	$(lucenedir)/Index/TermInfo.cs					\
	$(lucenedir)/Index/TermInfosReader.cs				\
	$(lucenedir)/Index/TermInfosWriter.cs				\
	$(lucenedir)/QueryParser/CharStream.cs				\
	$(lucenedir)/QueryParser/FastCharStream.cs			\
	$(lucenedir)/QueryParser/MultiFieldQueryParser.cs		\
	$(lucenedir)/QueryParser/ParseException.cs			\
	$(lucenedir)/QueryParser/QueryParserConstants.cs		\
	$(lucenedir)/QueryParser/QueryParser.cs				\
	$(lucenedir)/QueryParser/QueryParserTokenManager.cs		\
	$(lucenedir)/QueryParser/TermPositions.cs			\
	$(lucenedir)/QueryParser/Token.cs				\
	$(lucenedir)/QueryParser/TokenMgrError.cs			\
	$(lucenedir)/Search/BooleanClause.cs				\
	$(lucenedir)/Search/BooleanQuery.cs				\
	$(lucenedir)/Search/BooleanScorer.cs				\
	$(lucenedir)/Search/CachingWrapperFilter.cs			\
	$(lucenedir)/Search/DateFilter.cs				\
	$(lucenedir)/Search/DefaultSimilarity.cs			\
	$(lucenedir)/Search/ExactPhraseScorer.cs			\
	$(lucenedir)/Search/Explanation.cs				\
	$(lucenedir)/Search/Filter.cs					\
	$(lucenedir)/Search/FilteredTermEnum.cs				\
	$(lucenedir)/Search/FuzzyQuery.cs				\
	$(lucenedir)/Search/FuzzyTermEnum.cs				\
	$(lucenedir)/Search/HitCollector.cs				\
	$(lucenedir)/Search/HitQueue.cs					\
	$(lucenedir)/Search/Hits.cs					\
	$(lucenedir)/Search/IndexSearcher.cs				\
	$(lucenedir)/Search/MultiSearcher.cs				\
	$(lucenedir)/Search/MultiTermQuery.cs				\
	$(lucenedir)/Search/PhrasePositions.cs				\
	$(lucenedir)/Search/PhrasePrefixQuery.cs			\
	$(lucenedir)/Search/PhraseQuery.cs				\
	$(lucenedir)/Search/PhraseQueue.cs				\
	$(lucenedir)/Search/PhraseScorer.cs				\
	$(lucenedir)/Search/PrefixQuery.cs				\
	$(lucenedir)/Search/Query.cs					\
	$(lucenedir)/Search/QueryFilter.cs				\
	$(lucenedir)/Search/RangeQuery.cs				\
	$(lucenedir)/Search/RemoteSearchable.cs				\
	$(lucenedir)/Search/ScoreDoc.cs					\
	$(lucenedir)/Search/Scorer.cs					\
	$(lucenedir)/Search/Searchable.cs				\
	$(lucenedir)/Search/Searcher.cs					\
	$(lucenedir)/Search/Similarity.cs				\
	$(lucenedir)/Search/SloppyPhraseScorer.cs			\
	$(lucenedir)/Search/TermQuery.cs				\
	$(lucenedir)/Search/TermScorer.cs				\
	$(lucenedir)/Search/TopDocs.cs					\
	$(lucenedir)/Search/Weight.cs					\
	$(lucenedir)/Search/WildcardQuery.cs				\
	$(lucenedir)/Search/WildcardTermEnum.cs				\
	$(lucenedir)/Store/Directory.cs					\
	$(lucenedir)/Store/FSDirectory.cs				\
	$(lucenedir)/Store/InputStream.cs				\
	$(lucenedir)/Store/Lock.cs					\
	$(lucenedir)/Store/OutputStream.cs				\
	$(lucenedir)/Store/RAMDirectory.cs				\
	$(lucenedir)/Util/Arrays.cs					\
	$(lucenedir)/Util/BitVector.cs					\
	$(lucenedir)/Util/Number.cs					\
	$(lucenedir)/Util/PriorityQueue.cs

DAEMON_CSFILES = 				\
	$(LUCENE_CSFILES)		\
	$(srcdir)/IndexableFile.cs	\
	$(srcdir)/Query.cs		\
	$(srcdir)/Relevancy.cs		\
	$(srcdir)/IQueryResult.cs	\
	$(srcdir)/IQueryable.cs		\
	$(srcdir)/QueryableFlavor.cs	\
	$(srcdir)/QueryResult.cs	\
	$(srcdir)/QueryDriver.cs	\
	$(srcdir)/IndexDriver.cs	\
	$(srcdir)/GoogleDriver.cs	\
	$(srcdir)/BeagleDaemon.cs       \
	$(srcdir)/Indexer.cs            \
	$(srcdir)/IndexerQueue.cs       \
	$(srcdir)/LiveQuery.cs          \
	$(srcdir)/NoIndexFileHandler.cs \
	$(srcdir)/QueryManager.cs

DAEMON_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

DAEMON_ASSEMBLIES = 				\
	$(BEAGLED_LIBS)                 	\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll		\
	-r:./$(PLUGIN_TARGET)


if ENABLE_EVO_SHARP
DAEMON_CSFLAGS += -define:ENABLE_EVO_SHARP
DAEMON_CSFILES += $(srcdir)/EvolutionDataServerDriver.cs
DAEMON_ASSEMBLIES += $(EVO_SHARP_LIBS)
endif



$(DAEMON_TARGET): $(DAEMON_CSFILES) $(DAEMON_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(DAEMON_CSFLAGS) $(DAEMON_CSFILES) $(DAEMON_ASSEMBLIES)

#TestClient.exe: TestClient.cs 
#	$(CSC) -o $@ $(CSFLAGS) TestClient.cs $(ASSEMBLIES)

all: $(PLUGIN_TARGET) $(DAEMON_TARGET)

install-data-local: $(PLUGIN_TARGET) $(DAEMON_TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(PLUGIN_TARGET) $(DESTDIR)$(pkglibdir)/$(PLUGIN_TARGET)
	$(INSTALL_DATA) $(DAEMON_TARGET) $(DESTDIR)$(pkglibdir)/$(DAEMON_TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(bindir)/$(TARGET)

bin_SCRIPTS = $(DAEMON_WRAPPER)

$(DAEMON_WRAPPER): $(srcdir)/$(DAEMON_WRAPPER).in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|$(TARGET)|g" -e "s|\@gacprefix\@|$(GAC_PREFIX)|g" < $^ > $@
	chmod +x $(DAEMON_WRAPPER)

noinst_SCRIPTS=testclient

testclient: $(srcdir)/testclient.in
	sed -e "s|\@prefix\@|$(prefix)|g" -e "s|\@pkglibdir\@|$(pkglibdir)|g" -e "s|\@target\@|TestClient.exe|g" < $^ > $@
	chmod +x testclient


EXTRA_DIST =			\
	$(PLUGIN_CSFILES)	\
	$(DAEMON_CSFILES)	\
	$(EXTSTR_IN)		\
	$(DAEMON_WRAPPER).in   	\
	TestClient.cs

CLEANFILES =			\
	$(PLUGIN_TARGET)       	\
	$(DAEMON_TARGET)       	\
	$(EXTSTR)		\
	$(DAEMON_WRAPPER)      	\
	testclient      	\
	TestClient.exe
