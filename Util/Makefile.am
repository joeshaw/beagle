
CSC = mcs -g

TARGET = Util.dll

CSFLAGS = -target:library

GOOGLE_WSDL = $(srcdir)/GoogleSearch.wsdl
GOOGLE_CS   = GoogleSearch.cs

$(GOOGLE_CS): $(GOOGLE_WSDL)
	wsdl -namespace:Beagle.Util -out:$@ $^

CSFILES = 				\
	$(srcdir)/camel.cs		\
	$(srcdir)/gnome.cs		\
	$(srcdir)/Vfs.cs		\
	$(srcdir)/ImLog.cs		\
	$(srcdir)/Logger.cs		\
	$(srcdir)/MultiReader.cs	\
	$(srcdir)/PullingReader.cs	\
	$(srcdir)/StringFu.cs

ASSEMBLIES =			\
	$(BEAGLE_UI_LIBS)		\
	-r:System.Web.Services


$(TARGET): $(CSFILES) $(GOOGLE_CS)
	$(CSC) -o $@ $(CSFLAGS) $^ $(ASSEMBLIES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(GOOGLE_WSDL)	\
	$(CSFILES)

CLEANFILES =		\
	$(GOOGLE_CS)	\
	$(TARGET)
