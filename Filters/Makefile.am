
FILTERDIR = $(pkglibdir)/Filters

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
	$(srcdir)/FilterHtml.cs		\
	$(srcdir)/FilterJpeg.cs		\
	$(srcdir)/FilterMan.cs		\
	$(srcdir)/FilterMusic.cs	\
	$(srcdir)/FilterOpenOffice.cs	\
	$(srcdir)/FilterPdf.cs		\
	$(srcdir)/FilterPng.cs		\
	$(srcdir)/FilterText.cs	



LOCAL_ASSEMBLIES =			\
	../Util/Util.dll		\
	../Lucene.Net/Lucene.Net.dll	\
	../indexer/Indexer.dll

ASSEMBLIES =				\
	$(BEAGLE_UI_LIBS)		\
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =


$(TARGET): $(CSFILES)
	$(CSC) -o $@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(FILTERDIR)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(FILTERDIR)/$(TARGET)

uninstall-local:
	rm -f $(DESTDIR)$(FILTERDIR)/$(TARGET)

EXTRA_DIST =		\
	$(CSFILES)

CLEANFILES =		\
	$(TARGET)
