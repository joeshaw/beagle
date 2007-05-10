
FILTERDIR = $(pkglibdir)/Filters
CSC = $(MCS) -debug

TARGET = Filters.dll

CSFLAGS = -target:library $(BEAGLE_DEFINES)

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

#include entagged-sharp/entagged-sharp.sources

include TagLib/TagLib.sources

# Local copy of http://svn.myrealbox.com/viewcvs/trunk/mcs/class/Managed.Windows.Forms/System.Windows.Forms.RTF/
SWF_RTF_CSFILES =						\
	$(srcdir)/System.Windows.Forms.RTF/Charcode.cs		\
	$(srcdir)/System.Windows.Forms.RTF/Charset.cs		\
	$(srcdir)/System.Windows.Forms.RTF/CharsetFlags.cs	\
	$(srcdir)/System.Windows.Forms.RTF/CharsetType.cs	\
	$(srcdir)/System.Windows.Forms.RTF/ClassDelegate.cs	\
	$(srcdir)/System.Windows.Forms.RTF/Color.cs		\
	$(srcdir)/System.Windows.Forms.RTF/DestinationDelegate.cs \
	$(srcdir)/System.Windows.Forms.RTF/Font.cs		\
	$(srcdir)/System.Windows.Forms.RTF/KeysInit.cs		\
	$(srcdir)/System.Windows.Forms.RTF/KeyStruct.cs		\
	$(srcdir)/System.Windows.Forms.RTF/Major.cs		\
	$(srcdir)/System.Windows.Forms.RTF/Minor.cs		\
	$(srcdir)/System.Windows.Forms.RTF/RTF.cs		\
	$(srcdir)/System.Windows.Forms.RTF/RTFException.cs	\
	$(srcdir)/System.Windows.Forms.RTF/StandardCharCode.cs	\
	$(srcdir)/System.Windows.Forms.RTF/StandardCharName.cs	\
	$(srcdir)/System.Windows.Forms.RTF/Style.cs		\
	$(srcdir)/System.Windows.Forms.RTF/StyleElement.cs	\
	$(srcdir)/System.Windows.Forms.RTF/StyleType.cs		\
	$(srcdir)/System.Windows.Forms.RTF/TextMap.cs		\
	$(srcdir)/System.Windows.Forms.RTF/TokenClass.cs

CSFILES = 				\
	$(srcdir)/AssemblyInfo.cs	\
	$(TAGLIB_CSFILES)		\
	$(HAP_CSFILES)			\
	$(SWF_RTF_CSFILES)		\
	$(srcdir)/FilterArchive.cs	\
	$(srcdir)/FilterHtml.cs		\
	$(srcdir)/FilterImage.cs	\
	$(srcdir)/FilterJpeg.cs		\
	$(srcdir)/FilterTiff.cs		\
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
	$(srcdir)/FilterJs.cs		\
	$(srcdir)/FilterLisp.cs		\
	$(srcdir)/FilterMatlab.cs	\
	$(srcdir)/FilterScilab.cs	\
	$(srcdir)/FilterDocbook.cs	\
	$(srcdir)/FilterMonodoc.cs	\
	$(srcdir)/FilterDesktop.cs	\
	$(srcdir)/FilterMail.cs		\
	$(srcdir)/FilterMusic.cs	\
	$(srcdir)/FilterImLog.cs	\
	$(srcdir)/FilterKonqHistory.cs	\
	$(srcdir)/FilterShellscript.cs	\
	$(srcdir)/FilterRuby.cs		\
	$(srcdir)/FilterMPlayerVideo.cs	\
	$(srcdir)/FilterTotem.cs	\
	$(srcdir)/FilterBmp.cs		\
	$(srcdir)/FilterExternal.cs	\
	$(srcdir)/FilterEbuild.cs	\
	$(srcdir)/FilterGif.cs		\
	$(srcdir)/FilterXslt.cs		\
	$(srcdir)/FilterRPM.cs		\
	$(srcdir)/FilterPackage.cs	\
	$(srcdir)/FilterDeb.cs		\
	$(srcdir)/FilterBoo.cs		\
	$(srcdir)/FilterSvg.cs		\
	$(srcdir)/FilterKAddressBook.cs \
	$(srcdir)/FilterKCal.cs		\
	$(srcdir)/FilterKNotes.cs	\
	$(srcdir)/FilterKOrganizer.cs	\
	$(srcdir)/FilterLabyrinth.cs	\
	$(srcdir)/FilterScribus.cs	\
	$(srcdir)/FilterM3U.cs		\
	$(srcdir)/FilterPls.cs		\
	$(srcdir)/FilterIgnore.cs

if ENABLE_GSF_SHARP
CSFILES += 				\
	$(srcdir)/FilterOle.cs		\
	$(srcdir)/FilterPPT.cs

if ENABLE_WV1
CSFILES += \
	$(srcdir)/FilterDOC.cs		
endif
endif

if HAS_LIBCHM
CSFILES += 				\
	$(srcdir)/FilterChm.cs
endif


LOCAL_ASSEMBLIES =				\
	../Util/Util.dll			\
	../BeagleClient/Beagle.dll		\
	../beagled/BeagleDaemonPlugins.dll

ASSEMBLIES =				\
	$(BEAGLED_LIBS)			\
	$(SHARPZIPLIB_LIBS)		\
	-r:Mono.Posix			\
	-r:System.Web			\
	$(LOCAL_ASSEMBLIES:%=-r:%)

RESOURCES =

$(TARGET): $(CSFILES) $(LOCAL_ASSEMBLIES)
	$(CSC) -out:$@ $(CSFLAGS) $(CSFILES) $(ASSEMBLIES) $(RESOURCES)

all: $(TARGET)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(FILTERDIR)
	$(INSTALL_DATA) $(TARGET) $(TARGET).mdb $(DESTDIR)$(FILTERDIR)

uninstall-local:
	rm -f $(DESTDIR)$(FILTERDIR)/$(TARGET)
	rm -f $(DESTDIR)$(FILTERDIR)/$(TARGET).mdb

COND_CS_FILES = \
	$(srcdir)/FilterOle.cs		\
	$(srcdir)/FilterPPT.cs		\
	$(srcdir)/FilterDOC.cs		\
	$(srcdir)/FilterChm.cs

confdir = $(sysconfdir)/beagle
conf_DATA = external-filters.xml.sample

EXTRA_DIST =			\
	$(CSFILES)		\
	$(COND_CS_FILES)	\
	$(conf_DATA)

CLEANFILES =		\
	$(TARGET)	\
	$(TARGET).mdb
