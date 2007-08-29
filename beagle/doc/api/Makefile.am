ASSEMBLIES = 					\
	$(top_builddir)/BeagleClient/Beagle.dll

LOCAL_ASSEMBLIES =					\
	../Util/Util.dll

UPDATE_ASSEMBLIES = $(addprefix -assembly:, $(ASSEMBLIES))
UPDATER = $(MONODOCER) -delete -path:BeagleClient -name BeagleClient -pretty $(UPDATE_ASSEMBLIES)

if BUILD_DOCS

SOURCESDIR=$(srcdir)
TARGETS=beagle-docs.zip beagle-docs.tree beagle-docs.source

monodocdir=$(SOURCESDIR)
monodoc_DATA=$(TARGETS)

assemble: beagle-docs.zip beagle-docs.tree

beagle-docs.zip beagle-docs.tree: $(srcdir)/BeagleClient/*/*.xml $(srcdir)/BeagleClient/*.xml
	$(MDASSEMBLER) --ecma $(srcdir)/BeagleClient --out beagle-docs

update-docs:
	MONO_PATH=$(top_builddir)/Util $(UPDATER)

update-html:
	if [ -d beagleclient-web-docs ]; then \
		rm -rf beagleclient-web-docs; \
	fi; \
	mkdir beagleclient-web-docs; \
	monodocs2html --source BeagleClient --dest beagleclient-web-docs;

CLEANFILES = beagle-docs.zip beagle-docs.tree

EXTRA_DIST =			\
	beagle-docs.source

endif
