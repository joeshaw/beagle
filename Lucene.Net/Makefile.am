
CSC = mcs -g

TARGET = Lucene.Net.dll

CSFLAGS = -target:library

CSFILES =								\
	$(srcdir)/AssemblyInfo.cs					\
	$(srcdir)/Analysis/Analyzer.cs					\
	$(srcdir)/Analysis/CharTokenizer.cs				\
	$(srcdir)/Analysis/LetterTokenizer.cs				\
	$(srcdir)/Analysis/LowerCaseFilter.cs				\
	$(srcdir)/Analysis/LowerCaseTokenizer.cs			\
	$(srcdir)/Analysis/PerFieldAnalyzerWrapper.cs			\
	$(srcdir)/Analysis/PorterStemFilter.cs				\
	$(srcdir)/Analysis/PorterStemmer.cs				\
	$(srcdir)/Analysis/SimpleAnalyzer.cs				\
	$(srcdir)/Analysis/StopAnalyzer.cs				\
	$(srcdir)/Analysis/StopFilter.cs				\
	$(srcdir)/Analysis/Token.cs					\
	$(srcdir)/Analysis/TokenFilter.cs				\
	$(srcdir)/Analysis/Tokenizer.cs					\
	$(srcdir)/Analysis/TokenStream.cs				\
	$(srcdir)/Analysis/WhitespaceAnalyzer.cs			\
	$(srcdir)/Analysis/WhitespaceTokenizer.cs			\
	$(srcdir)/Analysis/Standard/CharStream.cs			\
	$(srcdir)/Analysis/Standard/FastCharStream.cs			\
	$(srcdir)/Analysis/Standard/ParseException.cs			\
	$(srcdir)/Analysis/Standard/StandardAnalyzer.cs			\
	$(srcdir)/Analysis/Standard/StandardFilter.cs			\
	$(srcdir)/Analysis/Standard/StandardTokenizerConstants.cs	\
	$(srcdir)/Analysis/Standard/StandardTokenizer.cs		\
	$(srcdir)/Analysis/Standard/StandardTokenizerTokenManager.cs	\
	$(srcdir)/Analysis/Standard/Token.cs				\
	$(srcdir)/Analysis/Standard/TokenMgrError.cs			\
	$(srcdir)/Document/DateField.cs				\
	$(srcdir)/Document/Document.cs				\
	$(srcdir)/Document/Field.cs				\
	$(srcdir)/Index/CompoundFileReader.cs			\
	$(srcdir)/Index/CompoundFileWriter.cs			\
	$(srcdir)/Index/DocumentWriter.cs			\
	$(srcdir)/Index/FieldInfo.cs				\
	$(srcdir)/Index/FieldInfos.cs				\
	$(srcdir)/Index/FieldsReader.cs				\
	$(srcdir)/Index/FieldsWriter.cs				\
	$(srcdir)/Index/FilterIndexReader.cs			\
	$(srcdir)/Index/IndexReader.cs				\
	$(srcdir)/Index/IndexWriter.cs				\
	$(srcdir)/Index/MultipleTermPositions.cs		\
	$(srcdir)/Index/SegmentInfo.cs				\
	$(srcdir)/Index/SegmentInfos.cs				\
	$(srcdir)/Index/SegmentMergeInfo.cs			\
	$(srcdir)/Index/SegmentMergeQueue.cs			\
	$(srcdir)/Index/SegmentMerger.cs			\
	$(srcdir)/Index/SegmentReader.cs			\
	$(srcdir)/Index/SegmentsReader.cs			\
	$(srcdir)/Index/SegmentTermDocs.cs			\
	$(srcdir)/Index/SegmentTermEnum.cs			\
	$(srcdir)/Index/SegmentTermPositions.cs			\
	$(srcdir)/Index/Term.cs					\
	$(srcdir)/Index/TermDocs.cs				\
	$(srcdir)/Index/TermEnum.cs				\
	$(srcdir)/Index/TermInfo.cs				\
	$(srcdir)/Index/TermInfosReader.cs			\
	$(srcdir)/Index/TermInfosWriter.cs			\
	$(srcdir)/QueryParser/CharStream.cs			\
	$(srcdir)/QueryParser/FastCharStream.cs			\
	$(srcdir)/QueryParser/MultiFieldQueryParser.cs		\
	$(srcdir)/QueryParser/ParseException.cs			\
	$(srcdir)/QueryParser/QueryParserConstants.cs		\
	$(srcdir)/QueryParser/QueryParser.cs			\
	$(srcdir)/QueryParser/QueryParserTokenManager.cs	\
	$(srcdir)/QueryParser/TermPositions.cs			\
	$(srcdir)/QueryParser/Token.cs				\
	$(srcdir)/QueryParser/TokenMgrError.cs			\
	$(srcdir)/Search/BooleanClause.cs			\
	$(srcdir)/Search/BooleanQuery.cs			\
	$(srcdir)/Search/BooleanScorer.cs			\
	$(srcdir)/Search/CachingWrapperFilter.cs		\
	$(srcdir)/Search/DateFilter.cs				\
	$(srcdir)/Search/DefaultSimilarity.cs			\
	$(srcdir)/Search/ExactPhraseScorer.cs			\
	$(srcdir)/Search/Explanation.cs				\
	$(srcdir)/Search/Filter.cs				\
	$(srcdir)/Search/FilteredTermEnum.cs			\
	$(srcdir)/Search/FuzzyQuery.cs				\
	$(srcdir)/Search/FuzzyTermEnum.cs			\
	$(srcdir)/Search/HitCollector.cs			\
	$(srcdir)/Search/HitQueue.cs				\
	$(srcdir)/Search/Hits.cs				\
	$(srcdir)/Search/IndexSearcher.cs			\
	$(srcdir)/Search/MultiSearcher.cs			\
	$(srcdir)/Search/MultiTermQuery.cs			\
	$(srcdir)/Search/PhrasePositions.cs			\
	$(srcdir)/Search/PhrasePrefixQuery.cs			\
	$(srcdir)/Search/PhraseQuery.cs				\
	$(srcdir)/Search/PhraseQueue.cs				\
	$(srcdir)/Search/PhraseScorer.cs			\
	$(srcdir)/Search/PrefixQuery.cs				\
	$(srcdir)/Search/Query.cs				\
	$(srcdir)/Search/QueryFilter.cs				\
	$(srcdir)/Search/RangeQuery.cs				\
	$(srcdir)/Search/RemoteSearchable.cs			\
	$(srcdir)/Search/ScoreDoc.cs				\
	$(srcdir)/Search/Scorer.cs				\
	$(srcdir)/Search/Searchable.cs				\
	$(srcdir)/Search/Searcher.cs				\
	$(srcdir)/Search/Similarity.cs				\
	$(srcdir)/Search/SloppyPhraseScorer.cs			\
	$(srcdir)/Search/TermQuery.cs				\
	$(srcdir)/Search/TermScorer.cs				\
	$(srcdir)/Search/TopDocs.cs				\
	$(srcdir)/Search/Weight.cs				\
	$(srcdir)/Search/WildcardQuery.cs			\
	$(srcdir)/Search/WildcardTermEnum.cs			\
	$(srcdir)/Store/Directory.cs				\
	$(srcdir)/Store/FSDirectory.cs				\
	$(srcdir)/Store/InputStream.cs				\
	$(srcdir)/Store/Lock.cs					\
	$(srcdir)/Store/OutputStream.cs				\
	$(srcdir)/Store/RAMDirectory.cs				\
	$(srcdir)/Util/Arrays.cs				\
	$(srcdir)/Util/BitVector.cs				\
	$(srcdir)/Util/Number.cs				\
	$(srcdir)/Util/PriorityQueue.cs

ASSEMBLIES = 				\
	-r:System.Runtime.Remoting

$(TARGET): $(CSFILES)
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
