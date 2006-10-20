
CSC = $(MCS) -debug

TARGET = Lucene.Net.dll

CSFLAGS = -target:library

CSFILES =								\
	$(srcdir)/Lucene.Net/Analysis/Standard/CharStream.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/FastCharStream.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/ParseException.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/StandardAnalyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/StandardFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/StandardTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/StandardTokenizerConstants.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/StandardTokenizerTokenManager.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/Token.cs \
	$(srcdir)/Lucene.Net/Analysis/Standard/TokenMgrError.cs \
	$(srcdir)/Lucene.Net/Analysis/Analyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/CharTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/ISOLatin1AccentFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/KeywordAnalyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/KeywordTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/LengthFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/LetterTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/LowerCaseFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/LowerCaseTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/PerFieldAnalyzerWrapper.cs \
	$(srcdir)/Lucene.Net/Analysis/PorterStemFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/PorterStemmer.cs \
	$(srcdir)/Lucene.Net/Analysis/SimpleAnalyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/StopAnalyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/StopFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/Token.cs \
	$(srcdir)/Lucene.Net/Analysis/TokenFilter.cs \
	$(srcdir)/Lucene.Net/Analysis/Tokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/TokenStream.cs \
	$(srcdir)/Lucene.Net/Analysis/WhitespaceAnalyzer.cs \
	$(srcdir)/Lucene.Net/Analysis/WhitespaceTokenizer.cs \
	$(srcdir)/Lucene.Net/Analysis/WordlistLoader.cs \
	$(srcdir)/Lucene.Net/Document/DateField.cs \
	$(srcdir)/Lucene.Net/Document/DateTools.cs \
	$(srcdir)/Lucene.Net/Document/Document.cs \
	$(srcdir)/Lucene.Net/Document/Field.cs \
	$(srcdir)/Lucene.Net/Document/NumberTools.cs \
	$(srcdir)/Lucene.Net/Index/CompoundFileReader.cs \
	$(srcdir)/Lucene.Net/Index/CompoundFileWriter.cs \
	$(srcdir)/Lucene.Net/Index/DocumentWriter.cs \
	$(srcdir)/Lucene.Net/Index/FieldInfo.cs \
	$(srcdir)/Lucene.Net/Index/FieldInfos.cs \
	$(srcdir)/Lucene.Net/Index/FieldsReader.cs \
	$(srcdir)/Lucene.Net/Index/FieldsWriter.cs \
	$(srcdir)/Lucene.Net/Index/FilterIndexReader.cs \
	$(srcdir)/Lucene.Net/Index/IndexFileNameFilter.cs \
	$(srcdir)/Lucene.Net/Index/IndexFileNames.cs \
	$(srcdir)/Lucene.Net/Index/IndexModifier.cs \
	$(srcdir)/Lucene.Net/Index/IndexReader.cs \
	$(srcdir)/Lucene.Net/Index/IndexWriter.cs \
	$(srcdir)/Lucene.Net/Index/MultipleTermPositions.cs \
	$(srcdir)/Lucene.Net/Index/MultiReader.cs \
	$(srcdir)/Lucene.Net/Index/ParallelReader.cs \
	$(srcdir)/Lucene.Net/Index/SegmentInfo.cs \
	$(srcdir)/Lucene.Net/Index/SegmentInfos.cs \
	$(srcdir)/Lucene.Net/Index/SegmentMergeInfo.cs \
	$(srcdir)/Lucene.Net/Index/SegmentMergeQueue.cs \
	$(srcdir)/Lucene.Net/Index/SegmentMerger.cs \
	$(srcdir)/Lucene.Net/Index/SegmentReader.cs \
	$(srcdir)/Lucene.Net/Index/SegmentTermDocs.cs \
	$(srcdir)/Lucene.Net/Index/SegmentTermEnum.cs \
	$(srcdir)/Lucene.Net/Index/SegmentTermPositions.cs \
	$(srcdir)/Lucene.Net/Index/SegmentTermPositionVector.cs \
	$(srcdir)/Lucene.Net/Index/SegmentTermVector.cs \
	$(srcdir)/Lucene.Net/Index/Term.cs \
	$(srcdir)/Lucene.Net/Index/TermBuffer.cs \
	$(srcdir)/Lucene.Net/Index/TermDocs.cs \
	$(srcdir)/Lucene.Net/Index/TermEnum.cs \
	$(srcdir)/Lucene.Net/Index/TermFreqVector.cs \
	$(srcdir)/Lucene.Net/Index/TermInfo.cs \
	$(srcdir)/Lucene.Net/Index/TermInfosReader.cs \
	$(srcdir)/Lucene.Net/Index/TermInfosWriter.cs \
	$(srcdir)/Lucene.Net/Index/TermPositions.cs \
	$(srcdir)/Lucene.Net/Index/TermPositionVector.cs \
	$(srcdir)/Lucene.Net/Index/TermVectorOffsetInfo.cs \
	$(srcdir)/Lucene.Net/Index/TermVectorsReader.cs \
	$(srcdir)/Lucene.Net/Index/TermVectorsWriter.cs \
	$(srcdir)/Lucene.Net/QueryParser/CharStream.cs \
	$(srcdir)/Lucene.Net/QueryParser/FastCharStream.cs \
	$(srcdir)/Lucene.Net/QueryParser/MultiFieldQueryParser.cs \
	$(srcdir)/Lucene.Net/QueryParser/ParseException.cs \
	$(srcdir)/Lucene.Net/QueryParser/QueryParser.cs \
	$(srcdir)/Lucene.Net/QueryParser/QueryParserConstants.cs \
	$(srcdir)/Lucene.Net/QueryParser/QueryParserTokenManager.cs \
	$(srcdir)/Lucene.Net/QueryParser/Token.cs \
	$(srcdir)/Lucene.Net/QueryParser/TokenMgrError.cs \
	$(srcdir)/Lucene.Net/Search/Regex/RegexQuery.cs \
	$(srcdir)/Lucene.Net/Search/Regex/RegexTermEnum.cs \
	$(srcdir)/Lucene.Net/Search/Regex/SpanRegexQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/NearSpans.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanFirstQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanNearQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanNotQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanOrQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/Spans.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanScorer.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanTermQuery.cs \
	$(srcdir)/Lucene.Net/Search/Spans/SpanWeight.cs \
	$(srcdir)/Lucene.Net/Search/BooleanClause.cs \
	$(srcdir)/Lucene.Net/Search/BooleanQuery.cs \
	$(srcdir)/Lucene.Net/Search/BooleanScorer.cs \
	$(srcdir)/Lucene.Net/Search/BooleanScorer2.cs \
	$(srcdir)/Lucene.Net/Search/CachingWrapperFilter.cs \
	$(srcdir)/Lucene.Net/Search/ConjunctionScorer.cs \
	$(srcdir)/Lucene.Net/Search/ConstantScoreQuery.cs \
	$(srcdir)/Lucene.Net/Search/ConstantScoreRangeQuery.cs \
	$(srcdir)/Lucene.Net/Search/DateFilter.cs \
	$(srcdir)/Lucene.Net/Search/DefaultSimilarity.cs \
	$(srcdir)/Lucene.Net/Search/DisjunctionMaxQuery.cs \
	$(srcdir)/Lucene.Net/Search/DisjunctionMaxScorer.cs \
	$(srcdir)/Lucene.Net/Search/DisjunctionSumScorer.cs \
	$(srcdir)/Lucene.Net/Search/ExactPhraseScorer.cs \
	$(srcdir)/Lucene.Net/Search/Explanation.cs \
	$(srcdir)/Lucene.Net/Search/FieldCache.cs \
	$(srcdir)/Lucene.Net/Search/FieldCacheImpl.cs \
	$(srcdir)/Lucene.Net/Search/FieldDoc.cs \
	$(srcdir)/Lucene.Net/Search/FieldDocSortedHitQueue.cs \
	$(srcdir)/Lucene.Net/Search/FieldSortedHitQueue.cs \
	$(srcdir)/Lucene.Net/Search/Filter.cs \
	$(srcdir)/Lucene.Net/Search/FilteredQuery.cs \
	$(srcdir)/Lucene.Net/Search/FilteredTermEnum.cs \
	$(srcdir)/Lucene.Net/Search/FuzzyQuery.cs \
	$(srcdir)/Lucene.Net/Search/FuzzyTermEnum.cs \
	$(srcdir)/Lucene.Net/Search/Hit.cs \
	$(srcdir)/Lucene.Net/Search/HitCollector.cs \
	$(srcdir)/Lucene.Net/Search/HitIterator.cs \
	$(srcdir)/Lucene.Net/Search/HitQueue.cs \
	$(srcdir)/Lucene.Net/Search/Hits.cs \
	$(srcdir)/Lucene.Net/Search/IndexSearcher.cs \
	$(srcdir)/Lucene.Net/Search/MatchAllDocsQuery.cs \
	$(srcdir)/Lucene.Net/Search/MultiPhraseQuery.cs \
	$(srcdir)/Lucene.Net/Search/MultiSearcher.cs \
	$(srcdir)/Lucene.Net/Search/MultiTermQuery.cs \
	$(srcdir)/Lucene.Net/Search/NonMatchingScorer.cs \
	$(srcdir)/Lucene.Net/Search/ParallelMultiSearcher.cs \
	$(srcdir)/Lucene.Net/Search/PhrasePositions.cs \
	$(srcdir)/Lucene.Net/Search/PhrasePrefixQuery.cs \
	$(srcdir)/Lucene.Net/Search/PhraseQuery.cs \
	$(srcdir)/Lucene.Net/Search/PhraseQueue.cs \
	$(srcdir)/Lucene.Net/Search/PhraseScorer.cs \
	$(srcdir)/Lucene.Net/Search/PrefixQuery.cs \
	$(srcdir)/Lucene.Net/Search/Query.cs \
	$(srcdir)/Lucene.Net/Search/QueryFilter.cs \
	$(srcdir)/Lucene.Net/Search/QueryTermVector.cs \
	$(srcdir)/Lucene.Net/Search/RangeFilter.cs \
	$(srcdir)/Lucene.Net/Search/RangeQuery.cs \
	$(srcdir)/Lucene.Net/Search/RemoteSearchable.cs \
	$(srcdir)/Lucene.Net/Search/ReqExclScorer.cs \
	$(srcdir)/Lucene.Net/Search/ReqOptSumScorer.cs \
	$(srcdir)/Lucene.Net/Search/ScoreDoc.cs \
	$(srcdir)/Lucene.Net/Search/ScoreDocComparator.cs \
	$(srcdir)/Lucene.Net/Search/Scorer.cs \
	$(srcdir)/Lucene.Net/Search/Searchable.cs \
	$(srcdir)/Lucene.Net/Search/Searcher.cs \
	$(srcdir)/Lucene.Net/Search/Similarity.cs \
	$(srcdir)/Lucene.Net/Search/SimilarityDelegator.cs \
	$(srcdir)/Lucene.Net/Search/SloppyPhraseScorer.cs \
	$(srcdir)/Lucene.Net/Search/Sort.cs \
	$(srcdir)/Lucene.Net/Search/SortComparator.cs \
	$(srcdir)/Lucene.Net/Search/SortComparatorSource.cs \
	$(srcdir)/Lucene.Net/Search/SortField.cs \
	$(srcdir)/Lucene.Net/Search/TermQuery.cs \
	$(srcdir)/Lucene.Net/Search/TermScorer.cs \
	$(srcdir)/Lucene.Net/Search/TopDocs.cs \
	$(srcdir)/Lucene.Net/Search/TopFieldDocs.cs \
	$(srcdir)/Lucene.Net/Search/Weight.cs \
	$(srcdir)/Lucene.Net/Search/WildcardQuery.cs \
	$(srcdir)/Lucene.Net/Search/WildcardTermEnum.cs \
	$(srcdir)/Lucene.Net/Store/BufferedIndexInput.cs \
	$(srcdir)/Lucene.Net/Store/BufferedIndexOutput.cs \
	$(srcdir)/Lucene.Net/Store/Directory.cs \
	$(srcdir)/Lucene.Net/Store/FSDirectory.cs \
	$(srcdir)/Lucene.Net/Store/IndexInput.cs \
	$(srcdir)/Lucene.Net/Store/IndexOutput.cs \
	$(srcdir)/Lucene.Net/Store/InputStream.cs \
	$(srcdir)/Lucene.Net/Store/Lock.cs \
	$(srcdir)/Lucene.Net/Store/MMapDirectory.cs \
	$(srcdir)/Lucene.Net/Store/OutputStream.cs \
	$(srcdir)/Lucene.Net/Store/RAMDirectory.cs \
	$(srcdir)/Lucene.Net/Store/RAMFile.cs \
	$(srcdir)/Lucene.Net/Store/RAMInputStream.cs \
	$(srcdir)/Lucene.Net/Store/RAMOutputStream.cs \
	$(srcdir)/Lucene.Net/Util/BitVector.cs \
	$(srcdir)/Lucene.Net/Util/Constants.cs \
	$(srcdir)/Lucene.Net/Util/Parameter.cs \
	$(srcdir)/Lucene.Net/Util/PriorityQueue.cs \
	$(srcdir)/Lucene.Net/Util/SmallFloat.cs \
	$(srcdir)/Lucene.Net/Util/StringHelper.cs \
	$(srcdir)/Lucene.Net/Util/ToStringUtils.cs \
	$(srcdir)/Lucene.Net/AssemblyInfo.cs \
	$(srcdir)/Lucene.Net/LucenePackage.cs \
	$(srcdir)/Lucene.Net/SharpZipLibAdapter.cs \
	$(srcdir)/Lucene.Net/SupportClass.cs

LOCAL_ASSEMBLIES =			\
	../Util/Util.dll

ASSEMBLIES = 				\
	-r:System.Runtime.Remoting	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
