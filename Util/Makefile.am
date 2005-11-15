# Warnings we don't want to see
# 0067 = event defined but not used

CSC = mcs -debug -nowarn:0067


TARGET = Util.dll
TARGET_CONFIG = $(TARGET).config

CSFLAGS = -target:library

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

CSFILES = 			        	\
	$(srcdir)/ArrayFu.cs              	\
	$(srcdir)/BetterBitArray.cs		\
	$(srcdir)/camel.cs              	\
	$(srcdir)/CommandLineFu.cs		\
	$(srcdir)/CompatFileChooser.cs		\
	$(srcdir)/Conf.cs			\
	$(srcdir)/DelayedTextWriter.cs		\
	$(srcdir)/DirectoryWalker.cs		\
	$(srcdir)/Evolution.cs			\
	$(srcdir)/ExceptionHandlingThread.cs	\
	$(srcdir)/ExifData.cs			\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GeckoUtils.cs                 \
	$(srcdir)/GnomeFu.cs              	\
	$(srcdir)/GtkUtils.cs			\
	$(srcdir)/GuidFu.cs			\
	$(srcdir)/HigMessageDialog.cs		\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/ImLog.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/IoPriority.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/KdeUtils.cs			\
	$(srcdir)/Log.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/Mozilla.cs	        	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/Note.cs               	\
	$(srcdir)/PathFinder.cs			\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/SmallIntArray.cs		\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/StringMatcher.cs		\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/TeeTextWriter.cs		\
	$(srcdir)/ThreadPond.cs			\
	$(srcdir)/Timeline.cs			\
	$(srcdir)/TopScores.cs			\
	$(srcdir)/UnclosableStream.cs		\
	$(srcdir)/UriFu.cs			\
	$(srcdir)/XKeybinder.cs			\
	$(srcdir)/XmlFu.cs

if HAS_LIBCHM 
CSFILES += 					\
	$(srcdir)/ChmFile.cs
endif

ASSEMBLIES =					\
	$(BEAGLE_UI_LIBS)			\
	-r:System.Web.Services			\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix

if ENABLE_GALAGO
CSFILES +=					\
	$(srcdir)/Galago.cs

ASSEMBLIES += 					\
	$(GALAGO_LIBS)
endif

if ENABLE_INOTIFY
INOTIFY_EXE = Inotify.exe
CSFLAGS += -define:ENABLE_INOTIFY
else
INOTIFY_EXE =
endif

$(TARGET): $(CSFILES) $(GOOGLE_CS) $(EXTSTR)
	$(CSC) -unsafe -out:$@ $(CSFLAGS) $^ $(ASSEMBLIES)


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


all: $(TARGET) $(INOTIFY_EXE)

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(TARGET_CONFIG) $(DESTDIR)$(pkglibdir)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

COND_CS_FILES =			\
	$(srcdir)/ChmFile.cs	\
	$(srcdir)/Galago.cs

EXTRA_DIST =			\
	$(GOOGLE_WSDL)		\
	$(EXTSTR_IN)		\
	$(CSFILES)		\
	$(COND_CS_FILES)	\
	Util.dll.config.in	\
	inotify-test

CLEANFILES =			\
	$(GOOGLE_CS)		\
	$(EXTSTR)		\
	$(TARGET)		\
	$(TARGET).mdb		\
	Inotify.exe		\
	Inotify.exe.mdb
