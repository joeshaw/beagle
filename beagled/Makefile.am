
# Warnings we don't want to see
# 0067 = event defined but not used
# 0618 = Mono.Posix.Syscall is obsolete

CSC = mcs -g -nowarn:0067,0618

PLUGIN_TARGET = BeagleDaemonPlugins.dll

PLUGIN_CFLAGS = -target:library

PLUGIN_CSFILES =			\
	$(srcdir)/PathFinder.cs		\
	$(srcdir)/Flavor.cs		\
	$(srcdir)/Filter.cs		\
	$(srcdir)/PreIndexHandler.cs	\
	$(srcdir)/PostIndexHandler.cs

PLUGIN_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

PLUGIN_ASSEMBLIES =				\
	-r:Mono.Posix				\
	$(BEAGLED_LIBS)                 	\
	$(PLUGIN_LOCAL_ASSEMBLIES:%=-r:%)

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"	\
		-e "s|\@VERSION\@|$(VERSION)|g"		\
		-e "s|\@GNOME_PREFIX\@|$(GNOME_PREFIX)|g"	\
		< $(EXTSTR_IN) > $@


$(PLUGIN_TARGET): $(PLUGIN_CSFILES) $(EXTSTR) $(PLUGIN_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(PLUGIN_CFLAGS) $(PLUGIN_CSFILES) $(EXTSTR) $(PLUGIN_ASSEMBLIES)

############################################################

WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"


############################################################

lucenedir = $(srcdir)/Lucene.Net

LUCENE_1_3_CSFILES =							\
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


LUCENE_1_4_CSFILES =							\
	$(lucenedir)/AssemblyInfo.cs					\
	$(lucenedir)/SupportClass.cs					\
	$(lucenedir)/Store/Directory.cs					\
	$(lucenedir)/Store/FSDirectory.cs				\
	$(lucenedir)/Store/InputStream.cs				\
	$(lucenedir)/Store/Lock.cs					\
	$(lucenedir)/Store/OutputStream.cs				\
	$(lucenedir)/Store/RAMDirectory.cs				\
	$(lucenedir)/Store/RAMFile.cs					\
	$(lucenedir)/Store/RAMInputStream.cs				\
	$(lucenedir)/Store/RAMOutputStream.cs				\
	$(lucenedir)/Util/BitVector.cs					\
	$(lucenedir)/Util/Constants.cs					\
	$(lucenedir)/Util/PriorityQueue.cs				\
	$(lucenedir)/Util/StringHelper.cs				\
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
	$(lucenedir)/Index/MultiReader.cs				\
	$(lucenedir)/Index/SegmentInfo.cs				\
	$(lucenedir)/Index/SegmentInfos.cs				\
	$(lucenedir)/Index/SegmentMergeInfo.cs				\
	$(lucenedir)/Index/SegmentMergeQueue.cs				\
	$(lucenedir)/Index/SegmentMerger.cs				\
	$(lucenedir)/Index/SegmentReader.cs				\
	$(lucenedir)/Index/SegmentTermDocs.cs				\
	$(lucenedir)/Index/SegmentTermEnum.cs				\
	$(lucenedir)/Index/SegmentTermPositions.cs			\
	$(lucenedir)/Index/SegmentTermVector.cs				\
	$(lucenedir)/Index/Term.cs					\
	$(lucenedir)/Index/TermDocs.cs					\
	$(lucenedir)/Index/TermEnum.cs					\
	$(lucenedir)/Index/TermFreqVector.cs				\
	$(lucenedir)/Index/TermInfo.cs					\
	$(lucenedir)/Index/TermInfosReader.cs				\
	$(lucenedir)/Index/TermInfosWriter.cs				\
	$(lucenedir)/Index/TermPositions.cs				\
	$(lucenedir)/Index/TermPositionVector.cs			\
	$(lucenedir)/Index/TermVectorsReader.cs				\
	$(lucenedir)/Index/TermVectorsWriter.cs				\
	$(lucenedir)/Document/DateField.cs				\
	$(lucenedir)/Document/Document.cs				\
	$(lucenedir)/Document/Field.cs					\
	$(lucenedir)/Analysis/DE/GermanAnalyzer.cs			\
	$(lucenedir)/Analysis/DE/GermanStemFilter.cs			\
	$(lucenedir)/Analysis/DE/GermanStemmer.cs			\
	$(lucenedir)/Analysis/DE/WordlistLoader.cs			\
	$(lucenedir)/Analysis/RU/RussianAnalyzer.cs			\
	$(lucenedir)/Analysis/RU/RussianCharsets.cs			\
	$(lucenedir)/Analysis/RU/RussianLetterTokenizer.cs		\
	$(lucenedir)/Analysis/RU/RussianLowerCaseFilter.cs		\
	$(lucenedir)/Analysis/RU/RussianStemFilter.cs			\
	$(lucenedir)/Analysis/RU/RussianStemmer.cs			\
	$(lucenedir)/Analysis/Standard/CharStream.cs			\
	$(lucenedir)/Analysis/Standard/FastCharStream.cs		\
	$(lucenedir)/Analysis/Standard/ParseException.cs		\
	$(lucenedir)/Analysis/Standard/StandardAnalyzer.cs		\
	$(lucenedir)/Analysis/Standard/StandardFilter.cs		\
	$(lucenedir)/Analysis/Standard/StandardTokenizer.cs		\
	$(lucenedir)/Analysis/Standard/StandardTokenizerConstants.cs	\
	$(lucenedir)/Analysis/Standard/StandardTokenizerTokenManager.cs	\
	$(lucenedir)/Analysis/Standard/Token.cs				\
	$(lucenedir)/Analysis/Standard/TokenMgrError.cs			\
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
	$(lucenedir)/Search/Spans/NearSpans.cs				\
	$(lucenedir)/Search/Spans/SpanFirstQuery.cs			\
	$(lucenedir)/Search/Spans/SpanNearQuery.cs			\
	$(lucenedir)/Search/Spans/SpanNotQuery.cs			\
	$(lucenedir)/Search/Spans/SpanOrQuery.cs			\
	$(lucenedir)/Search/Spans/SpanQuery.cs				\
	$(lucenedir)/Search/Spans/Spans.cs				\
	$(lucenedir)/Search/Spans/SpanScorer.cs				\
	$(lucenedir)/Search/Spans/SpanTermQuery.cs			\
	$(lucenedir)/Search/Spans/SpanWeight.cs				\
	$(lucenedir)/Search/BooleanClause.cs				\
	$(lucenedir)/Search/BooleanQuery.cs				\
	$(lucenedir)/Search/BooleanScorer.cs				\
	$(lucenedir)/Search/CachingWrapperFilter.cs			\
	$(lucenedir)/Search/ConjunctionScorer.cs			\
	$(lucenedir)/Search/DateFilter.cs				\
	$(lucenedir)/Search/DefaultSimilarity.cs			\
	$(lucenedir)/Search/ExactPhraseScorer.cs			\
	$(lucenedir)/Search/Explanation.cs				\
	$(lucenedir)/Search/FieldCache.cs				\
	$(lucenedir)/Search/FieldCacheImpl.cs				\
	$(lucenedir)/Search/FieldDoc.cs					\
	$(lucenedir)/Search/FieldDocSortedHitQueue.cs			\
	$(lucenedir)/Search/FieldSortedHitQueue.cs			\
	$(lucenedir)/Search/Filter.cs					\
	$(lucenedir)/Search/FilteredQuery.cs				\
	$(lucenedir)/Search/FilteredTermEnum.cs				\
	$(lucenedir)/Search/FuzzyQuery.cs				\
	$(lucenedir)/Search/FuzzyTermEnum.cs				\
	$(lucenedir)/Search/HitCollector.cs				\
	$(lucenedir)/Search/HitQueue.cs					\
	$(lucenedir)/Search/Hits.cs					\
	$(lucenedir)/Search/IndexSearcher.cs				\
	$(lucenedir)/Search/MultiSearcher.cs				\
	$(lucenedir)/Search/MultiTermQuery.cs				\
	$(lucenedir)/Search/ParallelMultiSearcher.cs			\
	$(lucenedir)/Search/PhrasePositions.cs				\
	$(lucenedir)/Search/PhrasePrefixQuery.cs			\
	$(lucenedir)/Search/PhraseQuery.cs				\
	$(lucenedir)/Search/PhraseQueue.cs				\
	$(lucenedir)/Search/PhraseScorer.cs				\
	$(lucenedir)/Search/PrefixQuery.cs				\
	$(lucenedir)/Search/Query.cs					\
	$(lucenedir)/Search/QueryFilter.cs				\
	$(lucenedir)/Search/QueryTermVector.cs				\
	$(lucenedir)/Search/RangeQuery.cs				\
	$(lucenedir)/Search/RemoteSearchable.cs				\
	$(lucenedir)/Search/ScoreDoc.cs					\
	$(lucenedir)/Search/ScoreDocComparator.cs			\
	$(lucenedir)/Search/Scorer.cs					\
	$(lucenedir)/Search/Searchable.cs				\
	$(lucenedir)/Search/Searcher.cs					\
	$(lucenedir)/Search/Similarity.cs				\
	$(lucenedir)/Search/SloppyPhraseScorer.cs			\
	$(lucenedir)/Search/Sort.cs					\
	$(lucenedir)/Search/SortComparator.cs				\
	$(lucenedir)/Search/SortComparatorSource.cs			\
	$(lucenedir)/Search/SortField.cs				\
	$(lucenedir)/Search/TermQuery.cs				\
	$(lucenedir)/Search/TermScorer.cs				\
	$(lucenedir)/Search/TopDocs.cs					\
	$(lucenedir)/Search/TopFieldDocs.cs				\
	$(lucenedir)/Search/Weight.cs					\
	$(lucenedir)/Search/WildcardQuery.cs				\
	$(lucenedir)/Search/WildcardTermEnum.cs				\
	$(lucenedir)/QueryParser/CharStream.cs				\
	$(lucenedir)/QueryParser/FastCharStream.cs			\
	$(lucenedir)/QueryParser/MultiFieldQueryParser.cs		\
	$(lucenedir)/QueryParser/ParseException.cs			\
	$(lucenedir)/QueryParser/QueryParser.cs				\
	$(lucenedir)/QueryParser/QueryParserConstants.cs		\
	$(lucenedir)/QueryParser/QueryParserTokenManager.cs		\
	$(lucenedir)/QueryParser/Token.cs				\
	$(lucenedir)/QueryParser/TokenMgrError.cs

LUCENE_CSFILES = $(LUCENE_1_4_CSFILES)

############################################################

DAEMON_DLL_TARGET = BeagleDaemonLib.dll

DAEMON_DLL_CSFLAGS = -target:library $(BEAGLE_DEFINES)

fsqueryabledir = $(srcdir)/FileSystemQueryable
FILE_SYSTEM_QUERYABLE_CSFILES =					\
	$(fsqueryabledir)/CrawlTask.cs				\
	$(fsqueryabledir)/DirectoryIndexableGenerator.cs	\
	$(fsqueryabledir)/FileNameFilter.cs			\
	$(fsqueryabledir)/FileSystemModel.cs			\
	$(fsqueryabledir)/FileSystemQueryable.cs		\
	$(fsqueryabledir)/FileSystemWatcherBackend.cs		\
	$(fsqueryabledir)/IFileEventBackend.cs			\
	$(fsqueryabledir)/InotifyBackend.cs

webhistqueryable = $(srcdir)/WebHistoryQueryable
WEB_HISTORY_QUERYABLE_CSFILES =				\
	$(webhistqueryable)/WebHistoryQueryable.cs

gaimlogqueryable = $(srcdir)/GaimLogQueryable
GAIM_LOG_QUERYABLE_CSFILES =				\
	$(gaimlogqueryable)/GaimLogQueryable.cs

edsqueryable = $(srcdir)/EvolutionDataServerQueryable
EDS_QUERYABLE_CSFILES =                                 \
	$(edsqueryable)/EvolutionDataServerQueryable.cs 

tomboyqueryable = $(srcdir)/TomboyQueryable
TOMBOY_QUERYABLE_CSFILES =				\
	$(tomboyqueryable)/TomboyQueryable.cs

evomailqueryable = $(srcdir)/EvolutionMailDriver
EVO_MAIL_QUERYABLE_CSFILES =					\
	$(evomailqueryable)/EvolutionMailDriver.cs		\
	$(evomailqueryable)/EvolutionMailIndexableGenerator.cs	\
	$(evomailqueryable)/MailCrawler.cs

blamqueryable = $(srcdir)/BlamQueryable
BLAM_QUERYABLE_CSFILES =				\
	$(blamqueryable)/BlamQueryable.cs

lifereaqueryable = $(srcdir)/LifereaQueryable
LIFEREA_QUERYABLE_CSFILES =				\
	$(lifereaqueryable)/LifereaQueryable.cs


launcherqueryable = $(srcdir)/LauncherQueryable
LAUNCHER_QUERYABLE_CSFILES =				\
	$(launcherqueryable)/LauncherQueryable.cs



#	$(EVO_MAIL_DRIVER_CSFILES)		\
#		$(TOMBOY_QUERYABLE_CSFILES)		\
#

#calendarqueryable = $(srcdir)/CalendarQueryable
#CALENDAR_QUERYABLE_CSFILES =				\
#	$(calendarqueryable)/CalendarQueryable.cs	\
#	$(calendarqueryable)/ICalParser/*.cs

DAEMON_DLL_CSFILES = 						\
	$(LUCENE_CSFILES)					\
	$(FILE_SYSTEM_QUERYABLE_CSFILES)			\
	$(GAIM_LOG_QUERYABLE_CSFILES)				\
	$(BLAM_QUERYABLE_CSFILES)				\
	$(LIFEREA_QUERYABLE_CSFILES)				\
	$(EVO_MAIL_QUERYABLE_CSFILES)				\
	$(WEB_HISTORY_QUERYABLE_CSFILES)			\
	$(LAUNCHER_QUERYABLE_CSFILES)				\
	$(TOMBOY_QUERYABLE_CSFILES)				\
	$(srcdir)/BugzillaDriver.cs 				\
	$(srcdir)/DBusisms.cs					\
	$(srcdir)/FactoryImpl.cs				\
	$(srcdir)/FileAttributes.cs				\
	$(srcdir)/FileAttributesStore.cs			\
	$(srcdir)/FileAttributesStore_ExtendedAttribute.cs	\
	$(srcdir)/FileAttributesStore_Mixed.cs			\
	$(srcdir)/FileAttributesStore_Sqlite.cs			\
	$(srcdir)/FilterFactory.cs				\
	$(srcdir)/FilteredIndexable.cs				\
	$(srcdir)/GoogleDriver.cs				\
	$(srcdir)/HitRegulator.cs				\
	$(srcdir)/IDBusObject.cs				\
	$(srcdir)/IFileAttributesStore.cs			\
	$(srcdir)/IIndexer.cs					\
	$(srcdir)/IQueryResult.cs				\
	$(srcdir)/IQueryWorker.cs               		\
	$(srcdir)/IQueryable.cs					\
	$(srcdir)/LuceneDriver.cs				\
	$(srcdir)/LuceneQueryable.cs				\
	$(srcdir)/QueryBody.cs					\
	$(srcdir)/QueryDriver.cs				\
	$(srcdir)/QueryImpl.cs					\
	$(srcdir)/QueryResult.cs				\
	$(srcdir)/Queryable.cs					\
	$(srcdir)/QueryableFlavor.cs				\
	$(srcdir)/RemoteControlImpl.cs				\
	$(srcdir)/RemoteIndexerProxy.cs				\
	$(srcdir)/Shutdown.cs					\
	$(srcdir)/SnippetFu.cs					\
	$(srcdir)/TextCache.cs

DAEMON_DLL_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll	\
	./$(PLUGIN_TARGET)

DAEMON_DLL_ASSEMBLIES = 			\
	$(BEAGLED_LIBS)                 	\
	$(DAEMON_DLL_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:ICSharpCode.SharpZipLib.dll		\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:System.Web


if ENABLE_EVO_SHARP
DAEMON_DLL_CSFLAGS += -define:ENABLE_EVO_SHARP
DAEMON_DLL_CSFILES += 	$(EDS_QUERYABLE_CSFILES)
DAEMON_DLL_ASSEMBLIES += $(EVO_SHARP_LIBS)
endif

authenticators = $(srcdir)/NetworkAuthenticators
AUTHENTICATORS_CSFILES =				\
	$(authenticators)/BasicAuthenticator.cs

if ENABLE_NETWORK
DAEMON_DLL_CSFILES += 					\
	$(AUTHENTICATORS_CSFILES) 			\
	$(srcdir)/NetworkHandler.cs			\
	$(srcdir)/NetworkService.cs			\
	$(srcdir)/NetworkDriver.cs
if ENABLE_RENDEZVOUS
DAEMON_DLL_CSFLAGS += -define:ENABLE_RENDEZVOUS
DAEMON_DLL_ASSEMBLIES += $(RENDEZVOUS_LIBS)
endif
endif

#if ENABLE_MOZILLA_BACKEND
#mozillaqueryable = $(srcdir)/MozillaQueryable
#MOZILLA_QUERYABLE_CSFILES =				\
#	$(mozillaqueryable)/MozillaQueryable.cs
#DAEMON_DLL_CSFILES +=					\
#	$(MOZILLA_QUERYABLE_CSFILES)
#endif


$(DAEMON_DLL_TARGET): $(DAEMON_DLL_CSFILES) $(DAEMON_DLL_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(DAEMON_DLL_CSFLAGS) $(DAEMON_DLL_CSFILES) $(DAEMON_DLL_ASSEMBLIES)


############################################################


DAEMON_WRAPPER = beagled
DAEMON_WRAPPER_IN = beagled.in
DAEMON_TARGET = BeagleDaemon.exe

$(DAEMON_WRAPPER): $(srcdir)/$(DAEMON_WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(DAEMON_TARGET)|g" < $(srcdir)/$(DAEMON_WRAPPER_IN) > $@
	chmod +x $(DAEMON_WRAPPER)


DAEMON_CSFLAGS =				\
	-target:exe				\
	$(BEAGLE_DEFINES)

#	-main:BeagleDaemon.BeagleDaemon

DAEMON_CSFILES =				\
	$(srcdir)/BeagleDaemon.cs		\
	$(srcdir)/IndexHelperFu.cs		\
	$(srcdir)/RemoteIndexer.cs


DAEMON_LOCAL_ASSEMBLIES =			\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

DAEMON_ASSEMBLIES =				\
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

if ENABLE_NETWORK
DAEMON_CSFLAGS += -define:ENABLE_NETWORK
endif

$(DAEMON_TARGET): $(DAEMON_CSFILES) $(DAEMON_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(DAEMON_CSFLAGS) $(DAEMON_CSFILES) $(DAEMON_ASSEMBLIES)


############################################################

INDEX_HELPER_WRAPPER = beagled-index-helper
INDEX_HELPER_TARGET = IndexHelper.exe

$(INDEX_HELPER_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_HELPER_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(INDEX_HELPER_WRAPPER)

INDEX_HELPER_CSFLAGS = -target:exe $(BEAGLE_DEFINES)

indexhelperdir = $(srcdir)/IndexHelper

INDEX_HELPER_CSFILES =	\
	$(indexhelperdir)/IndexHelper.cs	\
	$(indexhelperdir)/RemoteIndexerImpl.cs	

INDEX_HELPER_LOCAL_ASSEMBLIES =	\
	../Util/Util.dll		\
	./$(DAEMON_DLL_TARGET)

INDEX_HELPER_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(INDEX_HELPER_TARGET): $(INDEX_HELPER_CSFILES) $(INDEX_HELPER_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(INDEX_HELPER_CSFLAGS) $(INDEX_HELPER_CSFILES) $(INDEX_HELPER_ASSEMBLIES)

############################################################

EXTRACT_CONTENT_WRAPPER = beagle-extract-content
EXTRACT_CONTENT_TARGET = ExtractContent.exe

$(EXTRACT_CONTENT_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(EXTRACT_CONTENT_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(EXTRACT_CONTENT_WRAPPER)


EXTRACT_CONTENT_CSFLAGS = \
	-target:exe

EXTRACT_CONTENT_CSFILES = \
	$(srcdir)/ExtractContent.cs

EXTRACT_CONTENT_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

EXTRACT_CONTENT_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(EXTRACT_CONTENT_TARGET): $(EXTRACT_CONTENT_CSFILES) $(EXTRACT_CONTENT_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(EXTRACT_CONTENT_CSFLAGS) $(EXTRACT_CONTENT_CSFILES) $(EXTRACT_CONTENT_ASSEMBLIES)

############################################################

MASTER_DELETE_WRAPPER = beagle-master-delete-button
MASTER_DELETE_TARGET = MasterDelete.exe

$(MASTER_DELETE_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(MASTER_DELETE_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(MASTER_DELETE_WRAPPER)


MASTER_DELETE_CSFLAGS =	\
	-target:exe

MASTER_DELETE_CSFILES =	\
	$(srcdir)/MasterDelete.cs

MASTER_DELETE_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

MASTER_DELETE_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(MASTER_DELETE_TARGET): $(MASTER_DELETE_CSFILES) $(MASTER_DELETE_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(MASTER_DELETE_CSFLAGS) $(MASTER_DELETE_CSFILES) $(MASTER_DELETE_ASSEMBLIES)

############################################################

SNIPPET_TEST_WRAPPER = beagle-snippet-test
SNIPPET_TEST_TARGET = SnippetTest.exe

$(SNIPPET_TEST_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(SNIPPET_TEST_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(SNIPPET_TEST_WRAPPER)

SNIPPET_TEST_CSFLAGS = \
	-target:exe

SNIPPET_TEST_CSFILES = \
	$(srcdir)/SnippetTest.cs

SNIPPET_TEST_LOCAL_ASSEMBLIES =	\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

SNIPPET_TEST_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(SNIPPET_TEST_TARGET): $(SNIPPET_TEST_CSFILES) $(SNIPPET_TEST_LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(SNIPPET_TEST_CSFLAGS) $(SNIPPET_TEST_CSFILES) $(SNIPPET_TEST_ASSEMBLIES)

############################################################

ALL_TARGETS = \
	$(PLUGIN_TARGET)		\
	$(DAEMON_DLL_TARGET)		\
	$(DAEMON_TARGET)		\
	$(INDEX_HELPER_TARGET)		\
	$(INDEX_HELPER_WRAPPER)		\
	$(EXTRACT_CONTENT_TARGET)	\
	$(MASTER_DELETE_TARGET)		\
	$(SNIPPET_TEST_TARGET)


all: $(ALL_TARGETS)

install-data-local: $(PLUGIN_TARGET) $(DAEMON_DLL_TARGET) $(DAEMON_TARGET) $(EXTRACT_CONTENT_TARGET) $(SNIPPET_TEST_TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(PLUGIN_TARGET) $(DESTDIR)$(pkglibdir)/$(PLUGIN_TARGET)
	$(INSTALL_DATA) $(DAEMON_DLL_TARGET) $(DESTDIR)$(pkglibdir)/$(DAEMON_DLL_TARGET)
	$(INSTALL_DATA) $(DAEMON_TARGET) $(DESTDIR)$(pkglibdir)/$(DAEMON_TARGET)
	$(INSTALL_DATA) $(INDEX_HELPER_TARGET) $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_TARGET)
	$(INSTALL_SCRIPT) $(INDEX_HELPER_WRAPPER) $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_WRAPPER)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(PLUGIN_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(DAEMON_DLL_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(DAEMON_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_WRAPPER)

bin_SCRIPTS = \
	$(DAEMON_WRAPPER)

noinst_SCRIPTS=	\
	$(EXTRACT_CONTENT_WRAPPER)	\
	$(MASTER_DELETE_WRAPPER)	\
	$(SNIPPET_TEST_WRAPPER)

COND_CS_FILES = \
	$(AUTHENTICATORS_CSFILES) 	\
	$(srcdir)/NetworkHandler.cs	\
	$(srcdir)/NetworkService.cs	\
	$(srcdir)/NetworkDriver.cs	\
	$(MOZILLA_QUERYABLE_CSFILES)

EXTRA_DIST = \
	$(PLUGIN_CSFILES)		\
	$(DAEMON_DLL_CSFILES)		\
	$(DAEMON_CSFILES)		\
	$(EXTRACT_CONTENT_CSFILES)	\
	$(SNIPPET_TEST_CSFILES)		\
	$(INDEX_HELPER_CSFILES)		\
	$(MASTER_DELETE_CSFILES)	\
	$(EXTSTR_IN)			\
	$(WRAPPER_IN)   		\
	$(DAEMON_WRAPPER_IN)		\
	$(COND_CS_FILES)

CLEANFILES = \
	$(EXTSTR)			\
	$(PLUGIN_TARGET)       		\
	$(PLUGIN_TARGET).mdb   		\
	$(DAEMON_DLL_TARGET)		\
	$(DAEMON_DLL_TARGET).mdb	\
	$(DAEMON_TARGET)       		\
	$(DAEMON_TARGET).mdb   		\
	$(DAEMON_WRAPPER)      		\
	$(INDEX_HELPER_TARGET)		\
	$(INDEX_HELPER_TARGET).mdb	\
	$(INDEX_HELPER_WRAPPER)		\
	$(EXTRACT_CONTENT_TARGET)	\
	$(EXTRACT_CONTENT_TARGET).mdb	\
	$(EXTRACT_CONTENT_WRAPPER)	\
	$(MASTER_DELETE_TARGET)		\
	$(MASTER_DELETE_TARGET).mdb	\
	$(MASTER_DELETE_WRAPPER)	\
	$(SNIPPET_TEST_TARGET)		\
	$(SNIPPET_TEST_TARGET).mdb	\
	$(SNIPPET_TEST_WRAPPER)
