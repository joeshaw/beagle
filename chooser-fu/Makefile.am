
INCLUDES = 				\
	-DXDG_PREFIX=_beagle_xdg	\
	$(CHOOSER_FU_CFLAGS)

module_flags= -export_dynamic -avoid-version -module -no-undefined
modulesdir=$(libdir)/gtk-2.0/$(GTK_BINARY_VERSION)/filesystems

modules_LTLIBRARIES = libbeaglechooserhack.la

libbeaglechooserhack_la_SOURCES =	\
	beaglequery.h		\
	beaglequery.c		\
	xdgmime.c		\
	xdgmimeglob.c		\
	xdgmimeglob.h		\
	xdgmime.h		\
	xdgmimeint.c		\
	xdgmimeint.h		\
	xdgmimemagic.c		\
	xdgmimemagic.h		\
	gtkfilesystembeagle.h	\
	gtkfilesystembeagle.c

libbeaglechooserhack_la_LIBADD = $(CHOOSER_FU_LIBS)


noinst_PROGRAMS = chooser-test

chooser_test_SOURCES = 		\
	chooser-test.c

chooser_test_LDADD = $(CHOOSER_FU_LIBS)