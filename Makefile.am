
pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = beagle-0.0.pc beagle-ui-0.0.pc beagle-daemon.pc

SUBDIRS = 			\
	po

if ENABLE_LIBBEAGLE
SUBDIRS += libbeagle
endif

SUBDIRS +=			\
	wrappers		\
	Util			\
	glue			\
	BeagleClient            \
	beagled                 \
	Filters 		\
	tools

if ENABLE_GUI
SUBDIRS +=			\
	images			\
	search			\
	ImLogViewer
endif

SUBDIRS +=			\
	mozilla-extension	
	
if ENABLE_EPIPHANY_EXTENSION
SUBDIRS += epiphany-extension
endif

if ENABLE_BLUDGEON
SUBDIRS += bludgeon
endif

# When the chooser work has been fixed up to use libbeagle, we can
# reenable this directory.
# SUBDIRS += chooser-fu

EXTRA_DIST = 			\
	HACKING			\
	intltool-extract.in	\
	intltool-merge.in	\
	intltool-update.in	\
	$(pkgconfig_DATA) 	\
	beagle.spec

DISTCHECK_CONFIGURE_FLAGS = --disable-epiphany-extension-install --enable-gtk-doc

DISTCLEANFILES = 		\
	intltool-extract	\
	intltool-merge		\
	intltool-update		\
	$(pkgconfig_DATA)
