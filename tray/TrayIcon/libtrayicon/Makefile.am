
INCLUDES = 				\
	$(LIBTRAYICON_CFLAGS) 		\
	-DEGG_COMPILATION 		\
	-DGTK_DISABLE_DEPRECATED 	\
	-DGDK_DISABLE_DEPRECATED 	\
	-DG_DISABLE_DEPRECATED

beaglelibdir = $(pkglibdir)
beaglelib_LTLIBRARIES = libtrayicon.la

libtrayicon_la_SOURCES =	\
	eggtrayicon.h		\
	eggtrayicon.c

libtrayicon_la_LDFLAGS = -export-dynamic -module -avoid-version
libtrayicon_la_LIBADD = $(LIBTRAYICON_LIBS)

install-data-hook:
	rm $(DESTDIR)/$(beaglelibdir)/libtrayicon.a
	rm $(DESTDIR)/$(beaglelibdir)/libtrayicon.la

uninstall-local:
	rm $(DESTDIR)/$(beaglelibdir)/libtrayicon.so

maintainer-clean-local:
	rm -f Makefile.in
