# Warnings we don't want to see
# 0169 = The private field is never used

CSC = mcs -debug -nowarn:0169
CSFLAGS = -target:library

UTIL_TARGET = Util.dll
UTIL_TARGET_CONFIG = $(UTIL_TARGET).config

UI_UTIL_TARGET = UiUtil.dll
UI_UTIL_TARGET_CONFIG = $(UI_UTIL_TARGET).config

if OS_LINUX
CSFLAGS += -define:OS_LINUX
endif

if OS_FREEBSD
CSFLAGS += -define:OS_FREEBSD
endif

if ENABLE_WEBSERVICES
CSFLAGS += -define:ENABLE_WEBSERVICES
endif

GOOGLE_WSDL = $(srcdir)/GoogleSearch.wsdl
GOOGLE_CS   = GoogleSearch.cs

$(GOOGLE_CS): $(GOOGLE_WSDL)
	wsdl -namespace:Beagle.Util -out:$@ $^

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"			\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
		-e "s|\@pkgdatadir\@|$(pkgdatadir)|g"		\
		-e "s|\@localstatedir\@|$(localstatedir)|g"	\
		-e "s|\@VERSION\@|$(VERSION)|g"			\
		-e "s|\@GNOME_PREFIX\@|$(GNOME_PREFIX)|g"	\
		-e "s|\@KDE_PREFIX\@|$(KDE_PREFIX)|g"		\
		-e "s|\@SQLITE_MAJ_VER\@|$(SQLITE_MAJ_VER)|g"	\
		< $(EXTSTR_IN) > $@

UTIL_CSFILES = 			        	\
	$(srcdir)/ArrayFu.cs              	\
	$(srcdir)/BetterBitArray.cs		\
	$(srcdir)/EndianConverter.cs		\
	$(srcdir)/camel.cs              	\
	$(srcdir)/CommandLineFu.cs		\
	$(srcdir)/Conf.cs			\
	$(srcdir)/DelayedTextWriter.cs		\
	$(srcdir)/DigikamTags.cs		\
	$(srcdir)/DirectoryWalker.cs		\
	$(srcdir)/Evolution.cs			\
	$(srcdir)/ExceptionHandlingThread.cs	\
	$(srcdir)/ExifData.cs			\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GuidFu.cs			\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/ImLog.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/IoPriority.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/KdeUtils.cs			\
	$(srcdir)/KonqHistoryUtil.cs		\
	$(srcdir)/Log.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/MetadataStore.cs		\
	$(srcdir)/Mozilla.cs	        	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/Note.cs               	\
	$(srcdir)/PathFinder.cs			\
	$(srcdir)/PngHeader.cs			\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/SmallIntArray.cs		\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/StringMatcher.cs		\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/TeeTextWriter.cs		\
	$(srcdir)/ThreadPond.cs			\
	$(srcdir)/Tiff.cs			\
	$(srcdir)/Timeline.cs			\
	$(srcdir)/TopScores.cs			\
	$(srcdir)/UnclosableStream.cs		\
	$(srcdir)/UriFu.cs			\
	$(srcdir)/XdgMime.cs			\
	$(srcdir)/XmlFu.cs			\
	$(srcdir)/XmpFile.cs			\
	$(srcdir)/SemWeb/ForwardLogic.cs		\
	$(srcdir)/SemWeb/KnowledgeModel.cs		\
	$(srcdir)/SemWeb/MemoryStore.cs		\
	$(srcdir)/SemWeb/N3Parser.cs			\
	$(srcdir)/SemWeb/N3Writer.cs			\
	$(srcdir)/SemWeb/NamespaceManager.cs		\
	$(srcdir)/SemWeb/Query.cs			\
	$(srcdir)/SemWeb/RdfParser.cs			\
	$(srcdir)/SemWeb/RdfWriter.cs			\
	$(srcdir)/SemWeb/Reasoning.cs			\
	$(srcdir)/SemWeb/Resource.cs			\
	$(srcdir)/SemWeb/RSquary.cs			\
	$(srcdir)/SemWeb/RSquaryFilters.cs		\
	$(srcdir)/SemWeb/Sparql.cs			\
	$(srcdir)/SemWeb/SQLStore.cs			\
	$(srcdir)/SemWeb/Statement.cs			\
	$(srcdir)/SemWeb/Store.cs			\
	$(srcdir)/SemWeb/UriMap.cs			\
	$(srcdir)/SemWeb/Util.cs			\
	$(srcdir)/SemWeb/XmlParser.cs			\
	$(srcdir)/SemWeb/XmlWriter.cs			\
	$(srcdir)/SemWeb/XPathSemWebNavigator.cs


if HAS_LIBCHM 
UTIL_CSFILES +=					\
	$(srcdir)/ChmFile.cs
endif

UTIL_ASSEMBLIES =				\
	$(BEAGLED_LIBS)				\
	-r:System.Web.Services			\
	-r:System.Data				\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix				\
	-r:System.Web				\
	-r:ICSharpCode.SharpZipLib

if ENABLE_GALAGO
UTIL_CSFILES +=					\
	$(srcdir)/Galago.cs

UTIL_ASSEMBLIES += 				\
	$(GALAGO_LIBS)
endif

if ENABLE_INOTIFY
INOTIFY_EXE = Inotify.exe
CSFLAGS += -define:ENABLE_INOTIFY
else
INOTIFY_EXE =
endif

$(UTIL_TARGET): $(UTIL_CSFILES) $(GOOGLE_CS) $(EXTSTR)
	$(CSC) -unsafe -out:$@ $(CSFLAGS) $^ $(UTIL_ASSEMBLIES)

UI_UTIL_CSFILES =				\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/GnomeFu.cs              	\
	$(srcdir)/GtkUtils.cs			\
	$(srcdir)/HigMessageDialog.cs		\
	$(srcdir)/XKeybinder.cs

UI_UTIL_ASSEMBLIES =				\
	$(BEAGLE_UI_LIBS)			\
	-r:Util.dll

$(UI_UTIL_TARGET): $(UI_UTIL_CSFILES) $(UTIL_TARGET)
	$(CSC) -unsafe -out:$@ $(CSFLAGS) $(UI_UTIL_CSFILES) $(UI_UTIL_ASSEMBLIES)

INOTIFY_TEST_CSFILES =				\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/Log.cs			\
	$(srcdir)/Logger.cs			\
	$(srcdir)/DelayedTextWriter.cs		\
	$(srcdir)/TeeTextWriter.cs		\
	$(srcdir)/DirectoryWalker.cs		\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/ExceptionHandlingThread.cs

Inotify.exe: $(INOTIFY_TEST_CSFILES)
	$(CSC) -unsafe -out:$@ $^ -r:Mono.Posix -define:INOTIFY_TEST -define:ENABLE_INOTIFY

ALL_TARGETS =			\
	$(UTIL_TARGET)		\
	$(INOTIFY_EXE)

if ENABLE_GUI
ALL_TARGETS += $(UI_UTIL_TARGET)
endif

all: $(ALL_TARGETS)

install-data-local: $(ALL_TARGETS)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(UTIL_TARGET) $(UTIL_TARGET).mdb $(UTIL_TARGET_CONFIG) $(DESTDIR)$(pkglibdir)
if ENABLE_GUI
	$(INSTALL_DATA) $(UI_UTIL_TARGET) $(UI_UTIL_TARGET).mdb $(srcdir)/$(UI_UTIL_TARGET_CONFIG) $(DESTDIR)$(pkglibdir)
endif

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(UTIL_TARGET) $(DESTDIR)$(pkglibdir)/$(UTIL_TARGET).mdb $(DESTDIR)$(pkglibdir)/$(UTIL_TARGET_CONFIG)
	rm -f $(DESTDIR)$(pkglibdir)/$(UI_UTIL_TARGET) $(DESTDIR)$(pkglibdir)/$(UI_UTIL_TARGET).mdb $(DESTDIR)$(pkglibdir)/$(UI_UTIL_TARGET_CONFIG)


COND_CS_FILES =			\
	$(srcdir)/ChmFile.cs	\
	$(srcdir)/Galago.cs

EXTRA_DIST =			\
	$(GOOGLE_WSDL)		\
	$(EXTSTR_IN)		\
	$(UTIL_CSFILES)		\
	$(UI_UTIL_CSFILES)	\
	$(COND_CS_FILES)	\
	UiUtil.dll.config	\
	Util.dll.config.in	\
	inotify-test

CLEANFILES =			\
	$(GOOGLE_CS)		\
	$(EXTSTR)		\
	$(UTIL_TARGET)		\
	$(UTIL_TARGET).mdb	\
	$(UI_UTIL_TARGET)	\
	$(UI_UTIL_TARGET).mdb	\
	Inotify.exe		\
	Inotify.exe.mdb
