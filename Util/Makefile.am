# Warnings we don't want to see
# 0067 = event defined but not used
# 0618 = Mono.Posix.Syscall is obsolete

CSC = mcs -g -nowarn:0067,0618


TARGET = Util.dll

CSFLAGS = -target:library

GOOGLE_WSDL = $(srcdir)/GoogleSearch.wsdl
GOOGLE_CS   = GoogleSearch.cs

$(GOOGLE_CS): $(GOOGLE_WSDL)
	wsdl -namespace:Beagle.Util -out:$@ $^

EXTSTR = ExternalStringsHack.cs
EXTSTR_IN = $(srcdir)/$(EXTSTR).in
$(EXTSTR): $(EXTSTR_IN)
	sed	-e "s|\@prefix\@|$(prefix)|g"		\
		-e "s|\@pkglibdir\@|$(pkglibdir)|g"	\
		-e "s|\@VERSION\@|$(VERSION)|g"		\
		-e "s|\@GNOME_PREFIX\@|$(GNOME_PREFIX)|g"	\
		< $(EXTSTR_IN) > $@

CSFILES = 			        	\
	$(srcdir)/camel.cs              	\
	$(srcdir)/CommandLineFu.cs		\
	$(srcdir)/FileAdvise.cs			\
	$(srcdir)/ExifData.cs			\
	$(srcdir)/ExceptionHandlingThread.cs	\
	$(srcdir)/ExtendedAttribute.cs  	\
	$(srcdir)/FileSystem.cs			\
	$(srcdir)/FrequencyStatistics.cs	\
	$(srcdir)/FSpotTools.cs         	\
	$(srcdir)/GeckoUtils.cs                 \
	$(srcdir)/GuidFu.cs			\
	$(srcdir)/GtkUtils.cs			\
	$(srcdir)/gnome.cs              	\
	$(srcdir)/ImLog.cs              	\
	$(srcdir)/ImBuddy.cs              	\
	$(srcdir)/Inotify.cs			\
	$(srcdir)/JpegHeader.cs			\
	$(srcdir)/Logger.cs             	\
	$(srcdir)/MultiReader.cs        	\
	$(srcdir)/Mozilla.cs	        	\
	$(srcdir)/NautilusTools.cs      	\
	$(srcdir)/Note.cs               	\
	$(srcdir)/PullingReader.cs      	\
	$(srcdir)/Scheduler.cs			\
	$(srcdir)/Stopwatch.cs			\
	$(srcdir)/StringFu.cs           	\
	$(srcdir)/StringMatcher.cs		\
	$(srcdir)/SystemInformation.cs		\
	$(srcdir)/Timeline.cs			\
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

ASSEMBLIES =					\
	$(BEAGLE_UI_LIBS)			\
	-r:System.Web.Services			\
	-r:Mono.Data.SqliteClient		\
	-r:Mono.Posix


$(TARGET): $(CSFILES) $(GOOGLE_CS) $(EXTSTR)
	$(CSC) --unsafe -o $@ $(CSFLAGS) $^ $(ASSEMBLIES)

Inotify.exe: $(srcdir)/Inotify.cs $(srcdir)/Logger.cs
	$(CSC) --unsafe -o $@ $^ -r:Mono.Posix

all: $(TARGET) Inotify.exe

install-data-local: $(TARGET)
	$(mkinstalldirs) $(DESTDIR)$(pkglibdir)
	$(INSTALL_DATA) $(TARGET) $(DESTDIR)$(pkglibdir)/$(TARGET)

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
	inotify-test

CLEANFILES =			\
	$(GOOGLE_CS)		\
	$(EXTSTR)		\
	$(TARGET)		\
	$(TARGET).mdb		\
	Inotify.exe		\
	Inotify.exe.mdb
