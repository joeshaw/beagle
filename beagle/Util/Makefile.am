# Warnings we don't want to see
# 0169 = The private field is never used
# 0414 = The private field is assigned but its value is never used

CSC = $(MCS) -debug -nowarn:0169,0414
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

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"			\
		-e "s|\@sysconfdir\@|$(sysconfdir)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"		\
		-e "s|\@pkgdatadir\@|$(pkgdatadir)|g"		\
		-e "s|\@localstatedir\@|$(localstatedir)|g"	\
		-e "s|\@datadir\@|$(datadir)|g"			\
		-e "s|\@VERSION\@|$(VERSION)|g"			\
		-e "s|\@GNOME_PREFIX\@|$(GNOME_PREFIX)|g"	\
		-e "s|\@KDE_PREFIX\@|$(KDE_PREFIX)|g"		\
		-e "s|\@SQLITE_MAJ_VER\@|$(SQLITE_MAJ_VER)|g"	\
		< $(EXTSTR_IN) > $@

UTIL_CSFILES = 			        	\
	$(srcdir)/ArrayFu.cs              	\
	$(srcdir)/Battery.cs			\
	$(srcdir)/BetterBitArray.cs		\
	$(srcdir)/camel.cs              	\
	$(srcdir)/Config.cs			\
	$(srcdir)/DateTimeUtil.cs		\
	$(srcdir)/DelayedTextWriter.cs		\
	$(srcdir)/DigikamTags.cs		\
	$(srcdir)/DirectoryWalker.cs		\
	$(srcdir)/Evolution.cs			\
	$(srcdir)/ExceptionHandlingThread.cs	\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GuidFu.cs			\
	$(srcdir)/HtmlRemovingReader.cs		\
	$(srcdir)/ICalParser.cs              	\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/KdeUtils.cs			\
	$(srcdir)/KonqHistoryUtil.cs		\
	$(srcdir)/LineReader.cs			\
	$(srcdir)/Log.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/NetworkService.cs		\
	$(srcdir)/Note.cs               	\
	$(srcdir)/OperaHistory.cs		\
	$(srcdir)/Password.cs			\
	$(srcdir)/PathFinder.cs			\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/ReflectionFu.cs		\
	$(srcdir)/SafeProcess.cs		\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/SmallIntArray.cs		\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/StringMatcher.cs		\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/SystemPriorities.cs		\
	$(srcdir)/TeeTextWriter.cs		\
	$(srcdir)/ThreadPond.cs			\
	$(srcdir)/Timeline.cs			\
	$(srcdir)/TopScores.cs			\
	$(srcdir)/TypeCacheAttribute.cs		\
	$(srcdir)/UnclosableStream.cs		\
	$(srcdir)/UriFu.cs			\
	$(srcdir)/VersionFu.cs			\
	$(srcdir)/XdgMime.cs			\
	$(srcdir)/XmlFu.cs			\
	$(srcdir)/F-Spot/Bim.cs			\
	$(srcdir)/F-Spot/BitConverter.cs	\
	$(srcdir)/F-Spot/Exif.cs		\
	$(srcdir)/F-Spot/IptcFile.cs		\
	$(srcdir)/F-Spot/JpegHeader.cs		\
	$(srcdir)/F-Spot/MetadataStore.cs	\
	$(srcdir)/F-Spot/PngFile.cs		\
	$(srcdir)/F-Spot/PixbufUtils.cs		\
	$(srcdir)/F-Spot/Tiff.cs		\
	$(srcdir)/F-Spot/XmpFile.cs		\
	$(srcdir)/Hal/AccessControl.cs		\
	$(srcdir)/Hal/CPUFreq.cs		\
	$(srcdir)/Hal/Context.cs		\
	$(srcdir)/Hal/Device.cs			\
	$(srcdir)/Hal/IAccessControl.cs		\
	$(srcdir)/Hal/ICPUFreq.cs		\
	$(srcdir)/Hal/IDevice.cs		\
	$(srcdir)/Hal/IKillSwitch.cs		\
	$(srcdir)/Hal/ILaptopPanel.cs		\
	$(srcdir)/Hal/ILightSensor.cs		\
	$(srcdir)/Hal/IManager.cs		\
	$(srcdir)/Hal/IStorage.cs		\
	$(srcdir)/Hal/ISystemPowerManagement.cs	\
	$(srcdir)/Hal/IVolume.cs		\
	$(srcdir)/Hal/IVolumeCrypto.cs		\
	$(srcdir)/Hal/KillSwitch.cs		\
	$(srcdir)/Hal/LaptopPanel.cs		\
	$(srcdir)/Hal/LightSensor.cs		\
	$(srcdir)/Hal/Manager.cs		\
	$(srcdir)/Hal/PropertyModification.cs	\
	$(srcdir)/Hal/Storage.cs		\
	$(srcdir)/Hal/SystemPowerManagement.cs	\
	$(srcdir)/Hal/Volume.cs			\
	$(srcdir)/Hal/VolumeCrypto.cs		\
	$(srcdir)/SemWeb/AssemblyInfo.cs        \
	$(srcdir)/SemWeb/NamespaceManager.cs    \
	$(srcdir)/SemWeb/Util.cs                \
	$(srcdir)/SemWeb/UriMap.cs              \
	$(srcdir)/SemWeb/Resource.cs            \
	$(srcdir)/SemWeb/Statement.cs           \
	$(srcdir)/SemWeb/Store.cs               \
	$(srcdir)/SemWeb/MemoryStore.cs         \
	$(srcdir)/SemWeb/RdfReader.cs           \
	$(srcdir)/SemWeb/RdfXmlReader.cs        \
	$(srcdir)/SemWeb/N3Reader.cs            \
	$(srcdir)/SemWeb/RdfWriter.cs           \
	$(srcdir)/SemWeb/RdfXmlWriter.cs        \
	$(srcdir)/SemWeb/N3Writer.cs            \
	$(srcdir)/SemWeb/RSquary.cs             \
	$(srcdir)/SemWeb/LiteralFilters.cs      \
	$(srcdir)/SemWeb/Query.cs               \
	$(srcdir)/SemWeb/Inference.cs           \
	$(srcdir)/SemWeb/RDFS.cs                \
	$(srcdir)/SemWeb/Algos.cs               \
	$(srcdir)/SemWeb/XPathSemWebNavigator.cs\
	$(srcdir)/QueryKeywordMapping.cs

# To reduce dependencies, we don't build this
IGNORED_SEMWEB_CSFILES =			\
	$(srcdir)/SemWeb/Remote.cs

UTIL_ASSEMBLIES =				\
	$(BEAGLED_LIBS)				\
	$(NDESK_DBUS_GLIB_LIBS)			\
	-r:Mono.Data.Sqlite			\
	-r:Mono.Posix				\
	-r:ICSharpCode.SharpZipLib

if ENABLE_THUNDERBIRD
UTIL_CSFILES +=					\
	$(srcdir)/Thunderbird.cs
endif

if ENABLE_AVAHI
CSFLAGS += -define:ENABLE_AVAHI
UTIL_CSFILES += 				\
	$(srcdir)/AvahiBrowser.cs
UTIL_ASSEMBLIES += $(AVAHI_SHARP_LIBS)
endif

if HAS_LIBCHM 
UTIL_CSFILES +=					\
	$(srcdir)/ChmFile.cs
endif

if ENABLE_INOTIFY
INOTIFY_EXE = Inotify.exe
CSFLAGS += -define:ENABLE_INOTIFY
else
INOTIFY_EXE =
endif

UTIL_TARGET_FILES = $(UTIL_CSFILES) $(EXTSTR)

$(UTIL_TARGET): $(UTIL_TARGET_FILES)
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

if ENABLE_GALAGO
UI_UTIL_CSFILES +=				\
	$(srcdir)/Galago.cs

UI_UTIL_ASSEMBLIES += 				\
	$(GALAGO_LIBS)
endif

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


COND_CS_FILES =				\
	$(srcdir)/AvahiBrowser.cs	\
	$(srcdir)/ChmFile.cs		\
	$(srcdir)/Galago.cs		\
	$(srcdir)/Thunderbird.cs

EXTRA_DIST =			\
	$(IGNORED_SEMWEB_CSFILES)\
	$(EXTSTR_IN)		\
	$(UTIL_CSFILES)		\
	$(UI_UTIL_CSFILES)	\
	$(COND_CS_FILES)	\
	UiUtil.dll.config	\
	Util.dll.config.in	\
	inotify-test

CLEANFILES =			\
	$(EXTSTR)		\
	$(UTIL_TARGET)		\
	$(UTIL_TARGET).mdb	\
	$(UI_UTIL_TARGET)	\
	$(UI_UTIL_TARGET).mdb	\
	Inotify.exe		\
	Inotify.exe.mdb

