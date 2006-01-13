INCLUDES= \
	$(LIBTRAYICON_CFLAGS)				\
	$(UIGLUE_CFLAGS)

EXTRA_GLUE_SOURCES =
EXTRA_GLUE_LIBADD =

BEAGLED_GLUE_SOURCES =	\
	beagled-utils.c

if ENABLE_INOTIFY
EXTRA_GLUE_SOURCES +=		\
	inotify.h		\
	inotify-syscalls.h	\
	inotify-glue.c
endif

IOPRIO_GLUE_SOURCES =	\
	ioprio-glue.c

SYSINFO_GLUE_SOURCES =		\
	screensaver-glue.c	\
	vmsize-glue.c
SYSINFO_GLUE_LIBADD = $(SYSTEMINFO_GLUE_LIBS) $(SYSTEMINFO_GLUE_X_LIBS)

KEY_GLUE_SOURCES =	 	\
	tomboykeybinder.c	\
	tomboykeybinder.h	\
	eggaccelerators.c	\
	eggaccelerators.h

TRAYICON_GLUE_SOURCES =		\
	eggtrayicon.c		\
	eggtrayicon.h
TRAYICON_GLUE_LIBADD = $(LIBTRAYICON_LIBS)

XDGMIME_GLUE_SOURCES =		\
	xdgmime/xdgmime.c	\
	xdgmime/xdgmimeglob.c	\
	xdgmime/xdgmimeint.c	\
	xdgmime/xdgmimemagic.c	\
	xdgmime/xdgmimealias.c	\
	xdgmime/xdgmimeparent.c	\
	xdgmime/xdgmimecache.c

UI_GLUE_SOURCES =		\
	ephy-spinner.c		\
	ephy-spinner.h		\
	search-entry.c		\
	search-entry.h		\
	search-entry-watermark.h\
	ui-glue.c
UI_GLUE_LIBADD = $(UIGLUE_LIBS)

search-entry.c: search-entry-watermark.h

search-entry-watermark.h: search-entry-watermark.svg
	echo '#define SEARCH_ENTRY_WATERMARK_SVG "\' > $@; \
	sed -e 's/"/\\"/g' -e 's/$$/\\/' -e 's/#000000/#%s/g' $< >> $@; \
	echo '"' >> $@

if ENABLE_WV1
INCLUDES += \
	$(WV1_CFLAGS)
EXTRA_GLUE_SOURCES +=	\
	wv1-glue.c
EXTRA_GLUE_LIBADD +=	\
	$(WV1_LIBS)
endif

libbeagleglue_la_SOURCES = 		\
	$(BEAGLED_GLUE_SOURCES)		\
	$(IOPRIO_GLUE_SOURCES)		\
	$(SYSINFO_GLUE_SOURCES)		\
	$(XDGMIME_GLUE_SOURCES)		\
	$(EXTRA_GLUE_SOURCES)

libbeagleglue_la_LIBADD =	\
	$(SYSINFO_GLUE_LIBADD)	\
	$(EXTRA_GLUE_LIBADD)

libbeagleuiglue_la_SOURCES =	\
	$(KEY_GLUE_SOURCES)	\
	$(TRAYICON_GLUE_SOURCES)\
	$(UI_GLUE_SOURCES)

libbeagleuiglue_la_LIBADD =	\
	$(TRAYICON_GLUE_LIBADD)	\
	$(UI_GLUE_LIBADD)

gluelibdir = $(pkglibdir)
gluelib_LTLIBRARIES = 		\
	libbeagleglue.la	\
	libbeagleuiglue.la

COND_SOURCES = 			\
	wv1-glue.c		\
	inotify.h		\
	inotify-syscalls.h	\
	inotify-glue.c

EXTRA_DIST =			\
	$(COND_SOURCES)		\
	search-entry-watermark.svg


