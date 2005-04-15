
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
	$(srcdir)/FilterOpenOffice.cs	\
	$(srcdir)/FilterPdf.cs		\
	$(srcdir)/FilterPng.cs		\
	$(srcdir)/FilterText.cs	        \
	$(srcdir)/FilterRTF.cs		\
	$(srcdir)/FilterSource.cs	\
	$(srcdir)/FilterC.cs		\
	$(srcdir)/FilterCpp.cs		\
	$(srcdir)/FilterCSharp.cs	\
	$(srcdir)/FilterJava.cs		\
	$(srcdir)/FilterPython.cs	\
	$(srcdir)/FilterPerl.cs		\
	$(srcdir)/FilterPhp.cs		\
	$(srcdir)/FilterFortran.cs	\
	$(srcdir)/FilterPascal.cs	\
	$(srcdir)/FilterAbiword.cs	\
	$(srcdir)/FilterSpreadsheet.cs	\
	$(srcdir)/FilterJs.cs

if ENABLE_GSF_SHARP
CSFILES += 				\
	$(srcdir)/FilterPPT.cs
endif

if ENABLE_GST_SHARP
CSFILES +=				\
	$(srcdir)/FilterGst.cs
else
CSFILES +=				\
	$(srcdir)/FilterMusic.cs	\
	$(srcdir)/FilterFlac.cs		\
	$(srcdir)/FilterApe.cs		\
	$(srcdir)/FilterMp3.cs		\
	$(srcdir)/FilterOgg.cs		\
	$(srcdir)/FilterMpc.cs
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

COND_CS_FILES = \
	$(srcdir)/FilterPPT.cs		\
	$(srcdir)/FilterDOC.cs		\
	$(srcdir)/FilterGst.cs		\
	$(srcdir)/FilterMusic.cs	\
	$(srcdir)/FilterFlac.cs		\
	$(srcdir)/FilterApe.cs		\
	$(srcdir)/FilterMp3.cs		\
	$(srcdir)/FilterOgg.cs		\
	$(srcdir)/FilterMpc.cs

EXTRA_DIST =		\
	$(CSFILES)	\
	$(COND_CS_FILES)

CLEANFILES =		\
	$(TARGET)	\
	$(TARGET).mdb
