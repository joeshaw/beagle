@EPIPHANY_EXTENSION_RULE@

extension_in_files = beagle.ephy-extension.in
extension_files = $(extension_in_files:.ephy-extension.in=.ephy-extension)

beagle.py: beagle.py.in
	sed					\
	-e "s|\@localedir\@|${localedir}|g"	\
	< $(srcdir)/beagle.py.in > beagle.py

extensiondir = $(EPIPHANY_EXTENSIONS_DIR)
extension_DATA = $(extension_files) beagle.py

EXTRA_DIST = $(extension_in_files) beagle.py.in

CLEANFILES = $(extension_DATA)