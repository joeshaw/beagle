
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = dewey-0.0.pc

SUBDIRS = 			\
	images			\
	Util			\
	Lucene.Net 		\
	Filters 		\
	indexer 		\
	tools 			\
	Best

if ENABLE_EPIPHANY_EXTENSION
SUBDIRS += epiphany-extension
endif

EXTRA_DIST = $(pkgconfig_DATA)

DISTCLEANFILES = $(pkgconfig_DATA)
