GENERATED_TARGET = $(DUMMY_TARGET:%.dll=%.XmlSerializers.dll)

all:
	@echo "Generating serializer files"
	@rm -rf *.cs *.dll *.mdb
	MONO_PATH=../../Util mono ../../Util/sgen.exe ../$(DUMMY_TARGET) --debug --keep --out:.
	@test -f $(GENERATED_TARGET) \
		|| { \
			echo "ERROR in generating serializer assembly." ; \
			echo "Run manually \"MONO_PATH=../../Util mono ../../Util/sgen.exe ../$(DUMMY_TARGET) --debug --keep --out:. \"."; \
			exit 1; \
		}
	@echo "Patching generated file to change the namespace."
	@echo "Otherwise mono's automatically generated serializers will have conflict."
	@for i in *.cs; do \
		sed 	-e "s|Mono.GeneratedSerializers.Literal|Beagle.GeneratedSerializers.Literal|" \
			< $$i > $$i.patched; \
		mv $$i.patched $$i; \
	done;

