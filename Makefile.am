
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = beagle-0.0.pc

SUBDIRS = 			\
	Util			\
	images			\
	templates		\
	glue			\
	BeagleClient            \
	Tiles			\
	beagled                 \
	Filters 		\
	tools 			\
	Best			\
	ImLogViewer		\
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
