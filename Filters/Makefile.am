
CSC = mcs -g

TARGET = Filters.dll

CSFLAGS = -target:library

# Found at http://sharptoolbox.madgeek.com/
HAP_CSFILES =					\
	HtmlAgilityPack/crc32.cs		\
	HtmlAgilityPack/Header.cs		\
	HtmlAgilityPack/HtmlAttribute.cs	\
	HtmlAgilityPack/HtmlDocument.cs		\
	HtmlAgilityPack/HtmlEntity.cs		\
	HtmlAgilityPack/HtmlNode.cs		\
	HtmlAgilityPack/HtmlNodeNavigator.cs	\
	HtmlAgilityPack/MixedCodeDocument.cs	\
	HtmlAgilityPack/tools.cs

CSFILES = 			\
	$(HAP_CSFILES)		\
	Flavor.cs		\
	Filter.cs		\
	FilterHtml.cs		\
	FilterOpenOffice.cs	\
	FilterText.cs

ASSEMBLIES =				\
	-r:gtk-sharp.dll		\
	-r:ICSharpCode.SharpZipLib	\
	-r:../Util/Util.dll

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
