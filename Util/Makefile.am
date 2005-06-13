# Warnings we don't want to see
# 0067 = event defined but not used
# 0618 = Mono.Posix.Syscall is obsolete

CSC = mcs -debug -nowarn:0067,0618


TARGET = Util.dll
TARGET_CONFIG = $(TARGET).config

CSFLAGS = -target:library

if OS_LINUX
CSFLAGS += -define:OS_LINUX
endif

if OS_FREEBSD
CSFLAGS += -define:OS_FREEBSD
endif

GOOGLE_WSDL = $(srcdir)/GoogleSearch.wsdl
GOOGLE_CS   = GoogleSearch.cs

$(GOOGLE_CS): $(GOOGLE_WSDL)
	wsdl -namespace:Beagle.Util -out:$@ $^

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"	\
		-e "s|\@pkgdatadir\@|$(pkgdatadir)|g"	\
		-e "s|\@VERSION\@|$(VERSION)|g"		\
		-e "s|\@GNOME_PREFIX\@|$(GNOME_PREFIX)|g"	\
		-e "s|\@KDE_PREFIX\@|$(KDE_PREFIX)|g"	\
		-e "s|\@XATTR_LIB\@|$(XATTR_LIB)|g"	\
		< $(EXTSTR_IN) > $@

CSFILES = 			        	\
	$(srcdir)/camel.cs              	\
	$(srcdir)/CommandLineFu.cs		\
	$(srcdir)/Conf.cs			\
	$(srcdir)/DirectoryWalker.cs		\
	$(srcdir)/ExceptionHandlingThread.cs	\
	$(srcdir)/ExifData.cs			\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GeckoUtils.cs                 \
	$(srcdir)/gnome.cs              	\
	$(srcdir)/GtkUtils.cs			\
	$(srcdir)/GuidFu.cs			\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/ImLog.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/KdeUtils.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/Mozilla.cs	        	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/Note.cs               	\
	$(srcdir)/PathFinder.cs			\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/StringMatcher.cs		\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/Timeline.cs			\
	$(srcdir)/UnixClient.cs			\
	$(srcdir)/UnixListener.cs		\
	$(srcdir)/UriFu.cs			\
	$(srcdir)/Vfs.cs                        \
	$(srcdir)/XKeybinder.cs

if ! ENABLE_GST_SHARP
CSFILES +=					\
	$(srcdir)/ApeReader.cs			\
	$(srcdir)/Mp3Reader.cs			\
	$(srcdir)/FlacReader.cs			\
	$(srcdir)/OggReader.cs			\
	$(srcdir)/Tag.cs			
endif

if HAS_LIBCHM 
CSFILES += 					\
	$(srcdir)/ChmFile.cs
endif

ASSEMBLIES =					\
	$(BEAGLE_UI_LIBS)			\
	-r:System.Web.Services			\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix


$(TARGET): $(CSFILES) $(GOOGLE_CS) $(EXTSTR)
	$(CSC) -unsafe -out:$@ $(CSFLAGS) $^ $(ASSEMBLIES)

Inotify.exe: $(srcdir)/Inotify.cs $(srcdir)/Logger.cs $(srcdir)/DirectoryWalker.cs
	$(CSC) -unsafe -out:$@ $^ -r:Mono.Posix -define:INOTIFY_TEST

all: $(TARGET) Inotify.exe

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(TARGET_CONFIG) $(DESTDIR)$(pkglibdir)

uninstall-local:
	rm -f $(DESTDIR)$(pkglibdir)/$(TARGET)

COND_CS_FILES =	\
	$(srcdir)/ApeReader.cs	\
	$(srcdir)/Mp3Reader.cs	\
	$(srcdir)/FlacReader.cs	\
	$(srcdir)/OggReader.cs	\
	$(srcdir)/Tag.cs

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
