ASSEMBLY = Beagle.Util.Gui
#ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
DIR_BIN = $(top_builddir)/bin
LINK = -pkg:gtk-sharp-2.0 -pkg:gconf-sharp-2.0 -pkg:gnome-sharp-2.0 -r:$(DIR_BIN)/Beagle.Util.dll

SOURCES = \
	Beagle.Util.Gui/CompatFileChooser.cs	\
	Beagle.Util.Gui/GnomeFu.cs		\
	Beagle.Util.Gui/GtkUtils.cs		\
	Beagle.Util.Gui/HigMessageDialog.cs	\
	Beagle.Util.Gui/XKeyBinder.cs

# if ENABLE_GALAGO:
# SOURCES += Galago.cs
# LINK += $(GALAGO_LIBS)

include $(top_srcdir)/build/build.mk

SOURCES_BUILD += $(EXTSTR)
