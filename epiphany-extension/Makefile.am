extensiondir = $(libdir)/epiphany/extensions

INCLUDES =				\
	$(EPIPHANY_DEPENDENCY_CFLAGS)	\
	-DSHARE_DIR=\"pkgdatadir\"	\
	-DG_DISABLE_DEPRECATED		\
	-DGDK_DISABLE_DEPRECATED	\
	-DGTK_DISABLE_DEPRECATED	\
	-DGDK_PIXBUF_DISABLE_DEPRECATED	\
	-DGNOME_DISABLE_DEPRECATED

extension_LTLIBRARIES = libdeweyextension.la

libdeweyextension_la_SOURCES =		\
	ephy-dewey.c			\
	ephy-dewey-extension.c		\
	ephy-dewey-extension.h

