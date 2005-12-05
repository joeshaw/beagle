INCLUDES = $(PYTHON_INCLUDES) $(PYBEAGLE_CFLAGS) -I$(top_srcdir)/libbeagle

beagledir = $(pythondir)
beagle_LTLIBRARIES = beagle.la
beagle_la_LDFLAGS = -module -avoid-version -export-symbols-regex initbeagle
beagle_la_LIBADD = $(BEAGLE_LIBS) $(top_builddir)/libbeagle/beagle/libbeagle.la
beagle_la_SOURCES = \
	$(srcdir)/beaglemodule.c 

nodist_beagle_la_SOURCES = $(top_builddir)/beagle.c

CLEANFILES = $(top_builddir)/beagle.c
EXTRA_DIST = \
	beagle.defs \
	beagle.override \
	gen-defs.sh

$(top_builddir)/beagle.c: $(srcdir)/beagle.defs $(srcdir)/beagle.override
	(cd $(srcdir) \
	 && $(PYGTK_CODEGEN) \
	    --override $(srcdir)/beagle.override \
	    --register $(PYGTK_DEFSDIR)/gtk-types.defs \
	    --register $(PYGTK_DEFSDIR)/gdk-types.defs \
	    --prefix pybeagle $(srcdir)/beagle.defs) > $(top_builddir)/beagle.c

