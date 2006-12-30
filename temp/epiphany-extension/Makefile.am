# The extension needs to be installed into Epiphany's prefix,
# not Beagle's prefix.

# Hack to fix distcheck
if INSTALL_EPIPHANY_EXTENSION
extensiondir = $(EPIPHANY_EXTENSIONS_DIR)
else
extensiondir = $(libdir)/epiphany/extensions
endif

extension_LTLIBRARIES = libbeagleextension.la

INCLUDES =				\
	$(EPIPHANY_DEPENDENCY_CFLAGS)	\
	-DSHARE_DIR=\"pkgdatadir\"	\
	-DG_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED	\
	-DGTK_DISABLE_DEPRECATED	\
	-DGDK_PIXBUF_DISABLE_DEPRECATED	\
	-DGNOME_DISABLE_DEPRECATED

libbeagleextension_la_SOURCES =		\
	ephy-beagle.c			\
	ephy-beagle-extension.c		\
	ephy-beagle-extension.h

libbeagleextension_la_LDFLAGS = -module -avoid-version

extensionxmldir = $(extensiondir)
extensionxml_in_files = beagle.xml.in.in
extensionxml_DATA = $(extensionxml_in_files:.xml.in.in=.xml)

%.xml: %.xml.in.in $(extension_LTLIBRARIES)
	sed -e "s|%GETTEXT_PACKAGE%|FIXME|" \
	    -e "s|%LOCALE_DIR%|$(datadir)/locale|" \
	    -e "s|%LIBRARY%|`. ./$(extension_LTLIBRARIES) && echo $$dlname`|" \
	    -e "s|%EXTENSION_DIR%|$(extensiondir)|" \
	$< > $@

CLEANFILES = $(extensionxml_DATA)
DISTCLEANFILES = $(extensionxml_DATA)

EXTRA_DIST = $(extensionxml_in_files)
