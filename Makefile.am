
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = beagle-0.0.pc

SUBDIRS = 			\
	images			\
	templates		\
	Util			\
	Lucene.Net 		\
	Filters 		\
	indexer 		\
	Renderers		\
	tools 			\
	Best

if ENABLE_EPIPHANY_EXTENSION
SUBDIRS += epiphany-extension
endif

EXTRA_DIST = $(pkgconfig_DATA)

DISTCLEANFILES = $(pkgconfig_DATA)
