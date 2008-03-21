GENERATED_TARGET = $(DUMMY_TARGET:%.dll=%.XmlSerializers.dll)

TYPES =						\
	Beagle.CountMatchQuery			\
	Beagle.DaemonInformationRequest		\
	Beagle.IndexingServiceRequest		\
	Beagle.InformationalMessagesRequest	\
	Beagle.OptimizeIndexesRequest		\
	Beagle.Query				\
	Beagle.ReloadConfigRequest		\
	Beagle.ShutdownRequest			\
	Beagle.SnippetRequest			\
	Beagle.EmptyResponse			\
	Beagle.ErrorResponse			\
	Beagle.FinishedResponse			\
	Beagle.HitsAddedResponse		\
	Beagle.HitsSubtractedResponse		\
	Beagle.IndexingStatusResponse		\
	Beagle.SearchTermResponse		\
	Beagle.DaemonInformationResponse	\
	Beagle.SnippetResponse			\
	Beagle.CountMatchQueryResponse		\
	Beagle.Hit				\
	Beagle.Indexable

TYPES_SGEN_PARAMS = $(TYPES:%=--type:%)

all:
	@echo "Generating serializer files"
	@rm -rf *.cs *.dll *.mdb
	MONO_PATH=../../Util mono ../../Util/sgen.exe ../$(DUMMY_TARGET) $(TYPES_SGEN_PARAMS) --debug --keep --out:.
	@test -f $(GENERATED_TARGET) \
		|| { \
			echo "ERROR in generating serializer assembly." ; \
			echo "Run manually \"MONO_PATH=../../Util mono ../../Util/sgen.exe ../$(DUMMY_TARGET) $(TYPES_SGEN_PARAMS) --debug --keep --out:. \"."; \
			exit 1; \
		}
	@echo "Patching generated file to change the namespace."
	@echo "Otherwise mono's automatically generated serializers will have conflict."
	@for i in *.cs; do \
		sed 	-e "s|Mono.GeneratedSerializers.Literal|Beagle.GeneratedSerializers.Literal|" \
			< $$i > $$i.patched; \
		mv $$i.patched $$i; \
	done;

