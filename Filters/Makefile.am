
CSC = mcs -g

TARGET = Filters.dll

CSFLAGS = -target:library

# Found at http://sharptoolbox.madgeek.com/
HAP_CSFILES =						\
	$(srcdir)/HtmlAgilityPack/crc32.cs		\
	$(srcdir)/HtmlAgilityPack/Header.cs		\
	$(srcdir)/HtmlAgilityPack/HtmlAttribute.cs	\
	$(srcdir)/HtmlAgilityPack/HtmlDocument.cs	\
	$(srcdir)/HtmlAgilityPack/HtmlEntity.cs		\
	$(srcdir)/HtmlAgilityPack/HtmlNode.cs		\
	$(srcdir)/HtmlAgilityPack/HtmlNodeNavigator.cs	\
	$(srcdir)/HtmlAgilityPack/MixedCodeDocument.cs	\
	$(srcdir)/HtmlAgilityPack/tools.cs

CSFILES = 				\
	$(HAP_CSFILES)			\
	$(srcdir)/Flavor.cs		\
	$(srcdir)/Filter.cs		\
	$(srcdir)/FilterHtml.cs		\
	$(srcdir)/FilterOpenOffice.cs	\
	$(srcdir)/FilterText.cs

ASSEMBLIES =				\
	$(BEAGLE_UI_LIBS)		\
	-r:ICSharpCode.SharpZipLib	\
	-r:../Util/Util.dll

RESOURCES =


$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
