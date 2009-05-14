SUBDIRS=beagle examples wrappers docs

DISTCHECK_CONFIGURE_FLAGS = --enable-gtk-doc

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA = libbeagle-1.0.pc

EXTRA_DIST = $(pkgconfig_DATA)

DISTCLEANFILES = $(pkgconfig_DATA)

MAINTAINERCLEANFILES =	\
	aclocal.m4	\
	config.guess	\
	config.h.in	\
	config.sub	\
	configure	\
	depcomp		\
	install-sh	\
	ltmain.sh	\
	missing		\
	py-compile	\
	`find "$(srcdir)" -type f -name Makefile.in -print`

-include $(top_srcdir)/git.mk
