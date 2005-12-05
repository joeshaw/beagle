
# Warnings we don't want to see
# 0067 = event defined but not used
# 0169 = The private method/field 'foo' is never used
# 0164 = This label has not been referenced

CSC = mcs -debug -nowarn:0067,0619,0169,0164

BACKENDDIR = $(pkglibdir)/Backends

PLUGIN_TARGET = BeagleDaemonPlugins.dll

PLUGIN_CSFLAGS = -target:library

PLUGIN_CSFILES =			\
	$(srcdir)/Flavor.cs		\
	$(srcdir)/Filter.cs

PLUGIN_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll

PLUGIN_ASSEMBLIES =				\
	-r:Mono.Posix				\
	$(BEAGLED_LIBS)                 	\
	$(PLUGIN_LOCAL_ASSEMBLIES:%=-r:%)


$(PLUGIN_TARGET): $(PLUGIN_CSFILES) $(PLUGIN_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(PLUGIN_CSFLAGS) $(PLUGIN_CSFILES) $(PLUGIN_ASSEMBLIES)

############################################################

WRAPPER_IN = wrapper.in
WRAPPER_SED = sed 					\
	-e "s|\@prefix\@|$(prefix)|g"			\
	-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
	-e "s|\@libdir\@|$(libdir)|g"			\
	-e "s|\@gacprefix\@|$(GAC_PREFIX)|g"		\
	-e "s|\@bash\@|$(BASH)|"

############################################################

lucenedir = $(srcdir)/Lucene.Net

LUCENE_1_9_CSFILES =							\
	$(lucenedir)/AssemblyInfo.cs					\
	$(lucenedir)/LucenePackage.cs					\
	$(lucenedir)/SupportClass.cs					\
	$(lucenedir)/Store/BufferedIndexInput.cs			\
	$(lucenedir)/Store/BufferedIndexOutput.cs			\
	$(lucenedir)/Store/Directory.cs					\
	$(lucenedir)/Store/FSDirectory.cs				\
	$(lucenedir)/Store/IndexInput.cs				\
	$(lucenedir)/Store/IndexOutput.cs				\
	$(lucenedir)/Store/InputStream.cs				\
	$(lucenedir)/Store/Lock.cs					\
	$(lucenedir)/Store/MMapDirectory.cs				\
	$(lucenedir)/Store/OutputStream.cs				\
	$(lucenedir)/Store/RAMDirectory.cs				\
	$(lucenedir)/Store/RAMFile.cs					\
	$(lucenedir)/Store/RAMInputStream.cs				\
	$(lucenedir)/Store/RAMOutputStream.cs				\
	$(lucenedir)/Util/BitVector.cs					\
	$(lucenedir)/Util/Constants.cs					\
	$(lucenedir)/Util/Parameter.cs					\
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
	$(lucenedir)/Index/SegmentTermPositionVector.cs			\
	$(lucenedir)/Index/SegmentTermVector.cs				\
	$(lucenedir)/Index/Term.cs					\
	$(lucenedir)/Index/TermBuffer.cs				\
	$(lucenedir)/Index/TermDocs.cs					\
	$(lucenedir)/Index/TermEnum.cs					\
	$(lucenedir)/Index/TermFreqVector.cs				\
	$(lucenedir)/Index/TermInfo.cs					\
	$(lucenedir)/Index/TermInfosReader.cs				\
	$(lucenedir)/Index/TermInfosWriter.cs				\
	$(lucenedir)/Index/TermPositions.cs				\
	$(lucenedir)/Index/TermPositionVector.cs			\
	$(lucenedir)/Index/TermVectorOffsetInfo.cs			\
	$(lucenedir)/Index/TermVectorsReader.cs				\
	$(lucenedir)/Index/TermVectorsWriter.cs				\
	$(lucenedir)/Document/DateField.cs				\
	$(lucenedir)/Document/DateTools.cs				\
	$(lucenedir)/Document/Document.cs				\
	$(lucenedir)/Document/Field.cs					\
	$(lucenedir)/Document/NumberTools.cs				\
	$(lucenedir)/Analysis/DE/WordlistLoader.cs			\
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
	$(lucenedir)/Search/BooleanScorer2.cs				\
	$(lucenedir)/Search/CachingWrapperFilter.cs			\
	$(lucenedir)/Search/ConjunctionScorer.cs			\
	$(lucenedir)/Search/DateFilter.cs				\
	$(lucenedir)/Search/DefaultSimilarity.cs			\
	$(lucenedir)/Search/DisjunctionSumScorer.cs			\
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
	$(lucenedir)/Search/MultiPhraseQuery.cs				\
	$(lucenedir)/Search/MultiSearcher.cs				\
	$(lucenedir)/Search/MultiTermQuery.cs				\
	$(lucenedir)/Search/NonMatchingScorer.cs			\
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
	$(lucenedir)/Search/RangeFilter.cs				\
	$(lucenedir)/Search/RangeQuery.cs				\
	$(lucenedir)/Search/RemoteSearchable.cs				\
	$(lucenedir)/Search/ReqExclScorer.cs				\
	$(lucenedir)/Search/ReqOptSumScorer.cs				\
	$(lucenedir)/Search/ScoreDoc.cs					\
	$(lucenedir)/Search/ScoreDocComparator.cs			\
	$(lucenedir)/Search/Scorer.cs					\
	$(lucenedir)/Search/Searchable.cs				\
	$(lucenedir)/Search/Searcher.cs					\
	$(lucenedir)/Search/Similarity.cs				\
	$(lucenedir)/Search/SimilarityDelegator.cs			\
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
	$(lucenedir)/QueryParser/Precedence/CharStream.cs		\
	$(lucenedir)/QueryParser/Precedence/ParseException.cs		\
	$(lucenedir)/QueryParser/Precedence/FastCharStream.cs		\
	$(lucenedir)/QueryParser/Precedence/PrecedenceQueryParserConstants.cs\
	$(lucenedir)/QueryParser/Precedence/Token.cs			\
	$(lucenedir)/QueryParser/Precedence/PrecedenceQueryParserTokenManager.cs\
	$(lucenedir)/QueryParser/Precedence/PrecedenceQueryParser.cs	\
	$(lucenedir)/QueryParser/Precedence/TokenMgrError.cs		\
	$(lucenedir)/QueryParser/CharStream.cs				\
	$(lucenedir)/QueryParser/FastCharStream.cs			\
	$(lucenedir)/QueryParser/MultiFieldQueryParser.cs		\
	$(lucenedir)/QueryParser/ParseException.cs			\
	$(lucenedir)/QueryParser/QueryParser.cs				\
	$(lucenedir)/QueryParser/QueryParserConstants.cs		\
	$(lucenedir)/QueryParser/QueryParserTokenManager.cs		\
	$(lucenedir)/QueryParser/Token.cs				\
	$(lucenedir)/QueryParser/TokenMgrError.cs

LUCENE_CSFILES = $(LUCENE_1_9_CSFILES)

############################################################

sqlitedir = $(srcdir)/Mono.Data.SqliteClient

SQLITE_CSFILES =					\
	$(sqlitedir)/SqliteCommand.cs			\
	$(sqlitedir)/SqliteConnection.cs		\
	$(sqlitedir)/Sqlite.cs				\
	$(sqlitedir)/SqliteDataAdapter.cs		\
	$(sqlitedir)/SqliteDataReader.cs		\
	$(sqlitedir)/SqliteException.cs			\
	$(sqlitedir)/SqliteParameterCollection.cs	\
	$(sqlitedir)/SqliteParameter.cs			\
	$(sqlitedir)/SqliteRowUpdatedEventArgs.cs	\
	$(sqlitedir)/SqliteRowUpdatedEventHandler.cs	\
	$(sqlitedir)/SqliteRowUpdatingEventArgs.cs	\
	$(sqlitedir)/SqliteRowUpdatingEventHandler.cs	\
	$(sqlitedir)/SqliteTransaction.cs

############################################################

DAEMON_DLL_TARGET = BeagleDaemonLib.dll

DAEMON_DLL_CSFLAGS = -target:library $(BEAGLE_DEFINES)

fsqueryabledir = $(srcdir)/FileSystemQueryable
FILE_SYSTEM_QUERYABLE_CSFILES =					\
	$(fsqueryabledir)/DirectoryModel.cs			\
	$(fsqueryabledir)/DirectoryState.cs			\
	$(fsqueryabledir)/TreeCrawlTask.cs			\
	$(fsqueryabledir)/FileCrawlTask.cs			\
	$(fsqueryabledir)/DirectoryIndexableGenerator.cs	\
	$(fsqueryabledir)/FileNameFilter.cs			\
	$(fsqueryabledir)/LuceneNameResolver.cs			\
	$(fsqueryabledir)/FileSystemQueryable.cs		\
	$(fsqueryabledir)/IFileEventBackend.cs			\
	$(fsqueryabledir)/InotifyBackend.cs			\
	$(fsqueryabledir)/FileSystemWatcherBackend.cs


gaimlogqueryable = $(srcdir)/GaimLogQueryable
GAIM_LOG_QUERYABLE_CSFILES =				\
	$(gaimlogqueryable)/GaimLogQueryable.cs

tomboyqueryable = $(srcdir)/TomboyQueryable
TOMBOY_QUERYABLE_CSFILES =				\
	$(tomboyqueryable)/TomboyQueryable.cs

evomailqueryable = $(srcdir)/EvolutionMailDriver
EVO_MAIL_QUERYABLE_CSFILES =					\
	$(evomailqueryable)/EvolutionMailDriver.cs		\
	$(evomailqueryable)/EvolutionMailIndexableGenerator.cs	\
	$(evomailqueryable)/MailCrawler.cs

kmailqueryable = $(srcdir)/KMailQueryable
KMAIL_QUERYABLE_CSFILES =				\
	$(kmailqueryable)/KMailQueryable.cs		\
	$(kmailqueryable)/KMailIndexableGenerator.cs	\
	$(kmailqueryable)/KMailIndexer.cs

blamqueryable = $(srcdir)/BlamQueryable
BLAM_QUERYABLE_CSFILES =				\
	$(blamqueryable)/BlamQueryable.cs

lifereaqueryable = $(srcdir)/LifereaQueryable
LIFEREA_QUERYABLE_CSFILES =				\
	$(lifereaqueryable)/LifereaQueryable.cs

akregatorqueryable = $(srcdir)/AkregatorQueryable
AKREGATOR_QUERYABLE_CSFILES =				\
	$(akregatorqueryable)/AkregatorQueryable.cs

konqhistoryqueryable = $(srcdir)/KonqHistoryQueryable
KONQHISTORY_QUERYABLE_CSFILES =				\
	$(konqhistoryqueryable)/KonqQueryable.cs

kopetequeryable = $(srcdir)/KopeteQueryable
KOPETE_QUERYABLE_CSFILES =				\
	$(kopetequeryable)/KopeteQueryable.cs 	 	\
	$(kopetequeryable)/KopeteCrawler.cs

indexingservicequeryable = $(srcdir)/IndexingServiceQueryable
INDEXING_SERVICE_QUERYABLE_CSFILES =			\
	$(indexingservicequeryable)/IndexingServiceQueryable.cs

DAEMON_DLL_CSFILES = 						\
	$(LUCENE_CSFILES)					\
	$(EVO_MAIL_QUERYABLE_CSFILES)				\
	$(KMAIL_QUERYABLE_CSFILES)				\
	$(FILE_SYSTEM_QUERYABLE_CSFILES)			\
	$(GAIM_LOG_QUERYABLE_CSFILES)				\
	$(INDEXING_SERVICE_QUERYABLE_CSFILES)			\
	$(TOMBOY_QUERYABLE_CSFILES)				\
	$(BLAM_QUERYABLE_CSFILES)				\
	$(LIFEREA_QUERYABLE_CSFILES)				\
	$(AKREGATOR_QUERYABLE_CSFILES)				\
	$(KONQHISTORY_QUERYABLE_CSFILES)				\
	$(KOPETE_QUERYABLE_CSFILES)				\
	$(srcdir)/FileAttributes.cs				\
	$(srcdir)/FileAttributesStore.cs			\
	$(srcdir)/FileAttributesStore_ExtendedAttribute.cs	\
	$(srcdir)/FileAttributesStore_Mixed.cs			\
	$(srcdir)/FileAttributesStore_Sqlite.cs			\
	$(srcdir)/FilterFactory.cs				\
	$(srcdir)/GConfThreadHelper.cs				\
	$(srcdir)/GoogleDriver.cs				\
	$(srcdir)/IFileAttributesStore.cs			\
	$(srcdir)/IIndexer.cs					\
	$(srcdir)/IndexSynchronization.cs			\
	$(srcdir)/IndexerReceipts.cs				\
	$(srcdir)/IndexerRequest.cs				\
	$(srcdir)/IQueryResult.cs				\
	$(srcdir)/IQueryWorker.cs               		\
	$(srcdir)/IQueryable.cs					\
	$(srcdir)/LuceneBitArray.cs				\
	$(srcdir)/LuceneCommon.cs				\
	$(srcdir)/LuceneIndexingDriver.cs			\
	$(srcdir)/LuceneQueryingDriver.cs			\
	$(srcdir)/LuceneFileQueryable.cs			\
	$(srcdir)/LuceneQueryable.cs				\
	$(srcdir)/NoiseFilter.cs				\
	$(srcdir)/PropertyKeywordFu.cs				\
	$(srcdir)/QueryDriver.cs				\
	$(srcdir)/QueryExecutor.cs				\
	$(srcdir)/QueryResult.cs				\
	$(srcdir)/QueryStringParser.cs				\
	$(srcdir)/Queryable.cs					\
	$(srcdir)/QueryableFlavor.cs				\
	$(srcdir)/RemoteControlExecutors.cs			\
	$(srcdir)/RemoteIndexerRequest.cs			\
	$(srcdir)/RemoteIndexerResponse.cs			\
	$(srcdir)/Server.cs					\
	$(srcdir)/Shutdown.cs					\
	$(srcdir)/SnippetExecutor.cs				\
	$(srcdir)/SnippetFu.cs					\
	$(srcdir)/StaticQueryable.cs				\
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

if USE_LOCAL_SQLITE
DAEMON_DLL_CSFILES += $(SQLITE_CSFILES)
DAEMON_DLL_ASSEMBLIES += -r:System.Data
else
DAEMON_DLL_ASSEMBLIES += -r:Mono.Data.SqliteClient
endif

#if ENABLE_MOZILLA_BACKEND
#mozillaqueryable = $(srcdir)/MozillaQueryable
#MOZILLA_QUERYABLE_CSFILES =				\
#	$(mozillaqueryable)/MozillaQueryable.cs
#DAEMON_DLL_CSFILES +=					\
#	$(MOZILLA_QUERYABLE_CSFILES)
#endif

if ENABLE_WEBSERVICES
DAEMON_DLL_CSFILES += \
	$(webservicesdir)/WebServiceProxy.cs	\
	$(webservicesdir)/NetBeagleHandler.cs	\
	$(webservicesdir)/NetBeagleQueryable.cs	
DAEMON_DLL_ASSEMBLIES += -r:System.Web.Services	
endif


$(DAEMON_DLL_TARGET): $(DAEMON_DLL_CSFILES) $(DAEMON_DLL_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(DAEMON_DLL_CSFLAGS) $(DAEMON_DLL_CSFILES) $(DAEMON_DLL_ASSEMBLIES)

############################################################

###
### Pluggable backend support
###

BACKEND_CSFLAGS = -target:library
BACKEND_LOCAL_ASSEMBLIES =			\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(DAEMON_DLL_TARGET)

BACKEND_ASSEMBLIES =				\
	$(BACKEND_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:ICSharpCode.SharpZipLib.dll

###
### Evolution Data Server backend
###

edsqueryable = $(srcdir)/EvolutionDataServerQueryable
EDS_QUERYABLE_CSFILES =                                 \
	$(edsqueryable)/BookContainer.cs		\
	$(edsqueryable)/CalContainer.cs			\
	$(edsqueryable)/Container.cs			\
	$(edsqueryable)/EvolutionDataServerQueryable.cs	\
	$(edsqueryable)/SourcesHandler.cs

EDS_DLL_TARGET = EvolutionDataServer.dll
EDS_DLL_CSFLAGS = $(BACKEND_CSFLAGS)
EDS_DLL_CSFILES = $(EDS_QUERYABLE_CSFILES)
EDS_DLL_ASSEMBLIES = $(BACKEND_ASSEMBLIES) $(EVO_SHARP_LIBS)

$(EDS_DLL_TARGET): $(EDS_DLL_CSFILES) $(DAEMON_DLL_LOCAL_ASSEMBLIES) $(DAEMON_DLL_TARGET)
	$(CSC) -out:$@ $(EDS_DLL_CSFLAGS) $(EDS_DLL_CSFILES) -r:$(DAEMON_DLL_TARGET) $(DAEMON_DLL_ASSEMBLIES)

############################################################

###
### Web Services
###

webservicesdir = $(srcdir)/WebServices
xspdir = $(srcdir)/WebServices/WebServer

WEBSERVICES_XSP_TARGET = BeagleXsp.dll

WEBSERVICES_XSP_CSFLAGS = -target:library 

WEBSERVICES_XSP_ASSEMBLIES = 			\
	-r:System 				\
	-r:System.Web 				\
	-r:System.Runtime.Remoting	

WEBSERVICES_XSP_CSFILES =  			\
	$(xspdir)/IApplicationHost.cs		\
	$(xspdir)/MonoWorkerRequest.cs		\
	$(xspdir)/Tracing.cs			\
	$(xspdir)/ApplicationServer.cs		\
	$(xspdir)/LingeringNetworkStream.cs	\
	$(xspdir)/BaseApplicationHost.cs	\
	$(xspdir)/BaseRequestBroker.cs		\
	$(xspdir)/IWebSource.cs			\
	$(xspdir)/server.cs			\
	$(xspdir)/InitialWorkerRequest.cs	\
	$(xspdir)/XSPApplicationHost.cs		\
	$(xspdir)/XSPWorkerRequest.cs		\
	$(xspdir)/AssemblyInfo.cs

$(WEBSERVICES_XSP_TARGET): $(WEBSERVICES_XSP_CSFILES) 
	$(CSC) -out:$@ $(WEBSERVICES_XSP_CSFLAGS) $(WEBSERVICES_XSP_CSFILES) $(WEBSERVICES_XSP_ASSEMBLIES)


WEBSERVICES_BACKEND_TARGET = BeagleWebServicesBackEnd.dll

WEBSERVICES_BACKEND_LOCAL_ASSEMBLIES =	\
	../Util/Util.dll		\
	../images/Images.dll		\
	../BeagleClient/Beagle.dll      \
	../Tiles/Tiles.dll		\
	BeagleDaemonPlugins.dll 	\
	BeagleDaemonLib.dll			\
	$(WEBSERVICES_XSP_TARGET)

WEBSERVICES_BACKEND_ASSEMBLIES = \
	$(WEBSERVICES_BACKEND_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:System					\
	-r:System.Web 				\
	-r:System.Runtime.Remoting	\
	$(BEAGLED_LIBS)

WEBSERVICES_BACKEND_CSFILES = \
	$(webservicesdir)/ExternalAccessFilter.cs	\
	$(webservicesdir)/WebBackEnd.cs 	\
	$(webservicesdir)/WebServiceBackEnd.cs

WEBSERVICES_CSFLAGS = -target:library -noconfig

$(WEBSERVICES_BACKEND_TARGET): $(WEBSERVICES_BACKEND_CSFILES) $(WEBSERVICES_BACKEND_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(WEBSERVICES_CSFLAGS) $(WEBSERVICES_BACKEND_CSFILES) $(WEBSERVICES_BACKEND_ASSEMBLIES)

WEBSERVICES_FRONTEND_TARGET = BeagleWebServicesFrontEnd.dll
WEBSERVICES_FRONTEND_CSFILES = \
	$(webservicesdir)/WebFrontEnd.cs	\
	$(webservicesdir)/WebServiceFrontEnd.cs

WEBSERVICES_FRONTEND_ASSEMBLIES = 	\
	$(WEBSERVICES_BACKEND_ASSEMBLIES) 	\
	-r:$(WEBSERVICES_BACKEND_TARGET) 	\
	-r:System.Web.Services

$(WEBSERVICES_FRONTEND_TARGET): $(WEBSERVICES_FRONTEND_CSFILES) $(WEBSERVICES_BACKEND_TARGET) 
	$(CSC) -out:$@ $(WEBSERVICES_CSFLAGS) $(WEBSERVICES_FRONTEND_CSFILES) $(WEBSERVICES_FRONTEND_ASSEMBLIES)

WEBSERVICES_RESOURCES = 			\
	$(webservicesdir)/search.aspx	\
	$(webservicesdir)/search.asmx	\
	$(webservicesdir)/web.config

WEBSERVICES_RESOURCES_DIST = 		\
	$(webservicesdir)/search.aspx	\
	$(webservicesdir)/search.asmx	\
	$(webservicesdir)/web.config

XSP_DIR = $(pkgdatadir)/xsp
XSPBIN_DIR = $(XSP_DIR)/bin

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

DAEMON_CSFILES =				\
	$(srcdir)/BeagleDaemon.cs		\
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
	-r:ICSharpCode.SharpZipLib.dll

if ENABLE_WEBSERVICES
DAEMON_CSFLAGS += -define:ENABLE_WEBSERVICES

DAEMON_LOCAL_ASSEMBLIES += 		\
	BeagleDaemonPlugins.dll		\
	../images/Images.dll		\
	../Tiles/Tiles.dll		\
	$(WEBSERVICES_BACKEND_TARGET) 	\
	$(WEBSERVICES_FRONTEND_TARGET)	

DAEMON_ASSEMBLIES += -r:System.Runtime.Remoting
endif

$(DAEMON_TARGET): $(DAEMON_CSFILES) $(DAEMON_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(DAEMON_CSFLAGS) $(DAEMON_CSFILES) $(DAEMON_ASSEMBLIES)

############################################################
############################################################
############################################################

# This next section is a very good example of why we need real mono support in
# automake.  This is a massive wart on our development platform.

INDEX_HELPER_WRAPPER = beagled-index-helper
INDEX_HELPER_WRAPPER_IN = $(INDEX_HELPER_WRAPPER).in
INDEX_HELPER_TARGET = IndexHelper.exe

$(INDEX_HELPER_WRAPPER): $(srcdir)/$(INDEX_HELPER_WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_HELPER_TARGET)|g" < $(srcdir)/$(INDEX_HELPER_WRAPPER_IN) > $@
	chmod +x $(INDEX_HELPER_WRAPPER)

INDEX_HELPER_CSFLAGS = -target:exe $(BEAGLE_DEFINES)

indexhelperdir = $(srcdir)/IndexHelper

INDEX_HELPER_CSFILES =	\
	$(indexhelperdir)/IndexHelper.cs		\
	$(indexhelperdir)/RemoteIndexerExecutor.cs


INDEX_HELPER_LOCAL_ASSEMBLIES =		\
	../Util/Util.dll		\
	../BeagleClient/Beagle.dll	\
	./$(DAEMON_DLL_TARGET)

INDEX_HELPER_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(INDEX_HELPER_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(INDEX_HELPER_TARGET): $(INDEX_HELPER_CSFILES) $(INDEX_HELPER_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(INDEX_HELPER_CSFLAGS) $(INDEX_HELPER_CSFILES) $(INDEX_HELPER_ASSEMBLIES)

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
	$(CSC) -out:$@ $(EXTRACT_CONTENT_CSFLAGS) $(EXTRACT_CONTENT_CSFILES) $(EXTRACT_CONTENT_ASSEMBLIES)

############################################################

DUMP_INDEX_WRAPPER = beagle-dump-index
DUMP_INDEX_TARGET = DumpIndex.exe

$(DUMP_INDEX_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(DUMP_INDEX_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(DUMP_INDEX_WRAPPER)

DUMP_INDEX_CSFLAGS = \
	-target:exe

DUMP_INDEX_CSFILES = \
	$(srcdir)/DumpIndex.cs

DUMP_INDEX_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

DUMP_INDEX_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(DUMP_INDEX_TARGET): $(DUMP_INDEX_CSFILES) $(DUMP_INDEX_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(DUMP_INDEX_CSFLAGS) $(DUMP_INDEX_CSFILES) $(DUMP_INDEX_ASSEMBLIES)

############################################################

INDEX_WEB_CONTENT_WRAPPER = beagle-index-url
INDEX_WEB_CONTENT_TARGET = IndexWebContent.exe

$(INDEX_WEB_CONTENT_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(INDEX_WEB_CONTENT_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(INDEX_WEB_CONTENT_WRAPPER)


INDEX_WEB_CONTENT_CSFLAGS = \
	-target:exe

INDEX_WEB_CONTENT_CSFILES = \
	$(srcdir)/IndexWebContent.cs

INDEX_WEB_CONTENT_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

INDEX_WEB_CONTENT_ASSEMBLIES =	\
	$(INDEX_WEB_CONTENT_LOCAL_ASSEMBLIES:%=-r:%)

$(INDEX_WEB_CONTENT_TARGET): $(INDEX_WEB_CONTENT_CSFILES) $(INDEX_WEB_CONTENT_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(INDEX_WEB_CONTENT_CSFLAGS) $(INDEX_WEB_CONTENT_CSFILES) $(INDEX_WEB_CONTENT_ASSEMBLIES)

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
	$(CSC) -out:$@ $(MASTER_DELETE_CSFLAGS) $(MASTER_DELETE_CSFILES) $(MASTER_DELETE_ASSEMBLIES)

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
	$(CSC) -out:$@ $(SNIPPET_TEST_CSFLAGS) $(SNIPPET_TEST_CSFILES) $(SNIPPET_TEST_ASSEMBLIES)

############################################################

TERM_STATS_WRAPPER = beagle-term-stats
TERM_STATS_TARGET = TermStatsTool.exe

$(TERM_STATS_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(TERM_STATS_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(TERM_STATS_WRAPPER)

TERM_STATS_CSFLAGS = \
	-target:exe

TERM_STATS_CSFILES = \
	$(srcdir)/TermStatsTool.cs

TERM_STATS_LOCAL_ASSEMBLIES =	\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

TERM_STATS_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(TERM_STATS_TARGET): $(TERM_STATS_CSFILES) $(TERM_STATS_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(TERM_STATS_CSFLAGS) $(TERM_STATS_CSFILES) $(TERM_STATS_ASSEMBLIES)

############################################################

NAME_INDEX_WRAPPER = beagle-name-index
NAME_INDEX_TARGET = NameIndexTool.exe

$(NAME_INDEX_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(NAME_INDEX_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(NAME_INDEX_WRAPPER)

NAME_INDEX_CSFLAGS = \
	-target:exe

NAME_INDEX_CSFILES = \
	$(srcdir)/NameIndexTool.cs

NAME_INDEX_LOCAL_ASSEMBLIES =	\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

NAME_INDEX_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(NAME_INDEX_TARGET): $(NAME_INDEX_CSFILES) $(NAME_INDEX_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(NAME_INDEX_CSFLAGS) $(NAME_INDEX_CSFILES) $(NAME_INDEX_ASSEMBLIES)

############################################################

FS_SANITY_CHECK_WRAPPER = beagle-file-system-sanity-check
FS_SANITY_CHECK_TARGET = SanityCheckTool.exe

$(FS_SANITY_CHECK_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(FS_SANITY_CHECK_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(FS_SANITY_CHECK_WRAPPER)

FS_SANITY_CHECK_CSFLAGS = \
	-target:exe

FS_SANITY_CHECK_CSFILES = \
	$(srcdir)/SanityCheckTool.cs

FS_SANITY_CHECK_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

FS_SANITY_CHECK_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix				\
	-r:System.Runtime.Remoting		\
	-r:ICSharpCode.SharpZipLib.dll

$(FS_SANITY_CHECK_TARGET): $(FS_SANITY_CHECK_CSFILES) $(FS_SANITY_CHECK_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(FS_SANITY_CHECK_CSFLAGS) $(FS_SANITY_CHECK_CSFILES) $(FS_SANITY_CHECK_ASSEMBLIES)

############################################################

BUILD_INDEX_WRAPPER = beagle-build-index
BUILD_INDEX_TARGET = BuildIndex.exe

$(BUILD_INDEX_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(BUILD_INDEX_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(BUILD_INDEX_WRAPPER)

BUILD_INDEX_CSFLAGS = \
	-target:exe

BUILD_INDEX_CSFILES = \
	$(srcdir)/BuildIndex.cs

BUILD_INDEX_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

BUILD_INDEX_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix

$(BUILD_INDEX_TARGET): $(BUILD_INDEX_CSFILES) $(BUILD_INDEX_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(BUILD_INDEX_CSFLAGS) $(BUILD_INDEX_CSFILES) $(BUILD_INDEX_ASSEMBLIES)

############################################################

MANAGE_INDEX_WRAPPER = beagle-manage-index
MANAGE_INDEX_TARGET = ManageIndex.exe

$(MANAGE_INDEX_WRAPPER): $(srcdir)/$(WRAPPER_IN)
	$(WRAPPER_SED) -e "s|\@target\@|$(MANAGE_INDEX_TARGET)|g" < $(srcdir)/$(WRAPPER_IN) > $@
	chmod +x $(MANAGE_INDEX_WRAPPER)

MANAGE_INDEX_CSFLAGS = \
	-target:exe

MANAGE_INDEX_CSFILES = \
	$(srcdir)/ManageIndex.cs

MANAGE_INDEX_LOCAL_ASSEMBLIES = \
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	./$(PLUGIN_TARGET)			\
	./$(DAEMON_DLL_TARGET)

MANAGE_INDEX_ASSEMBLIES = \
	$(BEAGLED_LIBS)				\
	$(DAEMON_LOCAL_ASSEMBLIES:%=-r:%)	\
	-r:Mono.Posix

$(MANAGE_INDEX_TARGET): $(MANAGE_INDEX_CSFILES) $(MANAGE_INDEX_LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(MANAGE_INDEX_CSFLAGS) $(MANAGE_INDEX_CSFILES) $(MANAGE_INDEX_ASSEMBLIES)

############################################################

CRAWL_SYSTEM_IN = beagle-crawl-system.in
CRAWL_SYSTEM = beagle-crawl-system

$(CRAWL_SYSTEM): $(srcdir)/$(CRAWL_SYSTEM_IN)
	$(WRAPPER_SEB) -e "s|\@target\@|$(CRAWL_SYSTEM)|g" < $(srcdir)/$(CRAWL_SYSTEM_IN) > $@
	chmod +x $(CRAWL_SYSTEM)

# OK, now back to our regularly scheduled program.  But notice all of the
# EXTRA_DIST and CLEANFILES stuff at the bottom of the file.  Yuck.

############################################################
############################################################
############################################################

#	$(MASTER_DELETE_TARGET)

#	$(FS_SANITY_CHECK_TARGET)

ALL_TARGETS = 				\
	$(PLUGIN_TARGET)		\
	$(DAEMON_DLL_TARGET)		\
	$(DAEMON_TARGET)		\
	$(INDEX_HELPER_TARGET)		\
	$(INDEX_HELPER_WRAPPER)		\
	$(INDEX_WEB_CONTENT_TARGET)	\
	$(INDEX_WEB_CONTENT_WRAPPER)	\
	$(EXTRACT_CONTENT_TARGET)	\
	$(DUMP_INDEX_TARGET)		\
	$(SNIPPET_TEST_TARGET)		\
	$(TERM_STATS_TARGET)		\
	$(NAME_INDEX_TARGET)		\
	$(MANAGE_INDEX_TARGET)		\
	$(MANAGE_INDEX_WRAPPER)		\
	$(BUILD_INDEX_TARGET)		\
	$(BUILD_INDEX_WRAPPER)

if ENABLE_WEBSERVICES
ALL_TARGETS += 				\
	$(XSP_TARGET)			\
	$(WEBSERVICES_FRONTEND_TARGET)	\
	$(WEBSERVICES_BACKEND_TARGET)	
endif

if ENABLE_EVO_SHARP
ALL_TARGETS +=				\
	$(EDS_DLL_TARGET)
endif

man_MANS = 				\
	beagled.1			\
	beagle-build-index.8		\
	beagle-manage-index.8

all: $(ALL_TARGETS)

install-data-local: $(ALL_TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(mkinstalldirs) $(DESTDIR)$(pkgdatadir)
	$(mkinstalldirs) $(DESTDIR)$(BACKENDDIR)
if ENABLE_WEBSERVICES
	$(mkinstalldirs) $(DESTDIR)$(XSP_DIR)
	$(mkinstalldirs) $(DESTDIR)$(XSPBIN_DIR)
endif
if ENABLE_EVO_SHARP
	$(INSTALL_DATA) $(EDS_DLL_TARGET) $(DESTDIR)$(BACKENDDIR)/$(EDS_DLL_TARGET)
endif
	$(INSTALL_DATA) $(PLUGIN_TARGET) $(DESTDIR)$(pkglibdir)/$(PLUGIN_TARGET)
	$(INSTALL_DATA) $(DAEMON_DLL_TARGET) $(DESTDIR)$(pkglibdir)/$(DAEMON_DLL_TARGET)
	$(INSTALL_DATA) $(DAEMON_TARGET) $(DESTDIR)$(pkglibdir)/$(DAEMON_TARGET)
	$(INSTALL_DATA) $(INDEX_WEB_CONTENT_TARGET) $(DESTDIR)$(pkglibdir)/$(INDEX_WEB_CONTENT_TARGET)
	$(INSTALL_DATA) $(INDEX_HELPER_TARGET) $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_TARGET)
	$(INSTALL_SCRIPT) $(INDEX_HELPER_WRAPPER) $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_WRAPPER)
	$(INSTALL_DATA) $(BUILD_INDEX_TARGET) $(DESTDIR)$(pkglibdir)/$(BUILD_INDEX_TARGET)
	$(INSTALL_DATA) $(MANAGE_INDEX_TARGET) $(DESTDIR)$(pkglibdir)/$(MANAGE_INDEX_TARGET)
if ENABLE_WEBSERVICES
	$(INSTALL_DATA) $(WEBSERVICES_XSP_TARGET) $(DESTDIR)$(pkglibdir)/$(WEBSERVICES_XSP_TARGET)
	$(INSTALL_DATA) $(WEBSERVICES_BACKEND_TARGET) $(DESTDIR)$(pkglibdir)/$(WEBSERVICES_BACKEND_TARGET)
	$(INSTALL_DATA) $(WEBSERVICES_FRONTEND_TARGET) $(DESTDIR)$(XSPBIN_DIR)/$(WEBSERVICES_FRONTEND_TARGET)
	$(INSTALL_DATA) $(WEBSERVICES_BACKEND_TARGET) $(DESTDIR)$(XSPBIN_DIR)/$(WEBSERVICES_BACKEND_TARGET)
	$(INSTALL_DATA)  $(WEBSERVICES_RESOURCES) $(DESTDIR)$(XSP_DIR)
#	$(INSTALL_DATA) ../Util/Util.dll ../images/Images.dll ../BeagleClient/Beagle.dll ../Tiles/Tiles.dll BeagleDaemonPlugins.dll BeagleDaemonLib.dll $(DESTDIR)$(XSPBIN_DIR)

endif

uninstall-local:
if ENABLE_WEBSERVICES
	rm -f $(DESTDIR)$(pkglibdir)/$(WEBSERVICES_BACKEND_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(WEBSERVICES_XSP_TARGET)
	rm -rf $(DESTDIR)$(XSP_DIR)
endif
if ENABLE_EVO_SHARP
	rm -f $(DESTDIR)$(BACKENDDIR)/$(EDS_DLL_TARGET)
endif
	rm -f $(DESTDIR)$(pkglibdir)/$(PLUGIN_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(DAEMON_DLL_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(DAEMON_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(INDEX_HELPER_WRAPPER)
	rm -f $(DESTDIR)$(pkglibdir)/$(INDEX_WEB_CONTENT_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(BUILD_INDEX_TARGET)
	rm -f $(DESTDIR)$(pkglibdir)/$(MANAGE_INDEX_TARGET)

bin_SCRIPTS = \
	$(DAEMON_WRAPPER)		\
	$(INDEX_WEB_CONTENT_WRAPPER)

sbin_SCRIPTS = \
	$(BUILD_INDEX_WRAPPER)		\
	$(MANAGE_INDEX_WRAPPER)

#	$(MASTER_DELETE_WRAPPER)
#	$(FS_SANITY_CHECK_WRAPPER)

noinst_SCRIPTS=	\
	$(EXTRACT_CONTENT_WRAPPER)	\
	$(DUMP_INDEX_WRAPPER)		\
	$(SNIPPET_TEST_WRAPPER)		\
	$(TERM_STATS_WRAPPER)		\
	$(NAME_INDEX_WRAPPER)		

EXTRA_DIST = \
	$(WRAPPER_IN)   			\
	$(DAEMON_WRAPPER_IN)			\
	$(INDEX_HELPER_WRAPPER_IN)		\
	$(man_MANS)				\
	$(PLUGIN_CSFILES)			\
	$(DAEMON_DLL_CSFILES)			\
	$(DAEMON_CSFILES)			\
	$(EDS_QUERYABLE_CSFILES)		\
	$(EXTRACT_CONTENT_CSFILES)		\
	$(DUMP_INDEX_CSFILES)			\
	$(INDEX_WEB_CONTENT_CSFILES)		\
	$(SNIPPET_TEST_CSFILES)			\
	$(INDEX_HELPER_CSFILES)			\
	$(MASTER_DELETE_CSFILES)		\
	$(TERM_STATS_CSFILES)			\
	$(NAME_INDEX_CSFILES)			\
	$(FS_SANITY_CHECK_CSFILES)		\
	$(MANAGE_INDEX_CSFILES)			\
	$(BUILD_INDEX_CSFILES)			\
	$(WEBSERVICES_XSP_CSFILES)		\
	$(WEBSERVICES_BACKEND_CSFILES)		\
	$(WEBSERVICES_FRONTEND_CSFILES)		\
	$(WEBSERVICES_RESOURCES_DIST)		\
	$(webservicesdir)/WebServiceProxy.cs	\
	$(webservicesdir)/NetBeagleHandler.cs	\
	$(webservicesdir)/NetBeagleQueryable.cs

CLEANFILES = \
	$(PLUGIN_TARGET)       			\
	$(PLUGIN_TARGET).mdb   			\
	$(DAEMON_DLL_TARGET)			\
	$(DAEMON_DLL_TARGET).mdb		\
	$(DAEMON_TARGET)       			\
	$(DAEMON_TARGET).mdb   			\
	$(DAEMON_WRAPPER)      			\
	$(EDS_DLL_TARGET)			\
	$(EDS_DLL_TARGET).mdb			\
	$(INDEX_HELPER_TARGET)			\
	$(INDEX_HELPER_TARGET).mdb		\
	$(INDEX_HELPER_WRAPPER)			\
	$(EXTRACT_CONTENT_TARGET)		\
	$(EXTRACT_CONTENT_TARGET).mdb		\
	$(EXTRACT_CONTENT_WRAPPER)		\
	$(DUMP_INDEX_TARGET)			\
	$(DUMP_INDEX_TARGET).mdb		\
	$(DUMP_INDEX_WRAPPER)			\
	$(INDEX_WEB_CONTENT_TARGET)		\
	$(INDEX_WEB_CONTENT_TARGET).mdb		\
	$(INDEX_WEB_CONTENT_WRAPPER)		\
	$(MASTER_DELETE_TARGET)			\
	$(MASTER_DELETE_TARGET).mdb		\
	$(MASTER_DELETE_WRAPPER)		\
	$(SNIPPET_TEST_TARGET)			\
	$(SNIPPET_TEST_TARGET).mdb		\
	$(SNIPPET_TEST_WRAPPER)			\
	$(TERM_STATS_TARGET)			\
	$(TERM_STATS_TARGET).mdb		\
	$(TERM_STATS_WRAPPER)			\
	$(NAME_INDEX_TARGET)			\
	$(NAME_INDEX_TARGET).mdb		\
	$(NAME_INDEX_WRAPPER)			\
	$(FS_SANITY_CHECK_TARGET)		\
	$(FS_SANITY_CHECK_TARGET).mdb		\
	$(FS_SANITY_CHECK_WRAPPER)		\
	$(MANAGE_INDEX_TARGET)			\
	$(MANAGE_INDEX_TARGET).mdb		\
	$(MANAGE_INDEX_WRAPPER)			\
	$(BUILD_INDEX_TARGET)			\
	$(BUILD_INDEX_TARGET).mdb		\
	$(BUILD_INDEX_WRAPPER)			\
	$(WEBSERVICES_BACKEND_TARGET)		\
	$(WEBSERVICES_BACKEND_TARGET).mdb	\
	$(WEBSERVICES_FRONTEND_TARGET)		\
	$(WEBSERVICES_FRONTEND_TARGET).mdb	\
	$(WEBSERVICES_XSP_TARGET)		\
	$(WEBSERVICES_XSP_TARGET).mdb		

