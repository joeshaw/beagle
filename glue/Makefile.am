INCLUDES= \
	$(LIBGECKOGLUE_CFLAGS)                            \
        -I$(MOZILLA_INCLUDE_ROOT)/content                 \
        -I$(MOZILLA_INCLUDE_ROOT)/dom                     \
        -I$(MOZILLA_INCLUDE_ROOT)/find                    \
        -I$(MOZILLA_INCLUDE_ROOT)/locale                \
        -I$(MOZILLA_INCLUDE_ROOT)/webbrwsr                \
        -I$(MOZILLA_INCLUDE_ROOT)/string                  \
        -I$(MOZILLA_INCLUDE_ROOT)/pref                    \
        -I$(MOZILLA_INCLUDE_ROOT)/gfx                     \
	-I$(MOZILLA_INCLUDE_ROOT)/embedstring             \
	-I$(MOZILLA_INCLUDE_ROOT)/chrome             \
        -DALLOW_PRIVATE_API                               \
        -DMOZILLA_HOME=\""$(MOZILLA_HOME)\""              \
        -include $(MOZILLA_INCLUDE_ROOT)/mozilla-config.h


gluelibdir = $(pkglibdir)

gluelib_LTLIBRARIES = 		\
	libinotifyglue.la 	\
	libscreensaverglue.la	\
	libgeckoglue.la

libinotifyglue_la_SOURCES =	\
	inotify.h		\
	inotify-glue.c

libscreensaverglue_la_SOURCES = \
	screensaver-glue.c

libscreensaverglue_la_LIBADD = @X_LIBS@

libgeckoglue_la_SOURCES=        \
	gecko-utils.h           \
	gecko-utils.cpp
libgeckoglue_la_LIBADD=$(LIBGECKOGLUE_LIBS)



