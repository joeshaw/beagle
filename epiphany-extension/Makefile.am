
# The extension needs to installed into Epiphany's prefix,
# not Beagle's prefix.  
#extensiondir = $(EPIPHANY_PREFIX)/lib/epiphany/extensions

# ...but that causes problems w/ make distcheck.
extensiondir = $(pkglibdir)/extensions

INCLUDES =				\
	$(EPIPHANY_DEPENDENCY_CFLAGS)	\
	-DSHARE_DIR=\"pkgdatadir\"	\
	-DG_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED	\
	-DGTK_DISABLE_DEPRECATED	\
	-DGDK_PIXBUF_DISABLE_DEPRECATED	\
	-DGNOME_DISABLE_DEPRECATED

extension_LTLIBRARIES = libbeagleextension.la

libbeagleextension_la_SOURCES =		\
	ephy-beagle.c			\
	ephy-beagle-extension.c		\
	ephy-beagle-extension.h

