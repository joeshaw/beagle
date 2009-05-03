ASSEMBLY = Beagle.Util
ASSEMBLY_BUILD_FLAGS = -unsafe
TARGET = library
LINK = -r:Mono.Posix -r:Mono.Data.Sqlite

# Hopefully temporary
LINK += -pkg:glib-sharp-2.0 -pkg:ndesk-dbus-glib-1.0

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed     -e "s|\@prefix\@|$(prefix)|g"                   \
	        -e "s|\@sysconfdir\@|$(sysconfdir)|g"           \
	        -e "s|\@pkglibdir\@|$(pkglibdir)|g"             \
	        -e "s|\@pkgdatadir\@|$(pkgdatadir)|g"           \
	        -e "s|\@localstatedir\@|$(localstatedir)|g"     \
	        -e "s|\@datadir\@|$(datadir)|g"                 \
	        -e "s|\@VERSION\@|$(VERSION)|g"                 \
	        -e "s|\@KDE_PREFIX\@|$(KDE_PREFIX)|g"           \
	        -e "s|\@SQLITE_MAJ_VER\@|$(SQLITE_MAJ_VER)|g"   \
	        < $(EXTSTR_IN) > $@

SOURCES = \
	Beagle.Util/ArrayFu.cs			\
	Beagle.Util/BatteryMonitor.cs		\
	Beagle.Util/BetterBitArray.cs		\
	Beagle.Util/camel.cs			\
	Beagle.Util/Config.cs			\
	Beagle.Util/DateTimeUtil.cs		\
	Beagle.Util/Debug.cs			\
	Beagle.Util/DelayedTextWriter.cs	\
	Beagle.Util/DigikamTags.cs		\
	Beagle.Util/DirectoryWalker.cs		\
	Beagle.Util/Evolution.cs		\
	Beagle.Util/ExceptionHandlingThread.cs	\
	Beagle.Util/ExtendedAttribute.cs	\
	Beagle.Util/FileAdvise.cs		\
	Beagle.Util/FileNameMarshaler.cs	\
	Beagle.Util/FileSystem.cs		\
	Beagle.Util/FrequencyStatistics.cs	\
	Beagle.Util/FSpotTools.cs		\
	Beagle.Util/GuidFu.cs			\
	Beagle.Util/HtmlRemovingReader.cs	\
	Beagle.Util/ICalParser.cs		\
	Beagle.Util/ImBuddy.cs			\
	Beagle.Util/Inotify.cs			\
	Beagle.Util/KdeUtils.cs			\
	Beagle.Util/KonqHistoryUtil.cs		\
	Beagle.Util/LineReader.cs		\
	Beagle.Util/Log.cs			\
	Beagle.Util/MultiReader.cs		\
	Beagle.Util/NautilusTools.cs		\
	Beagle.Util/NetworkService.cs		\
	Beagle.Util/Note.cs			\
	Beagle.Util/OperaHistory.cs		\
	Beagle.Util/Password.cs			\
	Beagle.Util/PathFinder.cs		\
	Beagle.Util/PullingReader.cs		\
	Beagle.Util/ReflectionFu.cs		\
	Beagle.Util/SafeProcess.cs		\
	Beagle.Util/Scheduler.cs		\
	Beagle.Util/SmallIntArray.cs		\
	Beagle.Util/Stopwatch.cs		\
	Beagle.Util/StringFu.cs			\
	Beagle.Util/StringMatcher.cs		\
	Beagle.Util/SystemInformation.cs	\
	Beagle.Util/SystemPriorities.cs		\
	Beagle.Util/TeeTextWriter.cs		\
	Beagle.Util/ThreadPond.cs		\
	Beagle.Util/Timeline.cs			\
	Beagle.Util/TopScores.cs		\
	Beagle.Util/TypeCacheAttribute.cs	\
	Beagle.Util/UnclosableStream.cs		\
	Beagle.Util/UriFu.cs			\
	Beagle.Util/VersionFu.cs		\
	Beagle.Util/XdgMime.cs			\
	Beagle.Util/XmlFu.cs			\
	Hal/AccessControl.cs			\
	Hal/CPUFreq.cs				\
	Hal/Context.cs				\
	Hal/Device.cs				\
	Hal/IAccessControl.cs			\
	Hal/ICPUFreq.cs				\
	Hal/IDevice.cs				\
	Hal/IKillSwitch.cs			\
	Hal/ILaptopPanel.cs			\
	Hal/ILightSensor.cs			\
	Hal/IManager.cs				\
	Hal/IStorage.cs				\
	Hal/ISystemPowerManagement.cs		\
	Hal/IVolume.cs				\
	Hal/IVolumeCrypto.cs			\
	Hal/KillSwitch.cs			\
	Hal/LaptopPanel.cs			\
	Hal/LightSensor.cs			\
	Hal/Manager.cs				\
	Hal/PropertyModification.cs		\
	Hal/Storage.cs				\
	Hal/SystemPowerManagement.cs		\
	Hal/Volume.cs				\
	Hal/VolumeCrypto.cs			\
	FSpot/BitConverter.cs

include $(top_srcdir)/build/build.mk

SOURCES_BUILD += $(EXTSTR)
