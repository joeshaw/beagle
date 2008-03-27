@EPIPHANY_EXTENSION_RULE@

extension_in_files = beagle.ephy-extension.in
extension_files = $(extension_in_files:.ephy-extension.in=.ephy-extension)

if USE_NEW_EPIPHANY_EXTENSION
BEAGLE_PY_IN = beagle-2.22.py.in
else
BEAGLE_PY_IN = beagle.py.in
endif

beagle.py: $(BEAGLE_PY_IN)
	sed					\
	-e "s|\@localedir\@|${localedir}|g"	\
	< $(srcdir)/$(BEAGLE_PY_IN) > beagle.py

# Hack to fix distcheck
if INSTALL_EPIPHANY_EXTENSION
extensiondir = $(EPIPHANY_EXTENSIONS_DIR)
extension_DATA = $(extension_files) beagle.py
else
extensiondir = $(libdir)/epiphany/extensions
extension_DATA = $(extension_files) beagle.py
endif

EXTRA_DIST = $(extension_in_files) beagle.py.in beagle-2.22.py.in

CLEANFILES = $(extension_DATA)
