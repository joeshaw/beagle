INCLUDES= \
	$(LIBGECKOGLUE_CFLAGS)				\
	$(LIBTRAYICON_CFLAGS)				\
	-I$(MOZILLA_INCLUDE_ROOT)/content		\
	-I$(MOZILLA_INCLUDE_ROOT)/dom			\
	-I$(MOZILLA_INCLUDE_ROOT)/find			\
	-I$(MOZILLA_INCLUDE_ROOT)/locale		\
	-I$(MOZILLA_INCLUDE_ROOT)/webbrwsr		\
	-I$(MOZILLA_INCLUDE_ROOT)/string		\
	-I$(MOZILLA_INCLUDE_ROOT)/pref			\
	-I$(MOZILLA_INCLUDE_ROOT)/gfx			\
	-I$(MOZILLA_INCLUDE_ROOT)/embedstring		\
	-I$(MOZILLA_INCLUDE_ROOT)/chrome		\
	-I$(MOZILLA_INCLUDE_ROOT)/nspr		\
	-DALLOW_PRIVATE_API				\
	-DMOZILLA_HOME=\""$(MOZILLA_HOME)\""		\
	-include $(MOZILLA_INCLUDE_ROOT)/mozilla-config.h

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

GECKO_GLUE_SOURCES = 	\
	gecko-utils.h	\
	gecko-utils.cpp

GECKO_GLUE_LIBADD = $(LIBGECKOGLUE_LIBS)

KEY_GLUE_SOURCES =	 	\
	tomboykeybinder.c	\
	tomboykeybinder.h	\
	eggaccelerators.c	\
	eggaccelerators.h

TRAYICON_GLUE_SOURCES =		\
	eggtrayicon.c		\
	eggtrayicon.h
TRAYICON_GLUE_LIBADD = $(LIBTRAYICON_LIBS)

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
	$(EXTRA_GLUE_SOURCES)

libbeagleglue_la_LIBADD =	\
	$(SYSINFO_GLUE_LIBADD)	\
	$(EXTRA_GLUE_LIBADD)

libbeagleuiglue_la_SOURCES =	\
	$(GECKO_GLUE_SOURCES)	\
	$(KEY_GLUE_SOURCES)	\
	$(TRAYICON_GLUE_SOURCES)

libbeagleuiglue_la_LIBADD =	\
	$(GECKO_GLUE_LIBADD)	\
	$(TRAYICON_GLUE_LIBADD)

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
	$(COND_SOURCES)

