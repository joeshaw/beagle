
CSC = mcs -g

TARGET = Util.dll

CSFLAGS = -target:library

CSFILES = 			\
	camel.cs		\
	gnome.cs		\
	Vfs.cs

ASSEMBLIES =			\
	-r:glib-sharp.dll	\
	-r:gtk-sharp.dll	\
	-r:gnome-sharp.dll		

$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
