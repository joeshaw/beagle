
PROJECT = beagle
VERSION = 0.1

all: $(PROJECT).xpi

INNER_JAR_FILES = 			\
	content/contents.rdf		\
	content/about.xul		\
	content/beagleOverlay.css	\
	content/beagleOverlay.js	\
	content/beagleOverlay.xul	\
	content/beaglePrefs.js		\
	content/beaglePrefs.xul		\
	content/beagleAddDomainDialog.xul	\
	content/md5.js			\
	\
	content/jslib/jslib.js		\
	content/jslib/modules.js	\
	content/jslib/debug/debug.js	\
	content/jslib/io/dir.js		\
	content/jslib/io/dirUtils.js	\
	content/jslib/io/file.js	\
	content/jslib/io/fileUtils.js	\
	content/jslib/io/filesystem.js	\
	\
	locale/en-US/contents.rdf	\
	locale/en-US/beagle.dtd		\
	locale/en-US/beagle.properties	\
	\
	skin/classic/contents.rdf	\
	skin/classic/beagle-big.png	\
	skin/classic/beagle-disabled.png\
	skin/classic/beagle-error.png	\
	skin/classic/beagle.png

OUTER_XPI_FILES = 		\
	chrome.manifest		\
	install.rdf		\
	chrome/$(PROJECT).jar

chrome/$(PROJECT).jar: $(INNER_JAR_FILES)
	-@mkdir -p chrome
	zip -q9 $@ $^

$(PROJECT).xpi: $(OUTER_XPI_FILES)
	zip -q9 $@ $^


EXTRA_DIST =			\
	$(INNER_JAR_FILES)	\
	$(OUTER_XPI_FILES)

CLEANFILES =			\
	chrome/$(PROJECT).jar	\
	$(PROJECT).xpi
