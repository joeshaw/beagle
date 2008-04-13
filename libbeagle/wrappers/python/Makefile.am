INCLUDES = $(PYTHON_INCLUDES) $(PYBEAGLE_CFLAGS) -I$(top_srcdir)

beagledir = $(pythondir)/beagle
beagle_LTLIBRARIES = beagle.la
beagle_la_LDFLAGS = -module -avoid-version -export-symbols-regex initbeagle
beagle_la_LIBADD = $(BEAGLE_LIBS) $(top_builddir)/beagle/libbeagle.la
beagle_la_SOURCES = \
	beaglemodule.c 

nodist_beagle_la_SOURCES = beagle.c

beagle_PYTHON = __init__.py
beagle_PYTHON_IN =$(beagle_PYTHON).in

$(beagle_PYTHON) : $(beagle_PYTHON_IN)
	sed -e "s|\@VERSION\@|$(VERSION)|g" \
	    -e "s|\@LIB_VERSION\@|$(LIBBEAGLE_VERSION_INFO)|" \
	< $(srcdir)/$(beagle_PYTHON_IN) > $@

EXAMPLE_DIR = $(srcdir)/examples
EXAMPLES =					\
	$(EXAMPLE_DIR)/beagle-async-snippet.py	\
	$(EXAMPLE_DIR)/beagleinfo.py		\
	$(EXAMPLE_DIR)/beaglesearch.py

CLEANFILES = beagle.c
EXTRA_DIST = 		\
	$(EXAMPLES)	\
	$(beagle_PYTHON_IN) \
	beagle.defs 	\
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

