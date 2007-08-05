PROJECT = beagle
VERSION = 0.1

XPIDL_BIN = /usr/lib/firefox/xpidl
IDL_FILES = /usr/share/idl/mozilla-thunderbird

all: $(PROJECT).xpi

JAR_FILES =					\
	content/beagle.css			\
	content/beagleIndexer.js		\
	content/beagleMailWindow.xul		\
	content/beagleMessenger.xul		\
	content/beaglePrefs.xul			\
	content/beagleQueue.js			\
	content/beagleService.js		\
	content/beagleSettings.js		\
	content/beagleUnindex.js		\
	content/beagleUnindex.xul		\
	content/contents.rdf			\
	locale/en-US/beagle.dtd			\
	locale/en-US/strings.properties		\
	locale/en-US/contents.rdf		\
	skin/classic/overlay.css		\
	skin/classic/beagle.png			\
	skin/classic/beagle-disabled.png	\
	skin/classic/beagle-error.png		\
	skin/classic/contents.rdf

COMPONENT_FILES = 				\
        components/BeagleIndexer.xpt		\
        components/BeagleQueue.xpt		\
        components/BeagleSettings.xpt

XPI_FILES =					\
	chrome.manifest				\
	install.rdf				\
	chrome/$(PROJECT).jar			\
	defaults/preferences/default.js		\
	components/BeagleIndexer.js		\
	components/BeagleQueue.js		\
	components/BeagleSettings.js		\
	components/BeagleCommandLine.js		\
	$(COMPONENT_FILES)

chrome/$(PROJECT).jar: $(JAR_FILES)
	-@mkdir -p chrome
	zip -q9 $@ $^

$(COMPONENT_FILES): 
	-@mkdir -p components 
	$(XPIDL_BIN) -m typelib -w -v -I $(IDL_FILES) -e $@ `echo $@ | sed s/xpt/idl/`

$(PROJECT).xpi: $(XPI_FILES)
	zip -q9 $@ $^

EXTRA_DIST =					\
	$(JAR_FILES)				\
	$(COMPONENT_FILES)			\
	$(XPI_FILES)

CLEANFILES =					\
	chrome/$(PROJECT).jar			\
	components/*.xpt			\
	$(PROJECT).xpi				
