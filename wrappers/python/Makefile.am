INCLUDES = $(PYTHON_INCLUDES) $(PYBEAGLE_CFLAGS) -I$(top_srcdir)/libbeagle

beagledir = $(pyexecdir)
beagle_LTLIBRARIES = beagle.la
beagle_la_LDFLAGS = -module -avoid-version -export-symbols-regex initbeagle
beagle_la_LIBADD = $(BEAGLE_LIBS) $(top_builddir)/libbeagle/beagle/libbeagle.la
beagle_la_SOURCES = \
	beaglemodule.c 

nodist_beagle_la_SOURCES = beagle.c

CLEANFILES = beagle.c
EXTRA_DIST = \
	beagle.defs \
	beagle.override \
	gen-defs.sh

beagle.c: beagle.defs beagle.override
.defs.c:
	(cd $(srcdir) \
	 && $(PYGTK_CODEGEN) \
	    --override $*.override \
	    --register $(PYGTK_DEFSDIR)/gtk-types.defs \
	    --register $(PYGTK_DEFSDIR)/gdk-types.defs \
	    --prefix py$* $*.defs) > gen-$*.c \
	&& cp gen-$*.c $*.c \
	&& rm -f gen-$*.c

