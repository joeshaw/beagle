
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
	$(srcdir)/FilterFlac.cs		\
	$(srcdir)/FilterJpeg.cs		\
	$(srcdir)/FilterMan.cs		\
	$(srcdir)/FilterOpenOffice.cs	\
	$(srcdir)/FilterPdf.cs		\
	$(srcdir)/FilterPng.cs		\
	$(srcdir)/FilterText.cs	        \
	$(srcdir)/FilterSource.cs	\
	$(srcdir)/FilterRTF.cs

if ENABLE_GSF_SHARP
CSFILES += 				\
	$(srcdir)/FilterOle.cs		\
	$(srcdir)/FilterPPT.cs
endif

if ENABLE_GST_SHARP
CSFILES +=				\
	$(srcdir)/FilterGst.cs
else
CSFILES +=				\
	$(srcdir)/FilterMusic.cs
endif

if ENABLE_WV1
CSFILES += \
	$(srcdir)/FilterDOC.cs		
endif

LOCAL_ASSEMBLIES =				\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	../beagled/BeagleDaemonPlugins.dll	

ASSEMBLIES =				\
	$(BEAGLED_LIBS)			\
	-r:ICSharpCode.SharpZipLib.dll	\
	$(LOCAL_ASSEMBLIES:%=-r:%)

if ENABLE_GST_SHARP
ASSEMBLIES +=				\
	$(GST_SHARP_LIBS)
endif

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
