
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = beagle-0.0.pc

SUBDIRS = 			\
	images			\
	templates		\
	glue			\
	Util			\
	BeagleClient            \
	beagled                 \
	Filters 		\
	Tiles			\
	tools 			\
	Best			\
	mozilla-extension	\
	searchomatic		\
	chooser-fu

if ENABLE_EPIPHANY_EXTENSION
SUBDIRS += epiphany-extension
endif

EXTRA_DIST = \
	$(pkgconfig_DATA) \
	beagle.spec

DISTCLEANFILES = $(pkgconfig_DATA)
