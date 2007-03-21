SUBDIRS=beagle examples wrappers docs

DISTCHECK_CONFIGURE_FLAGS = --enable-gtk-doc

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = libbeagle-0.0.pc

EXTRA_DIST = $(pkgconfig_DATA)

DISTCLEANFILES = $(pkgconfig_DATA)