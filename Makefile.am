
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = beagle-0.0.pc

SUBDIRS = 			\
	po			\
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

EXTRA_DIST = 			\
	intltool-extract.in	\
	intltool-merge.in	\
	intltool-update.in	\
	$(pkgconfig_DATA) 	\
	beagle.spec

DISTCLEANFILES = 		\
	intltool-extract	\
	intltool-merge		\
	intltool-update		\
	$(pkgconfig_DATA)
